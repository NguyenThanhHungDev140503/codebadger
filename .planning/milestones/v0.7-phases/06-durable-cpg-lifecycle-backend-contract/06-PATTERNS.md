# Phase 6: Durable CPG Lifecycle & Backend Contract - Pattern Mapping

**Phase:** 06 - durable-cpg-lifecycle-backend-contract  
**Date:** 2026-08-11  

---

## 1. Summary of Files to Modify and Create

| File Path | Action | Role & Purpose | Data Flow |
|-----------|--------|----------------|-----------|
| `src/models.py` | Modify | Entity definitions | Extends `ProjectVersion` dataclass with `build_status`, `build_metadata`, `updated_at`. |
| `src/utils/postgres_db_manager.py` | Modify | Schema & catalog persistence | Adds columns to `project_versions`, status update query helpers, error masking. |
| `src/utils/postgres_job_store.py` | Modify | Job queue persistence | Adds `version_id` binding to `jobs` table & partial unique index `idx_jobs_version_active`. |
| `src/tools/core_tools.py` | Modify | Durable queue & worker pipeline | Enforces `version_id` dedup, transitions `build_status` (`queued`→`building`→`loading`→`ready`/`failed`), startup retry cap. |
| `src/services/project_version_service.py` | Modify | Core version lifecycle domain | Manages version status transitions, list/get filters, retry/cancel triggers. |
| `src/services/git_sync_service.py` | Modify | Git remote ingestion seam | Auto-enqueues CPG build on `status == "created"` using `version_id`. |
| `src/services/archive_upload_service.py` | Create | Source archive ingestion | Tar/zip safe extraction (ZipSlip guard, size limit), content digest, creates version & enqueues build. |
| `src/tools/lifecycle_tools.py` | Create | MCP lifecycle tools | Defines 9 MCP tools matching REST endpoints (`project_create`, `version_sync`, etc.). |
| `src/tools/mcp_tools.py` | Modify | MCP tool registration seam | Registers new lifecycle tools from `lifecycle_tools.py`. |
| `src/api/rest_routes.py` | Create | REST API router/handlers | Implements Starlette route handlers for `/projects` and `/versions` API. |
| `main.py` | Modify | ASGI app entrypoint | Mounts custom REST routes on FastMCP Starlette app instance. |

---

## 2. Pattern Mapping & Concrete Code Excerpts

### Pattern 1: Database Model & Schema Extension

**Analog:** `ProjectVersion` in `src/models.py` & `init_schema` in `src/utils/postgres_db_manager.py`

#### Code Excerpt (`src/models.py`):
```python
@dataclass
class ProjectVersion:
    id: str
    project_id: str
    commit_sha: str
    branch: str
    content_digest: str
    build_config: Dict[str, Any] = field(default_factory=dict)
    manifest: Dict[str, Any] = field(default_factory=dict)
    source_snapshot_ref: Optional[str] = None
    build_status: str = "queued"
    build_metadata: Dict[str, Any] = field(default_factory=dict)
    created_at: datetime = field(default_factory=lambda: datetime.now(timezone.utc))
    updated_at: datetime = field(default_factory=lambda: datetime.now(timezone.utc))
```

#### Code Excerpt (`src/utils/postgres_db_manager.py`):
```python
conn.execute("""
    ALTER TABLE project_versions 
    ADD COLUMN IF NOT EXISTS build_status TEXT NOT NULL DEFAULT 'queued',
    ADD COLUMN IF NOT EXISTS build_metadata TEXT DEFAULT '{}',
    ADD COLUMN IF NOT EXISTS updated_at TEXT;
""")
```

---

### Pattern 2: Durable Job Store Partial Unique Index & Version Binding

**Analog:** `PostgresJobStore.init_schema` and `enqueue_job` in `src/utils/postgres_job_store.py`

#### Code Excerpt (`src/utils/postgres_job_store.py`):
```python
conn.execute("""
    ALTER TABLE jobs ADD COLUMN IF NOT EXISTS version_id TEXT;
""")
conn.execute("""
    CREATE UNIQUE INDEX IF NOT EXISTS idx_jobs_version_active
    ON jobs(version_id, job_type) WHERE status IN ('queued', 'running') AND version_id IS NOT NULL;
""")
```

---

### Pattern 3: Auto-Enqueue Seam in Ingestion

**Analog:** `GitSyncService.sync_project_branch` in `src/services/git_sync_service.py`

#### Code Excerpt (`src/services/git_sync_service.py`):
```python
# After version creation in _do_sync:
version, status = self.version_service.create_or_get_version(...)
if status == "created" and self.cpg_queue:
    await self.cpg_queue.submit(
        codebase_hash=codebase_hash,
        job={
            "source_type": "local",
            "source_path": snapshot_path,
            "language": language,
            "version_id": version.id,
            "project_id": project_id,
        }
    )
return version.to_dict(), status
```

---

### Pattern 4: Transport Parity Envelope & Error Masking

**Analog:** FastMCP custom route pattern in `main.py` & `_mask_text` in `src/services/git_sync_service.py`

#### Code Excerpt (REST / MCP Envelope Format):
```python
def format_version_response(version: ProjectVersion, queue_pos: int = 0) -> Dict[str, Any]:
    return {
        "id": version.id,
        "project_id": version.project_id,
        "commit_sha": version.commit_sha,
        "branch": version.branch,
        "status": version.build_status,
        "phase": _STATUS_TO_PHASE.get(version.build_status, "unknown"),
        "queue_position": queue_pos,
        "elapsed_ms": version.build_metadata.get("elapsed_ms", 0),
        "retry_count": version.build_metadata.get("retry_count", 0),
        "error": version.build_metadata.get("error"),
        "created_at": version.created_at.isoformat(),
        "updated_at": version.updated_at.isoformat(),
    }
```

#### Code Excerpt (Error Masking Helper):
```python
def sanitize_error_detail(error: Exception | str) -> Dict[str, str]:
    msg = str(error)
    masked = re.sub(r"(https?://)[^@\s]+@", r"\1***@", msg)
    masked = re.sub(r"/[a-zA-Z0-9_.-]+(?:/[a-zA-Z0-9_.-]+)+", "[PATH]", masked)
    return {
        "error_code": "BUILD_FAILED",
        "message": masked[:500]
    }
```

---

### Pattern 5: Custom Starlette REST Routes on FastMCP

**Analog:** `@mcp.custom_route` in `main.py`

#### Code Excerpt (`main.py` / `src/api/rest_routes.py`):
```python
@mcp.custom_route("/projects", methods=["POST"])
async def rest_create_project(request: Request):
    body = await request.json()
    project = project_service.register_project(
        remote_url=body["remote_url"],
        default_branch=body.get("default_branch", "main"),
        owner_scope=body.get("owner_scope", "default"),
        credential=body.get("credential"),
    )
    return JSONResponse(project.to_dict(), status_code=201)
```

---

## 3. Implementation Verification Points

1. **State Consistency**: Ensure `build_status` transitions updated atomically on `project_versions` via PostgresDBManager.
2. **Deduplication**: Verify `idx_jobs_version_active` prevents duplicate active jobs when calling `retry` or `sync` concurrently.
3. **Archive Security**: Ensure `ArchiveUploadService` validates tar/zip members against directory traversal (`..`) and caps extraction size to 500 MB.
4. **Transport Parity**: Verify REST and MCP responses for version status contain identical top-level keys (`id`, `status`, `phase`, `queue_position`, `elapsed_ms`, `retry_count`, `error`).
