# Domain Pitfalls

**Domain:** Codebase upload, versioned Joern CPG lifecycle, and cited AI-context retrieval backend  
**Researched:** 2026-08-09  
**Overall confidence:** HIGH for repository/security and queue risks; MEDIUM for product-contract and retrieval-quality risks

## Critical Pitfalls

### 1. Archive extraction becomes a host filesystem primitive
**What goes wrong:** The service calls `extractall`, trusts member names or MIME/extension, follows symlinks/hardlinks, or writes before enforcing cumulative limits. A crafted archive can escape staging (`../` or absolute paths), overwrite files, create devices/FIFOs, or exhaust disk/CPU through compression bombs. Duplicate paths after Unicode/separator normalization can also cause manifest/CPG disagreement.
**Why it happens:** Archive libraries make extraction look atomic and archive metadata is mistaken for a security boundary.
**Consequences:** Source overwrite, secret disclosure, denial of service, poisoned CPGs, and citations that do not match the uploaded bytes.
**Prevention:** Stream to a server-generated mode-0700 staging directory; inspect every member before writing; accept regular files/directories only; reject absolute/traversal paths, links, special files, duplicate normalized names, excessive depth/count, compression ratio, per-file and total uncompressed limits. Compute a manifest and SHA-256 from the extracted snapshot, then atomically promote it under server-owned project/version IDs.
**Detection:** Alerts on extraction-limit rejections, unexpected filesystem entries, staging growth, orphan directories, and manifest/checksum mismatch. Fuzz ZIP/tar fixtures and restart during extraction in tests.
**Phase placement:** Secure ingestion (must precede any queue/Joern integration).

### 2. Upload and CPG build are not one transaction
**What goes wrong:** Metadata is committed while promotion/enqueue fails, or a worker starts from a still-changing staging path. Conversely, a promoted source has no catalog row after a crash.
**Why it happens:** Postgres transactions cannot atomically include filesystem rename and Joern execution.
**Consequences:** Versions stuck forever in `building`, jobs pointing at deleted paths, duplicate builds, and irreproducible context citations.
**Prevention:** Use a recoverable two-step protocol: stage → validate/manifest → atomic rename to immutable final snapshot → commit catalog + durable job linkage; never expose staging to workers. On startup reconcile staging/final directories and catalog rows, mark incomplete versions repairable/failed, and garbage-collect only with an explicit ownership check. Keep one monotonic version state machine and idempotency key.
**Detection:** Reconciliation metrics (orphan bytes, versions without jobs, jobs without versions), invariant checks, and crash/restart fault-injection tests at every boundary.
**Phase placement:** Catalog foundation and ingestion; repeated in lifecycle hardening.

### 3. Public API exposes paths, hashes, or raw CPGQL as authority
**What goes wrong:** Clients submit filesystem paths or CPG hashes that are accepted as identity, or REST context routes pass arbitrary CPGQL into Joern. Existing raw-query denylisting is defense-in-depth, not a sandbox.
**Why it happens:** Reusing v0.6 tool parameters is faster than introducing project/version authorization and fixed query templates.
**Consequences:** Cross-project reads, host-path traversal, query/code execution inside Joern, source/CPG disclosure, and unstable citations.
**Prevention:** Use opaque server-issued project/version IDs; resolve and authorize IDs inside `ContextService`; keep CPGQL behind admin/internal policy and named parameterized templates. Mount only version-specific artifacts where feasible, disable raw query for untrusted callers, and preserve timeout/row/output caps.
**Detection:** Security tests attempting path/hash substitution, IDOR across projects, Scala-string injection, and obfuscated raw-query escape; audit logs must record principal, version, template, and budget without source secrets.
**Phase placement:** API/auth foundation and context-service hardening.

### 4. Queue state and Joern state diverge on crash, retry, or timeout
**What goes wrong:** A process restart requeues a `running` DB job but the version remains `building`; a late worker marks a retried job `ready`; automatic retries create concurrent CPG builds; a query timeout kills a loading server and corrupts an otherwise valid import.
**Why it happens:** Existing durable jobs and CPG tracker have separate state stores and timeout semantics. `FOR UPDATE SKIP LOCKED` prevents double claim, but does not provide a complete lease/fencing protocol.
**Consequences:** Duplicate expensive builds, stale CPG overwrites, permanent false failures, memory spikes, and status endpoints that lie.
**Prevention:** Add job attempt/lease or fencing token, version-scoped output paths, compare-and-set terminal transitions, and one-active-build uniqueness per version. Define bounded retry/backoff versus terminal failure, cancellation semantics, and worker shutdown behavior. Treat `LOADING/GENERATING` specially in query timeout handling; never kill an import merely because a query deadline elapsed.
**Detection:** Metrics for stale leases, late completions, duplicate attempts, and state invariant violations; integration tests for crash-after-claim, timeout-during-load, retry races, and Compose redeploy.
**Phase placement:** Version build lifecycle, then operations hardening.

### 5. Resource admission is bypassed by uploads or parallel jobs
**What goes wrong:** Upload limits cover bytes but not number of queued builds, extracted file count, disk occupancy, or total CPG memory. A REST worker loop or per-request task bypasses the existing Postgres queue, Redis lock, cgroup caps, and backpressure.
**Why it happens:** HTTP responsiveness encourages fire-and-forget tasks; queue depth is mistaken for total resource control.
**Consequences:** Disk exhaustion, Postgres connection pressure, Joern OOM, host instability, and noisy-neighbor starvation.
**Prevention:** Reuse `DurableCPGQueue`, enforce admission before staging and enqueue, cap per-principal/project bytes and active jobs, reserve disk, and surface `202`/`429`/`503 queue_full` explicitly. Keep build worker concurrency and heap within the configured Joern memory budget; rate-limit retrieval as well as uploads.
**Detection:** Monitor staging bytes, queue age/depth, CPG disk usage, RSS/heap, Postgres pool saturation, and rejection reasons. Load-test worst-case archive/build mixes.
**Phase placement:** Ingestion quotas and lifecycle/operations hardening.

## Moderate Pitfalls

### 6. Version identity is mutable or content hashing is incomplete
Changing a label, source root, ignored files, frontend flags, or generated overlays without changing the fingerprint can reuse the wrong CPG. Hash the canonical manifest plus build options; make ready artifacts immutable and key caches by version fingerprint and authorization scope.

### 7. Authentication is added only to REST or only to MCP
FastMCP's mounted ASGI app requires the MCP lifespan to be passed to the host FastAPI app for session management; middleware state must be consistently propagated. Protect both transports with the same principal/scope checks, reject missing/invalid credentials, and test direct mounted paths and WebSocket/streaming variants. (FastMCP official docs, HIGH: https://github.com/prefecthq/fastmcp/blob/main/docs/integrations/fastapi.mdx)

### 8. Error/status contract leaks internals or hides lifecycle state
Returning `connection refused`, host paths, Joern stderr, or raw exception text leaks deployment details; returning generic `500` for `queued`, `building`, `sleeping`, and `failed` makes agents retry incorrectly. Define stable DTOs and error codes (`queued`, `building`, `ready`, `failed`, `queue_full`, `version_not_ready`), redact paths/secrets, include retry hints and correlation IDs.

### 9. Source cleanup invalidates citations or future reactivation
The current configuration can delete ephemeral source after CPG generation, while context responses need source excerpts and citations. Retain an immutable snapshot (or a separately durable, checksummed excerpt store) until retention/deletion policy permits removal; do not run CPG GC as if it were source retention.

### 10. Retrieval returns plausible but incorrect context
Lexical hits can outrank exact symbols; graph expansion can explode; stale CPGs can be queried after a version update; truncation can silently remove the evidence an agent needs. Use deterministic tiering (exact symbol/file → lexical → bounded graph), deduplication, global byte/item/token budgets, explicit `truncated`, and citations carrying version ID, repo-relative path, line range, and reason. Cache only with normalized query/options + version fingerprint.

### 11. Cross-version and cross-principal cache contamination
Caching by query text or legacy codebase hash alone can serve another version or tenant. Include immutable version fingerprint, retrieval options, and principal authorization scope in cache keys; re-authorize on cache hits and invalidate on deletion.

### 12. Language/frontend and repository edge cases are treated as generic failures
Joern frontends differ in flags, generated/ignored files, compile databases, and supported languages. Auto-detection can produce empty or misleading CPGs. Persist detected language, frontend/options, manifest statistics, and a sanitized build diagnostic; validate unsupported/ambiguous uploads before enqueue and expose a clear `unsupported_language`/`empty_cpg` state.

## Minor Pitfalls

### 13. Filename and path normalization differs between manifest, DB, and citations
Normalize separators, Unicode, case policy, and newline handling once; store repository-relative POSIX paths and use the same canonicalizer for extraction, hashing, source reads, and response citations.

### 14. Retention/GC races with active queries
Deleting a version while a Joern query or source read is active yields missing-file errors or dangling citations. Mark `deleting`, block new work, wait for active references/jobs, then remove artifacts; make deletion idempotent and auditable.

### 15. Observability records sensitive source data
Logging archive names, tokens, source snippets, full queries, or Joern errors can exfiltrate secrets. Log IDs, sizes, hashes, state transitions, bounded error classes, and correlation IDs; apply redaction before structured logs and traces.

### 16. API retries create accidental duplicates
Clients retry timeouts and receive a second version/job. Require an idempotency key for upload/build submission, enforce unique `(project, content_sha256)` and active-job constraints, and return the existing resource with a duplicate indication.

## Phase-Specific Warnings

| Phase topic | Likely pitfall | Mitigation |
|---|---|---|
| Catalog/API foundation | IDOR, mutable identities, REST/MCP drift | Opaque IDs, principal scope on every repository query, shared application services and DTO/error contract |
| Secure archive ingestion | Traversal, links, bombs, disk exhaustion | Pre-extraction inspection, regular-file-only extraction, cumulative limits, private staging, manifest/checksum, quotas |
| Async CPG lifecycle | Duplicate/stale workers and split-brain status | Durable queue reuse, leases/fencing, CAS state transitions, version-scoped outputs, startup reconciliation, bounded retries |
| Context retrieval | Wrong/stale evidence, graph/result explosion, source unavailable | Immutable retained snapshots, deterministic hybrid ranking, bounded templates/budgets, explicit truncation and citations |
| Auth/deployment hardening | Docker socket/root-equivalent host compromise; unauthenticated endpoint | Dedicated host, reverse-proxy/mTLS/JWT, same auth on REST+MCP, disable/gate raw CPGQL, restrict Joern mounts/egress |
| Operations/retention | GC races and unbounded storage | Separate source/CPG retention, active-reference draining, artifact accounting, orphan reconciliation and alerts |

## Sources

- [Project scope, constraints, and key decisions](../PROJECT.md) — HIGH
- [Repository threat model and existing controls](../../docs/security.md) — HIGH
- [Architecture integration seams and lifecycle proposal](./ARCHITECTURE.md) — HIGH for repository facts; MEDIUM for proposed contract
- [Durable Postgres job store](../../src/utils/postgres_job_store.py) — HIGH
- [QueryExecutor timeout/lock/auto-wake behavior](../../src/services/query_executor.py) — HIGH
- [FastMCP FastAPI integration and lifespan](https://github.com/prefecthq/fastmcp/blob/main/docs/integrations/fastapi.mdx) — HIGH (Context7-verified 2026-08-09)
- [FastMCP middleware request state](https://github.com/prefecthq/fastmcp/blob/main/docs/servers/middleware.mdx) — HIGH (Context7-verified 2026-08-09)

## Research Gaps

- Exact archive formats, limits, malware-scanning requirement, retention duration, and tenant/auth provider remain product decisions.
- Joern version-specific frontend behavior and CPG serialization compatibility should be checked during the lifecycle phase against the pinned image/version.
- The repository has no current REST identity model; authentication, authorization, and mounted FastMCP routing need phase-specific design and integration tests.
