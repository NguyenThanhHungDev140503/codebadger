# Stack Research

**Domain:** Version-to-version static-code comparison and bounded CPG change-impact analysis in a multi-tenant MCP service
**Researched:** 2026-09-17
**Confidence:** HIGH for the existing stack and integration boundary; MEDIUM for cross-language precision, which must be tested per Joern frontend.

## Recommendation in One Sentence

Build v0.8 as a small Python service layer plus parameterized CPGQL templates on top of the already pinned Joern worker fleet; do **not** add a graph database, vector store, diff SaaS, or a second web framework.

## Existing Primitives to Reuse

| Primitive | Evidence in CodeBadger | v0.8 use |
|---|---|---|
| Immutable version catalog | `ProjectVersionService`, `ProjectVersion` and `project_versions`; version IDs derive from project, 40-char SHA, and build config | Validate both IDs are from the same authorized project and are `ready`; use each version ID as its CPG hash. Keep comparison results derived, not a new mutable source of truth. |
| Durable CPG + worker lifecycle | `CpgGenerator` persists overlays; `QueryExecutor` wakes sleeping workers and serializes access per codebase | Run each side of a comparison against its existing CPG; do not rebuild CPGs to compare them. |
| Bounded execution | `QueryExecutor` clamps timeout to 300s, rows to 10,000, stdout to 5 MB and uses a 50-result default for `reachableByFlows`; `QueryLoader` escapes Scala values and clamps numeric placeholders | Create templates exclusively through `QueryLoader`; apply smaller feature-specific limits/depths before the global ceiling. Return `total`/`returned`/`truncated` and budget information. |
| Relationship and flow analysis | `call_graph.scala` already uses `callee`, `caller`, `callIn`, bounded BFS, and an explicitly labelled indirect-dispatch fallback; taint/slice templates use `reachableByFlows` | Extract a structured, cited impact subgraph, retaining an `evidence_kind` that distinguishes resolved graph edge, dataflow path, and indirect-dispatch heuristic. |
| Tenant/auth/audit contract | REST `/versions/{id}/context`, lifecycle MCP `version_context`, tenant checks, auth middleware, quotas and `AuditLogger` already exist | Put REST and MCP adapters over one comparison/impact service, preserving 404-on-unauthorized behavior and emitting separate audit events. |
| Test infrastructure | Unit contract tests for lifecycle/context/auth/audit and Docker-focused test command documented in `AGENTS.md` | Add fixture-driven tests for determinism, cross-tenant rejection, caps, and REST/MCP parity; live Joern checks stay focused. |

## Recommended Stack

### Core Technologies

| Technology | Version | Purpose | Why Recommended |
|---|---:|---|---|
| Python | >=3.10 (type checking targets 3.12) | Service orchestration, domain models, REST and MCP adapters | The entire catalog, authorization, worker coordination and tool surface already use Python. A new runtime would duplicate lifecycle and tenant policy. |
| Joern CLI / CPGQL | **4.0.594**, pinned in `Dockerfile` | Per-version source/AST/CFG/PDG/call graph queries and dataflow impact | It is the project’s canonical semantic source. Official CPGQL docs support `caller`, `callee`, `callIn`, `callOut`, and dataflow steps; the existing templates already prove the API path. Keep the image pin while v0.8 ships because Joern v4 changed its internal graph backend. |
| Eclipse Temurin JDK | **21**, pinned base image | Joern runtime | Joern upstream documents JDK 21 as the tested requirement; the current image already aligns. |
| FastMCP + MCP | `fastmcp>=3.4.2`, `mcp>=1.27.2` | Existing authenticated MCP contract | Reuse registration and schema generation; expose no independent comparison protocol. |
| Starlette/ASGI via FastMCP + Uvicorn | `uvicorn>=0.49.0` | Existing REST parity endpoints | Current REST routes are hosted through FastMCP/ASGI. Adding FastAPI would introduce a parallel routing and validation stack for no capability gain. |
| PostgreSQL | **16** container; `psycopg[binary,pool]>=3.3.4` | Version catalogue, source references, audit/cache and durable work | This is already the durable tenant-scoped catalog. Store only optional bounded comparison cache metadata here if measurement justifies it; do not persist a second graph. |
| Redis | **7** container; `redis>=8.0.0` | Cross-process coordination and durable queue support | `QueryExecutor` relies on cross-process per-codebase locking. It prevents concurrent impact traversals from piling onto the same worker. |

### Supporting Libraries and Modules

| Library / module | Version | Purpose | When to Use |
|---|---:|---|---|
| Pydantic | `>=2.13.4` | Stable request/response schemas for diff, change target and citation types | Define shared service DTOs and derive both MCP/REST boundary schemas from them; do not return raw Scala maps. |
| `src/tools/queries/QueryLoader` | in-repo | Escapes Scala string literals; clamps limits/depth; prevents template placeholder injection | Mandatory for every new Scala template. Only bare numeric/boolean placeholders already recognised by the loader should be added deliberately and tested. |
| `src/services/query_executor.QueryExecutor` | in-repo | Query serialization, server reactivation, timeout/error handling, output caps | Mandatory execution gateway; comparison/impact code must not instantiate Joern clients directly. |
| `src/utils/query_rendering.escape_scala_string` | in-repo | Scala literal escaping | Indirectly via `QueryLoader`; never interpolate paths, symbols, names, or signatures with f-strings. |
| `src/services/project_version_service.ProjectVersionService` | in-repo | Ownership/ready-state lookup | Resolve both versions through this service before CPGQL. Require same `project_id` for a two-version comparison unless a later explicitly authorized cross-project feature changes policy. |
| `src/services/audit_logger.AuditLogger` | in-repo | Request audit trail | Audit compare/impact success and safe failure outcomes at the adapter boundary, matching lifecycle routes. |
| OpenTelemetry API/SDK/OTLP | `>=1.42.1` | Existing tracing | Add spans/attributes for compare and impact latency, result counts, truncation and error class; never attach source code or unbounded symbol text. |
| pytest + pytest-asyncio | `9.0.3`, `1.4.0` | Contract and focused regression tests | Use deterministic miniature source fixtures for file/symbol delta and controlled CPG fixture for impact traversal. |

### Development Tools

| Tool | Purpose | Notes |
|---|---|---|
| Docker Compose | Reproduce MCP + Joern + Postgres + Redis topology | Use the project’s existing image and `JOERN_WORKER_MODE=pool`; do not build/rebuild CPGs as routine tests. |
| `docker run ... codebadger-mcp:latest pytest -q` | Focused regression verification | Run only the new focused tests first, as prescribed in `AGENTS.md`. |
| mypy / Black / isort / flake8 | Existing quality checks | New DTO and service code should preserve strict typing; current `mypy` target is Python 3.12 even though runtime floor is 3.10. |

## Implementation Shape

```text
REST compare/impact route     MCP compare/impact tool
             \                 /
              shared VersionComparisonService
                 |        |
   ProjectVersionService    AuditLogger / shared response DTOs
                 |
         QueryLoader templates -> QueryExecutor
                              -> ready CPG A / ready CPG B
                              -> Joern 4.0.594 workers
```

1. A **comparison service** validates ownership/readiness, obtains deterministic per-version file and symbol inventories, normalizes them in Python, and produces stable added/modified/deleted records with source locations and version digests.
2. An **impact service** consumes one cited changed symbol/location, executes structured call/dataflow templates only against the selected ready version, ranks/caps results, and marks evidence as exact or heuristic.
3. REST and MCP remain thin adapters. They must share input validation, error mapping, response DTOs, authorization check ordering, quota behavior, and audit event names.

For v0.8, retrieve both inventories at request time. Add a Postgres cache only after measurements show repeated full-inventory queries dominate latency; cache keys must include both version IDs, query-template revision, limit/depth and tenant visibility assumptions.

## Installation

No package installation is recommended for this milestone. The required stack is already present in `requirements.txt` and Compose.

```bash
# Focused verification after implementing v0.8 tests
docker run --rm -v "$PWD:/workspace" -w /workspace \
  codebadger-mcp:latest pytest -q tests/unit/services tests/unit/api
```

If a deliberate Joern upgrade is scheduled separately, update the Dockerfile pin, rebuild the image, and rerun the cross-language CPG fixture suite. It is not a v0.8 prerequisite.

## Alternatives Considered

| Recommended | Alternative | When to Use Alternative |
|---|---|---|
| Structured CPGQL templates + Python normalizer | Raw user-provided CPGQL | Never for this public feature. Raw queries defeat stable contract, tenant-safe bounds, and citation semantics. Keep raw CPGQL only in its existing constrained diagnostic surface. |
| Existing Joern CPGs | Neo4j / a separate graph database | Only if a later product requires long-lived cross-repository graph joins that Joern workers cannot serve. v0.8 compares two CPGs already held and managed by CodeBadger. |
| Existing Postgres version catalog | External Git diff service or a generic diff library as authoritative source | Only for a later source-text-only feature that must operate without CPGs. Here, version and CPG readiness must stay aligned; a new source of truth invites mismatches. |
| FastMCP/Starlette routes | FastAPI application | Only for a broad independent HTTP product rewrite. v0.8 needs parity beside existing routes, not duplicate middleware and OpenAPI ownership. |
| Request-time inventory | Persisted derived symbol index | Only after measured latency/worker cost establishes it. A new index entails invalidation by build config, Joern version, overlays and tenant authorization. |
| Existing OTLP instrumentation | A new observability vendor SDK | Only if operations adopt it platform-wide. Existing OpenTelemetry keeps the feature vendor-neutral. |

## What NOT to Use

| Avoid | Why | Use Instead |
|---|---|---|
| Unbounded `reachableByFlows`, BFS, or `.l` materialization | Dataflow expands rapidly and can monopolize the one-query-per-CPG worker; no global cap makes payloads non-deterministic. | Feature-specific `max_depth`, `max_nodes`, `max_paths`, byte budget, plus existing QueryExecutor limits and truncation fields. |
| Direct f-string CPGQL | Symbols and paths can alter Scala syntax or template semantics, and policy clamps are bypassed. | `QueryLoader.load()` plus its escaped string and numeric placeholder rules. |
| Diffing raw Joern internal node IDs across versions | Node IDs are build-local and do not form a stable cross-version identity. | Stable file path + qualified/signature/name + source span fingerprint; label unmatched/ambiguous mappings honestly. |
| Treating every caller/callee as an exact fact | Dynamic dispatch, callbacks, macros and incomplete frontend resolution may yield missing/heuristic edges. | Evidence kind and citation fields; retain the current address-taken fallback only as a labelled heuristic. |
| Joern version upgrade in this feature | Upstream Joern 4 changed from OverflowDB to FlatGraph, so a bump can change traversal behavior and serialized CPG compatibility. | Keep 4.0.594 pinned for v0.8; make future upgrades a separately tested migration. |
| Vector DB / embedding reranker | It does not establish deterministic version delta or graph evidence, while it adds an unrelated operational surface. | Defer to the existing RETR backlog after v0.8’s reliable structured baseline. |

## Stack Patterns by Variant

**If both versions are ready and have the same project/build configuration:**

- Use the shared comparison service and fetch structural inventories from each CPG.
- Because that maintains the immutable-version/CPG correspondence and avoids filesystem or Git-side assumptions.

**If a version is unauthorized, belongs to another project, or is not `ready`:**

- Stop before touching a worker; surface the established safe not-found/validation contract and audit the attempt.
- Because querying first could leak whether a CPG exists and wastes constrained worker capacity.

**If file/symbol inventory is too large for the request budget:**

- Use a deterministic sort order, return the first bounded page, and state `total`, `returned`, budget and `truncated`.
- Because stable incomplete output is actionable; implicit output clipping is not.

**If the changed target is a source location but not a uniquely resolvable method:**

- Return a source citation and a bounded file-level/nearby-symbol result, or a stable `TARGET_NOT_RESOLVED` response; do not invent a symbol identity.
- Because CPG node coverage and frontend fidelity vary by language and build configuration.

**If the request asks for dataflow impact:**

- Require a narrow resolved target and use persisted dataflow overlays with a smaller per-feature path/node/timeout budget than the global maximum.
- Because the codebase already identifies `reachableByFlows` as expensive and performs overlay persistence specifically to protect query workers.

## Version Compatibility

| Package / component | Compatible With | Notes |
|---|---|---|
| Joern **4.0.594** | Eclipse Temurin **21**, Ubuntu Noble | Exact project image combination. Upstream states JDK 21 is the tested requirement; do not lower the JDK image. |
| Joern v4 CPGQL | Existing `call_graph.scala`, dataflow templates and persisted overlays | v4’s FlatGraph migration means validate every template and fixture after any Joern upgrade. |
| Python >=3.10 | Pydantic v2 / current source annotations | The repository uses PEP 604 unions; avoid syntax or library changes that narrow compatibility below the published Python floor. |
| FastMCP >=3.4.2 + MCP >=1.27.2 | Existing registration and ASGI REST routes | Preserve this pairing during v0.8; no evidence supports an upgrade need for compare/impact. |
| PostgreSQL 16 + psycopg pool >=3.3.4 | Existing durable catalog | Reuse connections through `PostgresDBManager`; no schema is necessary unless a measured cache is introduced. |
| Redis 7 + redis-py >=8.0.0 | Existing coordinator / pool workers | The cross-process lock remains essential when compare/impact makes multiple CPG calls. |

## Research Notes and Validation Priorities

- `Dockerfile` pins Joern 4.0.594, while the upstream release page currently displays 4.0.592 as latest. Treat the checked-in pin as the compatibility baseline, not a signal to downgrade or automatically upgrade.
- The source already documents C/C++ macro/include sensitivity and an explicit indirect-call fallback. Cross-version symbol matching and impact tests must include at least one direct-call, callback/virtual-dispatch, and preprocessor-gated fixture.
- Joern official docs establish the traversal vocabulary, but they do not guarantee identical resolution across every frontend. Therefore the service contract should distinguish empty, unresolved, exact, and heuristic evidence rather than claiming global semantic completeness.

## Sources

- [CodeBadger Dockerfile](../../Dockerfile) — exact Joern 4.0.594, Temurin 21/Noble and Rust runtime pin (HIGH, checked-in implementation).
- [CodeBadger dependencies](../../requirements.txt) and [Compose topology](../../docker-compose.yml) — installed runtime, Postgres 16, Redis 7, worker-mode configuration (HIGH, checked-in implementation).
- [ProjectVersionService](../../src/services/project_version_service.py), [ContextRetrievalService](../../src/services/context_retrieval_service.py), [QueryExecutor](../../src/services/query_executor.py), [QueryLoader](../../src/tools/queries/__init__.py), and [call graph template](../../src/tools/queries/call_graph.scala) — reusability and bounds/security behavior (HIGH, checked-in implementation).
- [Joern CPGQL complex steps](https://docs.joern.io/cpgql/complex-steps/) and [call traversals](https://docs.joern.io/cpgql/calls/) — `caller`/`callee`/`callIn` and dataflow traversal semantics (HIGH, official docs checked 2026-09-17).
- [Joern repository](https://github.com/joernio/joern) and [releases](https://github.com/joernio/joern/releases) — JDK 21 requirement and Joern 4 FlatGraph migration/release cadence (HIGH, official upstream).

---
*Stack research for: CodeBadger v0.8 — Version Intelligence & Change Impact*
*Researched: 2026-09-17*
