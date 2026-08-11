# Phase 6: Durable CPG Lifecycle & Backend Contract - Research

**Date:** 2026-08-11  
**Status:** Complete  
**Objective:** Research implementation strategy for Phase 6 (Durable CPG Lifecycle & Backend Contract) covering requirements CPG-01..04 and API-01..02.

---

## 1. Executive Summary

Phase 6 binds the immutable project versions introduced in Phase 5 to CodeBadger's existing Postgres-backed durable queue (`DurableCPGQueue`) and Joern server manager pool (`JoernServerManager`). It establishes a unified backend lifecycle contract exposed identically over both **REST (Starlette custom routes)** and **MCP tools**.

### Key Architectural Objectives
1. **Version-Driven Build Lifecycle**: Single source of truth for build status on `project_versions.build_status` across 6 states (`queued`, `building`, `loading`, `ready`, `failed`, `cancelled`).
2. **Exactly-One Build Enforcement**: Database partial unique index on active jobs by version/job_type, avoiding duplicate build jobs across concurrent sync/retry requests.
3. **Resilient Recovery & Cleanup**: Retry with exponential backoff and retry attempt caps during startup reconciliation; clean teardown of partial artifacts on cancellation.
4. **Transport Parity (REST & MCP)**: Matching schemas (`id`, `status`, `phase`, `queue_position`, `elapsed_ms`, `retry_count`, sanitized `error`) between Starlette routes and FastMCP tools, laying clean groundwork for Phase 8 authentication & quotas.

---

## 2. Architecture & Design Patterns

### 2.1 Database & Schema Extensions (`src/models.py`, `src/utils/postgres_db_manager.py`)

#### Schema Changes for `project_versions`
The `project_versions` table needs columns for lifecycle tracking:
- `build_status TEXT NOT NULL DEFAULT 'queued'` (`queued`, `building`, `loading`, `ready`, `failed`, `cancelled`).
- `build_metadata TEXT` (JSON text store for `queue_position`, `elapsed_ms`, `retry_count`, `error_code`, `error_message`).
- `updated_at TEXT NOT NULL`.

#### Schema Changes for `jobs` (Durable Queue)
- Add optional `version_id TEXT` column to `jobs` table (or include in payload and index).
- Partial unique index: `CREATE UNIQUE INDEX IF NOT EXISTS idx_jobs_version_active ON jobs(version_id, job_type) WHERE status IN ('queued', 'running');`

### 2.2 Lifecycle State Flow (CPG-02)

```
[Git Sync / Archive Upload]
           │
           ▼ (create_or_get_version)
      (created) ──► QUEUED (DurableCPGQueue.enqueue_job)
       │               │
  (unchanged)          ▼ (claim_next_job)
       │           BUILDING (c2cpg / javasrc2cpg / etc.)
       ▼               │
    [READY]            ▼
                   LOADING (Joern importCpg & probe)
                       │
                       ▼
                    READY (Registered in codebase_tracker)
                       │
             ┌─────────┴─────────┐
             ▼                   ▼
          FAILED             CANCELLED
        (Retryable)         (Retryable)
```

- **`queued`**: Version created; job submitted to Postgres `jobs` table.
- **`building`**: Worker claimed job; AST/CPG generation in progress via containerized Joern frontend.
- **`loading`**: CPG binary built (`cpg.bin`); Joern query server spawning & loading CPG.
- **`ready`**: Server probe verified; version↔`codebase_hash` mapped in `codebases` table.
- **`failed`**: Frontend build error, load timeout, or retry cap exceeded. Stored with sanitized `error_code` + message.
- **`cancelled`**: Explicit user cancellation. Partial artifacts removed; version record preserved.

### 2.3 Existing Codebase References & Integration Points

| Feature / Seam | File Path | Existing Symbol / Method | Required Extension |
|----------------|-----------|--------------------------|--------------------|
| **Version Catalog** | `src/services/project_version_service.py` | `ProjectVersionService.create_or_get_version` | Extend to record `build_status`, provide status update helpers (`update_version_status`). |
| **Git Ingestion** | `src/services/git_sync_service.py` | `GitSyncService.sync_project_branch` | On `status == "created"`, auto-enqueue CPG build job bound to `version.id`. |
| **Archive Ingestion** | `src/services/archive_upload_service.py` (New) | N/A | Add safe tarball/zip extract with path-traversal prevention (`..` checks), size bounds, content digest computation. |
| **Durable Queue** | `src/tools/core_tools.py`, `src/utils/postgres_job_store.py` | `DurableCPGQueue`, `PostgresJobStore` | Add `version_id` payload binding, enforce `(version_id, job_type)` dedup, pass `version_id` to worker. |
| **Worker Pipeline** | `src/tools/core_tools.py` | `_generate_cpg_async` | Update `version.build_status` through stages (`building` → `loading` → `ready`/`failed`), mask errors. |
| **Startup Reconciliation** | `src/tools/core_tools.py` | `DurableCPGQueue.start()` / `requeue_running_jobs()` | Implement retry attempt cap (e.g. `max_retries = 3`); mark over-cap jobs `failed`. |
| **REST API Surface** | `main.py` | `@mcp.custom_route` / Starlette routing | Mount REST lifecycle endpoints (`/projects`, `/versions`, etc.) sharing app state. |
| **MCP Tool Surface** | `src/tools/mcp_tools.py`, `src/tools/lifecycle_tools.py` (New) | `register_tools` | Register 9 lifecycle MCP tools with schemas matching REST endpoints. |

---

## 3. Requirements Analysis

### CPG-01: Exactly-One Durable CPG Build per Version
- **Requirement**: Enqueue exactly one durable CPG build per version using existing Postgres queue and Joern worker pool.
- **Implementation**:
  - Enqueue triggered automatically when `sync_project_branch` or archive upload yields `status == "created"`.
  - Enforce DB-level constraint via `idx_jobs_version_active` partial unique index.
  - If `sync_project_branch` returns `status == "unchanged"`, skip enqueue and return existing version status.

### CPG-02: Stable Observability & Status Detail
- **Requirement**: Clients can observe stable lifecycle states (`queued`, `building`, `loading`, `ready`, `failed`, `cancelled`) with phase, queue position, elapsed time, retry count, and sanitized errors.
- **Implementation**:
  - Store `build_status` on `project_versions` table.
  - Compute `queue_position` dynamically using `DurableCPGQueue.queue_position(codebase_hash)`.
  - Calculate `elapsed_ms` from `created_at` (or job start time) to current time/completion time.
  - Track `retry_count` in build metadata.
  - Sanitize failure messages using `_mask_text` pattern from `git_sync_service.py` (remove tokens, passwords, host workspace absolute paths, stack traces).

### CPG-03: Retry, Cancellation & Recovery
- **Requirement**: Failed builds can be retried idempotently, cancellable work cleans partial artifacts, and startup reconciliation repairs interrupted jobs.
- **Implementation**:
  - **Retry**: `POST /versions/{id}/retry` or `version_retry` MCP tool. If state is `failed` or `cancelled`, reset `build_status` to `queued`, increment `retry_count`, re-enqueue in `jobs`. If state is `queued` or `building`, return current status without re-enqueue.
  - **Cancel**: `POST /versions/{id}/cancel` or `version_cancel` MCP tool. Guard against cancelling `ready` or `failed` versions. Set `build_status = 'cancelled'`. Purge partial snapshot directories or unfinished `.cpg.bin` files.
  - **Reconciliation**: `DurableCPGQueue.start()` checks running jobs. Increment `attempts`. If `attempts > max_retries` (default 3), fail job with `EXCEEDED_MAX_RETRIES` and mark version `failed`. Otherwise, requeue job.

### CPG-04: Cache Reuse without Version Mutation
- **Requirement**: Equivalent source content and build options reuse existing content-addressed CPG cache without mutating ready versions.
- **Implementation**:
  - Version ID is computed as `hashlib.sha256(f"{project_id}:{commit_sha}:{build_config}")`.
  - Content digest is sha256 of tree content.
  - If two versions share `content_digest` and `build_config`, CPG generation reuses existing `cpg.bin` artifact in `playground/cpgs/{codebase_hash}.cpg.bin` without rebuilding AST.

### API-01: REST Endpoints
- **Requirement**: REST endpoints support project creation, archive upload, version listing/detail, build/status, and deletion.
- **Endpoints Breakdown**:
  - `POST /projects`: Register project (`remote_url`, `default_branch`, `owner_scope`, optional `credential`).
  - `GET /projects`: List projects for owner scope.
  - `GET /projects/{id}`: Get project details.
  - `DELETE /projects/{id}`: Delete project and associated versions/credentials.
  - `POST /projects/{id}/versions/update`: Sync Git remote branch & auto-enqueue build.
  - `POST /projects/{id}/versions`: Upload source archive (zip/tar.gz), create version & auto-enqueue build.
  - `GET /versions/{id}`: Get version detail, `build_status`, `queue_position`, `elapsed_ms`, `retry_count`, `error`.
  - `GET /projects/{id}/versions`: List versions for project.
  - `POST /versions/{id}/retry`: Idempotent build retry.
  - `POST /versions/{id}/cancel`: Cancel active build.

### API-02: MCP Parity
- **Requirement**: MCP lifecycle tools call the same application services and return IDs/status schemas compatible with REST.
- **MCP Tools List**:
  1. `project_create`
  2. `project_list`
  3. `project_delete`
  4. `version_sync`
  5. `version_upload`
  6. `version_list`
  7. `version_get`
  8. `version_retry`
  9. `version_cancel`

---

## 4. Key Technical Challenges & Mitigations

### 4.1 Race Conditions in Concurrent Sync / Retry
- **Challenge**: Multiple clients calling `version_sync` or `version_retry` simultaneously for the same version could create duplicate jobs or race on database status updates.
- **Mitigation**:
  - Use `asyncio.Lock` per project/version in service layers.
  - Postgres Partial Unique Index `idx_jobs_version_active` guarantees single-active-job at the DB level (`ON CONFLICT` handled gracefully).
  - Version status updates use `UPDATE project_versions SET build_status = ... WHERE id = ... AND build_status = ...` optimistic concurrency guards.

### 4.2 Safe Archive Extraction (API-01 Upload)
- **Challenge**: Malicious zip/tar archives containing zip-slips (`../path/traversal`), symlinks to sensitive host files, or oversized files (decompression bombs).
- **Mitigation**:
  - Validate every member path in tar/zip to verify it stays within destination root directory (`os.path.abspath(target_path).startswith(os.path.abspath(extract_dir))`).
  - Reject symlinks/hardlinks in uploaded archives.
  - Enforce total uncompressed size limit (e.g. 500 MB) and individual file count limits.

### 4.3 Error Sanitization Consistency
- **Challenge**: Internal exceptions (e.g. Docker command stderr, Java heap OOM traces, host filesystem paths, Git tokens) could leak into API/MCP responses.
- **Mitigation**:
  - Standardize error masking via a centralized `sanitize_error_detail(error: Exception | str) -> dict` returning `{"error_code": str, "message": str}`.
  - Map known internal exception types (e.g., `GitOperationError`, `DockerException`, `TimeoutError`) to clean codes (`GIT_SYNC_FAILED`, `DOCKER_UNAVAILABLE`, `BUILD_TIMEOUT`, `OOM_KILLED`).

---

## 5. Implementation Plan Recommendations

We recommend breaking Phase 6 into **3 sequential, test-driven waves**:

### Wave 1: Core Lifecycle Service & Schema Extensions (CPG-01, CPG-02, CPG-04)
- Update `ProjectVersion` model & DB migration in `PostgresDBManager` (`build_status`, `build_metadata`).
- Update `DurableCPGQueue` and `PostgresJobStore` to support `version_id` binding and DB-level deduplication.
- Refactor `_generate_cpg_async` worker loop to transition `project_versions.build_status` (`queued` → `building` → `loading` → `ready` / `failed`).
- Wire `GitSyncService.sync_project_branch` to automatically enqueue CPG build on `status == "created"`.
- Unit & integration tests for state transitions and deduplication.

### Wave 2: Cancellation, Idempotent Retry & Crash Recovery (CPG-03)
- Implement `cancel_version_build` service method: update status to `cancelled`, clean partial snapshot/CPG files.
- Implement `retry_version_build` service method: idempotent re-queue for `failed`/`cancelled` versions.
- Enhance `DurableCPGQueue.start()` startup recovery: track retry attempt count per job and mark over-capped jobs `failed` with sanitized error code.
- Unit & integration tests for cancel, retry, and crash recovery logic.

### Wave 3: REST & MCP Transport Surfaces + Archive Upload (API-01, API-02)
- Create `ArchiveUploadService` with security validation (ZipSlip protection, size limits, digest computation).
- Implement REST custom routes on FastMCP Starlette app (`/projects`, `/versions`, update/sync, upload, retry, cancel).
- Implement 9 matching MCP lifecycle tools in `src/tools/lifecycle_tools.py` registered via `src/tools/mcp_tools.py`.
- End-to-end integration tests confirming schema parity between REST and MCP.

---

## 6. Verification Strategy

1. **Unit Tests**:
   - Schema creation & version status update persistence.
   - Idempotent deduplication in `PostgresJobStore` for `version_id`.
   - Archive extraction security (ZipSlip rejection, file size limit rejection).
   - Error sanitizer regex masking (token stripping, absolute path scrubbing).

2. **Integration Tests**:
   - `test_lifecycle_flow.py`: Full cycle from `version_sync` → `queued` → `building` → `loading` → `ready`.
   - `test_retry_and_cancel.py`: Cancel running build, verify artifact cleanup, trigger retry, verify state returns to `queued` → `ready`.
   - `test_startup_recovery.py`: Simulate worker crash during build, restart queue, verify job requeue and max_retries cap.

3. **API & MCP Parity Tests**:
   - Call REST `GET /versions/{id}` and MCP `version_get(version_id)` for the same version and assert JSON payload equivalence (`id`, `status`, `phase`, `queue_position`, `elapsed_ms`, `retry_count`, `error`).
