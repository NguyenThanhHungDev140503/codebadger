# Feature Landscape

**Domain:** Secure, versioned code-context backend for AI agents
**Researched:** 2026-08-09
**Confidence:** HIGH for existing-platform dependencies; MEDIUM for product prioritization.

## Product Boundary

CodeBadger v0.7 should make a submitted source archive into a durable, named
**project version**, build its CPG asynchronously, and let an authenticated
agent request a small set of source-backed context passages.  The returned
context must always identify the project version and cite each passage's file,
line range, and retrieval reason.  It is a context service, not a general code
hosting product or an unbounded graph-query endpoint.

The existing platform already has the core build primitives: a Postgres job
queue with atomic claims and active-job deduplication, progress/status polling,
and a disk-cached CPG lifecycle.  v0.7 should wrap those primitives in a new
public catalog/lifecycle contract rather than replace them. [HIGH]

## Table Stakes

Features users expect. Missing = product feels incomplete.

| Feature | Why Expected | Complexity | Acceptance-oriented behavior / notes |
|---|---|---:|---|
| Authenticated archive submission | A backend receiving proprietary source must make ownership and ingress explicit. | High | `POST /projects/{project}/versions` accepts one allowed archive type over an authenticated interface; it returns `201` with immutable `project_id`, `version_id`, content digest, and a lifecycle URL. Reject missing identity, unsupported media type, malformed archive, over-limit compressed/uncompressed size, excessive file count, traversal paths, special files, or symlinks. Never expose an archive path or upload token in responses/logs. |
| Staged, canonical source snapshot | A version must be reproducible and safe to hand to a parser. | High | Extract into a per-upload temporary directory; validate every archive entry before copying to a final snapshot owned by `version_id`. Reject `..`, absolute paths, NUL/control characters, duplicate canonical paths, escaping symlinks, and files outside configured policy. Persist a manifest (relative path, byte size, SHA-256), source digest, detected/specified language, and ingest timestamps. Delete staging on success and failure. |
| Project and immutable version catalog | Agents must refer to the same code, even after another upload. | Medium | Create/list/read projects and versions. A version holds its source digest, manifest summary, CPG build ID/status, language/config, creation time, and parent/version label. A new upload never mutates an existing ready version; identical content for the same analysis configuration returns or references the existing version deterministically. |
| Asynchronous CPG build lifecycle | CPG construction is long-running and capacity-bound. | Medium | Submitting a version enqueues one durable build, returning without waiting. Status exposes a stable state (`queued`, `building`, `loading`, `ready`, `failed`), phase, queue position when queued, elapsed/deadline, retry count, and sanitized failure code/message. A duplicate submit does not start a second active build. `ready` only means a usable CPG exists, not merely that an archive was stored. |
| Retry and cancellation semantics | Users need a recoverable path after transient parser/worker failure. | Medium | An explicit retry creates/requeues work only for a terminal failed version and preserves attempt history; it does not overwrite a successful CPG. Interrupted running jobs are requeued on scheduler startup. Cancellation is allowed only before a worker begins parsing; it produces a terminal `cancelled` result and cleans partial CPG artifacts. (Cancellation requires a small extension beyond the current queue.) |
| Bounded context retrieval | Agents need direct answers, not raw graph dumps. | High | `POST /versions/{id}/context` requires a ready version and an explicit query/symbol; it returns a bounded response with a documented maximum item/byte/token budget, `truncated` when applicable, and no raw CPGQL. Each item includes `path`, start/end line, snippet, symbol (when known), and why it was selected. |
| Hybrid symbol, lexical, and graph expansion | Name lookup alone misses callers, callees, and data-flow-adjacent code. | High | Retrieval resolves exact/qualified symbols first; lexical search supplies candidates when symbols are absent; graph expansion adds a capped relationship neighborhood (for example callers/callees or relevant flow nodes). Rank and deduplicate passages, then fetch source spans. The response identifies which method(s) produced each item. Start without embeddings, as the project boundary specifies. |
| Source citations and version provenance | An agent must be able to inspect or quote retrieved code safely. | Medium | Every context response includes `project_id`, `version_id`, immutable digest, retrieval timestamp, and per-item citation stable within the version: relative path plus 1-based inclusive line range. A client can request a cited span only if it belongs to the version and configured maximum-span limits. |
| REST/MCP parity | Existing MCP users and backend clients need the same lifecycle concepts. | High | REST is the contract of record; thin MCP tools call the same application services and return the same IDs/status/citation schema. No endpoint/tool accepts host-local paths for public archive ingestion. Contract tests prove parity for upload/version status and context retrieval. |
| Tenant-bound authorization and audit trail | The current deployment is explicitly single-tenant with no built-in auth, which is insufficient for archive upload. | High | Authenticate every new REST/MCP lifecycle/context call; authorize project/version access before metadata, status, source span, or context is returned. Record actor, project/version, operation, outcome, request/correlation ID, and timestamp without raw source or credentials. v0.7 may implement one tenant/trust domain, but ownership checks must be in the service boundary so a later multi-tenant model is possible. |
| Quotas, backpressure, and observability | Parsing untrusted archives can exhaust disk, CPU, and memory. | Medium | Enforce upload/project quotas before finalization and return a typed retryable response for a full build queue. Expose version counters by lifecycle state, admission rejection reason, queue depth, build duration, retrieval latency, and truncation count. Operators can diagnose a failed build without receiving filesystem paths or sensitive source. |

## Differentiators

Features that set product apart. Not expected, but valued.

| Feature | Value Proposition | Complexity | Notes |
|---|---|---:|---|
| Explainable CPG-grounded context | Makes agent context auditable: results show not only matching text but graph evidence such as call/data-flow relationships. | High | Return compact `relationship` evidence (`caller_of`, `callee_of`, `flow_adjacent`, etc.) next to citations. Keep traversal depth/result count capped and allow only approved relationship types. |
| Version-aware comparison context | Lets an agent reason about a regression or security change with stable inputs. | High | Retrieve the same symbol/path across two ready versions and cite both. Defer until the base catalog and one-version context contract are proven; it depends on snapshots, manifests, and stable citation format. |
| Retrieval coverage/quality signal | Helps agents know when a CPG may have parsed little or none of a project. | Medium | Surface user-method count, indexed-file count, excluded/unsupported file count, and a `partial_analysis` warning in version readiness and context responses. The backend already records a user-method count as a CPG coverage sanity check. |
| Deterministic retrieval profile | Makes CI and agent runs reproducible rather than relying on opaque ranking. | Medium | Persist a named retrieval profile/version (lexical fields, relationship types, caps) with each request/response. Same version + query + profile produces stable ordering where underlying CPG results are stable. |

## Anti-Features

Features to explicitly NOT build in v0.7.

| Anti-Feature | Why Avoid | What to Do Instead |
|---|---|---|
| Unrestricted raw CPGQL for untrusted agents | Joern uses a Scala interpreter; the existing denylist is explicitly defense-in-depth, and raw queries can reach the shared playground. | Keep raw CPGQL internal/admin-only. Expose a curated ContextService with fixed, parameterized retrieval operations and hard output/traversal limits. |
| Embedding/vector-database-first retrieval | Adds an index, model, ingestion pipeline, and relevance failure modes before the product proves its lexical/CPG contract; it is out of scope in the project brief. | Build explainable symbol + lexical + graph retrieval first. Re-evaluate embeddings after recorded retrieval quality/latency data exists. |
| Mutable versions or “replace archive” | Invalidates citations, cached CPGs, reproducibility, and audit records. | Create a new immutable version for every distinct snapshot; mark or archive old versions through a separate lifecycle policy. |
| Git hosting, pull-request sync, web IDE, or full repository browser | Broadens the trust surface and competes with established SCMs; it does not advance upload-to-context. | Accept a bounded archive, retain a manifest, and return cited snippets only. Add SCM connectors in a later milestone if evidence supports it. |
| Public multi-tenant marketplace | The current Docker-socket deployment is root-equivalent on its host and the current system is documented as single-tenant. | Treat v0.7 as a protected trust domain on a dedicated host, with authenticated project ownership and upstream rate limiting. Revisit multi-tenancy only with stronger worker isolation and per-tenant storage boundaries. |
| Arbitrary host paths / server-side URL fetch for new REST API | Reintroduces local-file exposure and SSRF-style ingress risks. | Public ingestion is archive upload only. Existing trusted deployment modes remain separately configured, never silently exposed through the new API. |

## Feature Dependencies

```text
Authentication + project authorization
  -> archive admission -> staged extraction -> manifest/content digest
  -> immutable project version -> durable CPG job -> lifecycle/status API
  -> ready CPG + source snapshot -> bounded lexical/symbol retrieval
  -> graph expansion -> cited, ranked ContextService response
  -> REST/MCP parity

Version catalog + stable citation schema -> version comparison context
Build telemetry + manifest/index stats -> retrieval coverage/quality signal
```

## MVP Recommendation

Prioritize:

1. **Secure archive ingestion and immutable project/version catalog.** Establish the ownership, snapshot, digest, manifest, and access-control boundaries before making source queryable.
2. **Version-to-durable-build lifecycle REST API.** Reuse Postgres queue semantics; surface reliable status, deduplication, backpressure, sanitized failure data, and explicit retry.
3. **Bounded, cited hybrid context retrieval.** Deliver exact symbol lookup plus lexical candidates and one capped graph-neighborhood expansion through a single ContextService, surfaced in REST and MCP.

Defer:

- **Version comparison context** until version identity and citation stability have production tests.
- **Embeddings/vector search** until lexical + graph retrieval is measured as inadequate.
- **Multi-tenant isolation and arbitrary raw-query access** because the current host/worker trust model cannot safely support them.
- **Automatic repository synchronization and web UI** because they do not unblock agent context retrieval.

## Sources

- [Project brief: v0.7 scope, decisions, and exclusions](../PROJECT.md) — HIGH confidence; current project authority.
- [Architecture: durable queue, CPG lifecycle, memory-aware worker behavior](../../docs/architecture.md) — HIGH confidence; repository architecture documentation.
- [Security: trust boundaries, source staging controls, no built-in auth, raw CPGQL residual risk](../../docs/security.md) — HIGH confidence; repository threat model.
- [Usage: asynchronous generation and readiness polling](../../docs/usage.md) — HIGH confidence; current user-facing workflow.
- [Postgres job store implementation](../../src/utils/postgres_job_store.py) and [core durable queue/status implementation](../../src/tools/core_tools.py) — HIGH confidence; current source confirms atomic claims, deduplication, restart requeueing, queue positions, deadline reconciliation, and public status fields.
