# Phase 6: Durable CPG Lifecycle & Backend Contract - Context

**Gathered:** 2026-08-10
**Status:** Ready for planning

<domain>
## Phase Boundary

Connect the immutable project versions from Phase 5 to the existing durable CPG build queue, and expose the full lifecycle (build/status/retry/cancel) through both REST and MCP with idempotent, sanitized status semantics. This phase covers exactly-one build per version, stable lifecycle states, durable recovery, and a backend contract whose REST responses and MCP tools return the same schemas. It does not build context retrieval (Phase 7) nor auth/quotas (Phase 8).

</domain>

<decisions>
## Implementation Decisions

### Build trigger & job binding
- **D-01:** CPG builds auto-enqueue immediately after a version is created by sync. `POST /projects/{id}/versions/update` syncs the branch and schedules the build in one call. An unchanged branch returns the existing version and does not enqueue a duplicate job (Phase 5 dedup semantics).
- **D-02:** Lifecycle status is stored as a `build_status` column on the `project_versions` row, authoritative and single source of truth; no separate build table. Queue internals (running/retry) are written back onto the version as its state advances.
- **D-03:** "Exactly one durable build per version" is enforced by a DB partial unique index on `(job_type, version_id)` in the durable `jobs` table — DB-level guarantee, not just application logic.
- **D-04:** The durable queue stays keyed by `codebase_hash` (rest of the system unchanged); when a version's build starts, the version↔`codebase_hash`(+`cpg_path`) mapping is registered so post-build exposing/loading keeps working. Mapping registration is a build-start side effect, not a separate endpoint.

### Lifecycle state model (CPG-02)
- **D-05:** Six-state model on `version.build_status`: `queued → building → loading → ready`, plus `failed` and `cancelled`. `loading` is a distinct sub-stage after the CPG file exists but before Joern exposes it — matches the existing `_STATUS_TO_PHASE` map.
- **D-06:** Version status detail exposes `queue_position`, `elapsed_ms`, `retry_count`, and sanitized error as optional fields in a JSON metadata column on the version row (nullable/zero when not applicable). All four are included now — CPG-02 names them explicitly.
- **D-07:** Failure errors are stored and surfaced only in sanitized form: `error_code` + human message, reusing Phase 5/GitManager error masking (no credentials, no host paths, no stack traces). Raw errors never persist.

### Cancel, retry & recovery (CPG-03)
- **D-08:** Cancellation is explicit and user-initiated only (no timeout/auto-cancel). A cancel request on a non-final version flips status to `cancelled` with a DB-level guard preventing cancellation of a `ready`/`failed` version.
- **D-09:** Cancelling deletes partial artifacts (partial snapshot dir / unstaged CPG) but keeps the `project_versions` row with `status=cancelled`, so a future sync/retry can produce a fresh build without losing provenance.
- **D-10:** Retry is idempotent: `POST /versions/{id}/retry` on a `failed`/`cancelled` version re-enqueues exactly one job (version_id dedup still applies) and resets `build_status` to `queued`. Retrying an already-queued/running version returns the same single job rather than a duplicate.
- **D-11:** Startup reconciliation keeps `requeue_running_jobs()` for crash recovery, but adds a capped `retry_count` per job so a permanently failing build is not requeued forever; builds past the cap land in `failed` with a sanitized error.

### REST surface & archive upload (API-01)
- **D-12:** REST routes are mounted on the same FastMCP/Starlette app, process, and port (shares the `services` dict and one HTTP server; no second port, no separate FastAPI app).
- **D-13:** Archive upload is in scope as a secondary source adapter: `POST /projects/{id}/versions` accepts a tarball and creates a version without needing a Git remote (Git remains primary; archive is an alternative genesis path). Phase 5's deferral was about not making upload the *primary* mechanism, not excluding it.
- **D-14:** RESTful resource shape with action verbs: `POST /projects`, `GET /projects` (list), `GET /projects/{id}`; `POST /projects/{id}/versions/update` (git sync + build), `POST /projects/{id}/versions` (archive upload); `GET /versions` (list), `GET /versions/{id}` (detail + status), `POST /versions/{id}/retry`, `POST /versions/{id}/cancel`; `DELETE /projects/{id}`.

### MCP parity (API-02)
- **D-15:** MCP lifecycle tools (`project_create`, `project_list`, `version_sync`, `version_upload`, `version_list`, `version_get`, `version_retry`, `version_cancel`, `project_delete`) call the same service methods as REST and return envelopes with the same `id`/`status`/`phase`/`queue_position` fields — one contract, REST and MCP are interchangeable surfaces.

### Auth & quotas posture
- **D-16:** Authentication and quotas are Phase 8 (API-03/04). Phase 6 REST/MCP lifecycle surfaces are unauthenticated shells; the response envelope and sanitization patterns are designed now so Phase 8 can bolt authorization on without breaking the contract.

### Claude's Discretion
- Exact REST response envelope field enumeration, Starlette route added to FastMCP's app, how `queue_position`/`elapsed_ms` are computed (reuse `DurableCPGQueue.queue_position()`), metadata JSON column schema for status detail, archive upload validation (size limits, extraction removal of path traversal), and exact MCP tool argument names — standard contracts, subject to existing validation and no-shell rules.
- Archive→version content digest/commit identity mapping (archive has no commit SHA; a synthetic digest-based version identity is acceptable).

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Milestone scope & requirements
- `.planning/ROADMAP.md` — Phase 6 goal, requirements (CPG-01..04, API-01, API-02), dependencies, and success criteria.
- `.planning/REQUIREMENTS.md` — v0.7 requirement IDs; exact wording of CPG-01..04 and API-01/02.
- `.planning/PROJECT.md` — milestone product boundary, Key Decisions, and deferred scope (auth/quotas, raw CPGQL, GHCR deployment).

### Phase 5 contract (the version source of truth this phase consumes)
- `.planning/phases/05-secure-ingestion-version-catalog/05-CONTEXT.md` — immutable version identity, unchanged semantics, credential sealing decisions (D-05..D-08).
- `src/services/project_version_service.py` — `ProjectVersionService.create_or_get_version` (unchanged/created), `compute_version_id`, project/version CRUD to extend.
- `src/services/git_sync_service.py` — `sync_project_branch` returns `(version, status)`; current enqueue handoff point for D-01.
- `src/models.py` — `ProjectVersion` model; add `build_status` + status metadata fields (D-02, D-05, D-06).

### Existing durable queue & coordination (reuse, don't reimplement)
- `src/tools/core_tools.py` §DurableCPGQueue — `job_type`, `submit`, `_worker`, `requeue_running_jobs()`, `queue_position()`, `is_in_flight()`, `QUEUE_FULL`/`SUBMITTED`/`DUPLICATE` return codes, `_STATUS_TO_PHASE` map; add version_id dedup index (D-03) and retry cap (D-11) here.
- `src/services/coordination.py` — `RedisCoordinator` gen/query locks used around build-start side effects.
- `src/services/cpg_generator.py` — `_generate_cpg_async` build path and snapshot mercury/copy logic; cancel partial-artifact cleanup hooks (D-09).
- `src/tools/core_tools.py` — `CPGGenerationQueue` in-memory vs `DurableCPGQueue`; confirm phase targets durable path.

### REST / MCP transport
- `main.py` — FastMCP server construction, lifespan, `register_tools`; where Starlette REST routes get mounted (D-12).
- `src/tools/mcp_tools.py` — tool registration seam for the MCP lifecycle tools (D-15).
- `docs/security.md` — trust boundary, token handling, sanitization and raw CPGQL residual-risk language the contract must respect.

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `ProjectVersionService.create_or_get_version`: returns `(version, status)` with `unchanged` vs `created` — the sync→enqueue seam for D-01.
- `DurableCPGQueue` (`src/tools/core_tools.py:1623`): Postgres jobs, `FOR UPDATE SKIP LOCKED` claiming, `queue_position()`, `is_in_flight()`, backpressure (`QUEUE_FULL`), `requeue_running_jobs()` at `start()` — reuse for CPG-01/02/03.
- `_STATUS_TO_PHASE` map (`generating → building`, `ready`, `failed`) — already decomposes build vs load; extend for `loading`/`cancelled`.
- `RedisCoordinator.codebase_generation_lock`: non-blocking single-flight for build-start side effects.
- Phase 5 error masking in `GitManager`/`git_sync_service.py`: sanitized `error_code` + message pattern to replicate for CPG failures (D-07).
- FastMCP is built on Starlette — the `FastMCP` instance exposes an ASGI/Starlette app that can mount additional routes (D-12).

### Established Patterns
- CPG generation is asynchronous and deduplicated through Postgres-backed jobs; a version must enqueue exactly one (D-03).
- Inputs are validated at MCP boundaries; errors sanitized before return — extend same discipline to REST.
- Immutable versions from Phase 5 must not be mutated by cache reuse; a ready version stays ready.
- Single process/port serves one contract (D-12, D-15).

### Integration Points
- `git_sync_service.sync_project_branch` — after `create_or_get_version` returns `created`, enqueue build with version_id (D-01).
- `core_tools.DurableCPGQueue` — add version_id dedup index and retry-cap; worker updates `version.build_status` as state advances (D-02/03/05).
- `models.ProjectVersion` — add `build_status` column + metadata fields (D-02, D-06).
- `main.py` — mount REST routes + register MCP lifecycle tools on the same app (D-12, D-15).
- Phase 8 hooks: envelope/sanitization designed for later auth injection (D-16).

</code_context>

<specifics>
## Specific Ideas

- One call should get a codebase to a scheduled build: sync then build is automatic.
- An unchanged branch returns the existing ready version — no duplicate work, ever.
- REST and MCP must agree on `id`/`status`/`phase`/`queue_position` so an agent can switch transports without re-keying state.
- Cancelled versions stay in the catalog so provenance is preserved and a later sync/retry can rebuild.

</specifics>

<deferred>
## Deferred Ideas

- Authenticated project/version authorization, quotas, audit events, correlation IDs, metrics — Phase 8 (API-03/API-04).
- Webhook-driven syncing — deferred in Phase 5; v0.7 uses explicit update calls.
- Full multi-tenant worker isolation and key-management infrastructure — later hardening scope.

</deferred>

---

*Phase: 6-Durable CPG Lifecycle & Backend Contract*
*Context gathered: 2026-08-10*