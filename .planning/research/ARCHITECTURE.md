# Architecture Research: Version Intelligence & Change Impact

**Domain:** Multi-tenant, CPG-backed version comparison and impact analysis
**Researched:** 2026-09-17
**Confidence:** HIGH for existing CodeBadger integration points; MEDIUM for Joern query behavior across every supported language.

## Executive Recommendation

Keep CodeBadger as a modular Python service. Each immutable `ProjectVersion.id` is already the CPG/cache key and is the stable boundary for both sides of a comparison. Add one orchestration layer, `VersionIntelligenceService`, which owns tenant-aware version-pair validation, deterministic source/symbol comparison, bounded CPG impact queries, citations, and response budgets. Its narrow dependencies are `ProjectVersionService` for catalog ownership, an immutable snapshot reader for file contents, and `QueryExecutor` for CPG facts.

Do not compare raw Joern node IDs across CPGs: node identity is local to one CPG. Match symbols with a stable attributed key (normalized path + full name/signature + declaration span) and return an ambiguity marker where necessary. Preserve the existing context contract’s bounding model: clamp inputs, report `total`/`returned`/`available`, set `truncated`, and expose stable domain error codes.

## Current Architecture Evidence

| Existing component | Current responsibility | v0.8 integration point |
|---|---|---|
| `ProjectVersionService` | Tenant-scoped immutable project/version catalog and `ready` state | Resolve both version IDs, assert same project and tenant before any read. |
| `ProjectVersion` | Commit SHA, content digest, manifest, `source_snapshot_ref` | Diff only immutable snapshots/references; never a mutable working tree. |
| `CodebaseTracker` + `QueryExecutor` | Maps version ID/codebase hash to CPG; per-CPG query lock, cache, timeout, auto-wake | Query each version CPG independently; reuse its lock and budget behavior. |
| Joern CPG generation | Persists CPG and dataflow overlay | Run impact only when relevant catalog versions are `ready`. |
| `ContextRetrievalService` | Bounded cited lookup against one ready version | Reuse citation/budget conventions; do not turn it into pair orchestration. |
| REST + lifecycle MCP tools | Version lifecycle/context over two transports | Add thin adapters over one shared service/schema. |
| Auth/rate-limit/correlation/audit | Tenant identity, request cap, trace, audit log | Preserve middleware and add comparison/impact audit events. |

### Important Integration Finding

`main.py` creates `ProjectVersionService`, `QueryExecutor`, and `CodeBrowsingService`, then registers REST/MCP lifecycle routes. It does **not** instantiate or register `ContextRetrievalService` in `services`, although both transports look it up. Tests inject it explicitly, masking this runtime `503` path. Phase 9 should wire the existing context service and new intelligence service in the lifespan before registering tools/routes.

## Recommended Architecture

### System Overview

```text
Clients (REST / MCP)
        |
        v
+---------------- Request boundary -----------------+
| correlation -> auth -> rate limit -> sanitizer     |
| REST route                MCP lifecycle tool       |
+-------------------+------------------+-------------+
                    | same DTO / response schema
                    v
+----------------------------------------------------+
| VersionIntelligenceService                         |
|  1. authorize + validate pair                      |
|  2. source/file diff + symbol matching             |
|  3. CPG impact collector + deterministic ranking   |
|  4. citation/budget/truncation response composer   |
+---------+-------------------+----------------------+
          |                   |
          v                   v
 ProjectVersionService   SourceSnapshotReader
  Postgres catalog       immutable snapshot/ref
          |                   |
          +---------+---------+
                    |
                    v
             QueryExecutor (one version/CPG at a time)
                    |
                    v
           JoernServerManager -> Joern workers/CPGs

Cross-cutting: Postgres cache + Redis query locks; AuditLogger/correlation ID
```

### Component Boundaries

| Component | Responsibility | Must not own |
|---|---|---|
| `VersionIntelligenceService` | Shared domain contract, same-project/tenant + ready-state preconditions, response composition | HTTP/MCP parsing, token decoding, user raw CPGQL |
| `VersionPairResolver` | Fetch base/target; conceal unauthorized IDs; reject cross-project pairs | Joern query or snapshot walks |
| `SnapshotDiffAdapter` | Normalized immutable file paths/bytes; deterministic add/modify/delete summary | Auth, CPG traversal, mutable clone access |
| `SymbolDiffAdapter` | CPG declaration inventory, normalization and changed/ambiguous symbols | Impact expansion, transport serialization |
| `ImpactCollector` | Approved caller/callee and bounded dataflow templates for selected changes | Whole-version diff or arbitrary CPGQL |
| `CitationBudgeter` | Stable citations (`version`, digest/commit, file, span, symbol); row/byte/time caps | Data acquisition |
| REST/MCP adapters | Parse transport fields, call service, map domain code, audit read | Duplicated business rules |

## Recommended Project Structure

```text
src/
├── services/
│   ├── version_intelligence_service.py  # pair preconditions + use cases
│   ├── version_pair_resolver.py         # catalog/tenant/ready validation
│   ├── snapshot_diff_adapter.py          # immutable file comparison
│   ├── symbol_diff_adapter.py            # CPG symbol identity/comparison
│   ├── impact_collector.py               # vetted callers/callees/dataflow
│   └── context_retrieval_service.py      # existing single-version lookup
├── tools/lifecycle_tools.py              # MCP adapter
├── tools/queries/                        # reviewed, parameterized CPGQL
└── api/rest_routes.py                    # REST adapter
tests/
├── unit/services/test_version_intelligence_service.py
├── unit/services/test_snapshot_diff_adapter.py
├── unit/services/test_symbol_diff_adapter.py
├── unit/services/test_impact_collector.py
├── unit/api/test_version_intelligence_api.py
└── integration/test_version_intelligence_parity.py
```

Keep query text in vetted templates and clamp numeric limits through `QueryLoader`; the public contract must not expose raw user CPGQL.

## Architectural Patterns

### Immutable-pair application service

**What:** Every request names `base_version_id` and `target_version_id`; resolve both under the acting tenant, require same `project_id` and `ready` state, then analyze.

**Trade-off:** Two catalog reads up front, in exchange for reproducible citations and no cross-tenant/repository disclosure.

```python
pair = pair_resolver.resolve_ready_same_project(
    base_version_id, target_version_id, owner_scope=tenant_id
)
return intelligence.compare(pair, limits=limits)
```

### Source-first diff, graph-enriched impact

**What:** Compare file membership/content and source-attributed symbols from immutable snapshots first; use CPG only to enrich a selected changed symbol with callers/callees/dataflow evidence.

**Trade-off:** Two analysis paths, but precise file changes even where a graph lacks a declaration. Return `changed`, `unresolved`, or `ambiguous`, not false certainty.

### Approved bounded traversal

**What:** `ImpactCollector` renders only fixed caller/callee/dataflow templates, applies a deadline and response budget before/after every query, and returns partial facts honestly.

**Trade-off:** Results can omit impact, but no client can monopolize a per-CPG Joern lock. Preserve `QueryExecutor`’s dataflow cap and timeout behavior rather than bypass it.

## Data Flow

### Compare Two Versions

```text
REST/MCP -> middleware attaches tenant + correlation ID
  -> resolve pair through ProjectVersionService
  -> same project + both ready? otherwise stable not-found/precondition error
  -> SnapshotDiffAdapter reads two immutable references
  -> SymbolDiffAdapter inventories each CPG independently via QueryExecutor
  -> CitationBudgeter sorts, caps, marks truncated
  -> canonical response + AuditLogger("version.compare_read")
```

### Explain Impact

```text
selected changed symbol + pair
  -> pair and symbol-citation validation
  -> ImpactCollector renders approved direct caller/callee query
  -> optional separately capped dataflow query
  -> QueryExecutor serializes and auto-wakes chosen CPG
  -> direct evidence ranks before indirect/dataflow
  -> citations + per-section budget + AuditLogger("version.impact_read")
```

### Response Contract

Use a transport-neutral object containing pair metadata (IDs, commits/digests), deterministic `files`, `symbols`, `impact`, `citations`, `budget`, cap metadata and `warnings`. Domain error codes should include `VERSION_NOT_FOUND`, `VERSION_NOT_READY`, `VERSION_PROJECT_MISMATCH`, `SYMBOL_AMBIGUOUS`, `CPG_UNAVAILABLE`, and `IMPACT_BUDGET_EXCEEDED`. REST maps codes to HTTP while MCP preserves the same machine-readable code.

## Phased Build Order

1. **Phase 9 — pair + deterministic diff foundation:** service, DTO/citation schema, pair validation, source-file diff, first symbol matching, bounds/cache-key versioning, and lifespan wiring for `ContextRetrievalService`; then REST/MCP compare adapters.
2. **Phase 10 — CPG impact enrichment:** vetted caller/callee templates first; optional dataflow after it, with stricter caps/timeouts and partial-result warnings; reuse QueryExecutor locks and cache namespace.
3. **Phase 11 — contract hardening/evaluation:** diff and graph fixtures, REST/MCP parity, tenant/admin isolation, quotas/rate-limit, audit/correlation assertions, and sleeping/degraded CPG tests.

Identity and source attribution must precede impact; a shared contract must precede parity testing.

## Test Strategy

| Layer | Essential cases |
|---|---|
| Pair resolver | Unauthorized target hidden; cross-project rejected; non-ready rejected; admin semantics preserved |
| Snapshot diff | Sorted paths; add/delete/modify; binary/unreadable warning; no mutable-repo read |
| Symbol diff | Renamed/moved/overloaded/ambiguous symbols, missing spans, deterministic citations/order |
| Impact collector | Projection, service failure, dataflow deadline/row/byte cap, no raw interpolation, cache keyed by both version IDs + schema version |
| API/MCP contract | Identical success shape/error code; input bounds; OpenAPI kept in sync |
| Integration | Two tenants + admin, audit/correlation, rate/quotas, sleeping CPG auto-wake, partial result under load |

Use bundled core/libxml2 fixtures for live CPG checks and a mocked `QueryExecutor` for units; do not regenerate large CPGs for comparison coverage.

## Anti-Patterns

### Comparing CPG node IDs across versions

**Why wrong:** Joern IDs are local to an independently built graph.

**Instead:** Normalized, source-attributed symbol keys with two-version citations and ambiguity states.

### Public raw-CPGQL compare endpoint

**Why wrong:** It defeats escaping, clamping, bounded output, predictable authorization, and audit.

**Instead:** Named compare/impact modes backed by vetted templates.

### Expanding `ContextRetrievalService` into a monolith

**Why wrong:** It mixes pair diff, CPG impact, source walking and transport concerns, causing drift.

**Instead:** A dedicated intelligence service with narrow adapters; keep context retrieval focused.

### Presenting a capped traversal as complete impact

**Why wrong:** High-fanout call and dataflow graphs are incomplete under safe limits.

**Instead:** Return ranking, bounds and `truncated`; use later pagination/refined scope rather than unsafe defaults.

## Scaling Considerations

| Scale | Architecture adjustment |
|---|---|
| Current/small tenant set | Synchronous source diff + serialized per-CPG impact is appropriate; reuse Postgres cache and Redis locks. |
| Many concurrent compares | Cache deterministic results by `(base_id, target_id, schema_version, options)`; isolate/TTL dataflow caches; never cache authorization decisions. |
| Large snapshots/high fan-out | Add a durable derived-comparison job only when measured synchronous budgets block product use; retain immutable pair/options/digests and the same response schema. |

The first likely bottleneck is Joern worker/dataflow contention, not source comparison. Tighten traversal and summarize results before adding worker topology.

## Sources

- [Current CodeBadger architecture](../../docs/architecture.md) — Joern lifecycle, Redis locks, Postgres cache/queue.
- [Version catalog](../../src/services/project_version_service.py) — immutable tenant-scoped versions.
- [Bounded context retrieval](../../src/services/context_retrieval_service.py) — citation and item/byte budgets.
- [Query executor](../../src/services/query_executor.py) — per-CPG lock, auto-wake, timeout/dataflow constraints.
- [REST routes](../../src/api/rest_routes.py) and [MCP lifecycle tools](../../src/tools/lifecycle_tools.py) — transport integration.
- [Security parity test](../../tests/integration/test_security_parity.py) — tenant/admin, quota, audit and correlation pattern.

---
*Architecture research for CodeBadger v0.8 Version Intelligence & Change Impact — 2026-09-17*
