# Project Research Summary

**Project:** CodeBadger
**Milestone:** v0.8 — Version Intelligence & Change Impact
**Domain:** Authenticated, multi-tenant CPG-backed version comparison and bounded change-impact analysis for AI agents
**Researched:** 2026-09-17
**Confidence:** HIGH for repository integration and security constraints; MEDIUM for cross-language Joern fidelity and operational thresholds.

## Executive Summary

CodeBadger v0.8 should be an evidence-backed agent workflow: compare two immutable, ready snapshots from one authorized project, then explain the bounded structural and data-flow impact of one selected change. It is not a Git/PR product, a generic semantic-diff engine, or an arbitrary graph-query endpoint. Experts build this by treating the immutable version pair as the authority, deriving a deterministic source-first diff, and using CPG facts only to enrich a selected change.

The recommended implementation is one Python `VersionIntelligenceService` with thin REST and MCP adapters. It reuses `ProjectVersionService` for concealed tenant-scoped pair resolution, immutable snapshot references for files, `QueryLoader` plus `QueryExecutor` for vetted CPGQL, and the existing audit, quota, correlation, Redis-lock, and Joern-worker controls. Do not add a graph database, vector store, external diff service, second web framework, or persistent derived index in this milestone.

The principal risks are invalid pair comparison, unstable cross-CPG identities, runaway dataflow traversals, and transport-security drift. Mitigate them with same-project/ready/build-compatible pair validation before any worker access; stable source-attributed symbol keys rather than Joern node IDs; explicit traversal and response budgets with truthful partial states; and a single domain contract tested through both REST and MCP. Phase 9 must also correct the discovered production wiring gap for `ContextRetrievalService`, which tests currently mask.

## Key Findings

Detailed evidence is in [STACK.md](STACK.md), [FEATURES.md](FEATURES.md), [ARCHITECTURE.md](ARCHITECTURE.md), and [PITFALLS.md](PITFALLS.md).

### Recommended Stack

No new platform technology is warranted. The current Python service, FastMCP/ASGI routes, PostgreSQL catalog, Redis coordination, pinned Joern 4.0.594/JDK 21 workers, OpenTelemetry, and pytest fixture suite form the complete v0.8 stack. The graph report supports this: `ProjectVersionService`, `QueryExecutor`, `PostgresDBManager`, and `JoernServerManager` are already highly connected core abstractions, while no import cycles were found.

**Core technologies:**

- **Python service layer + Pydantic DTOs:** create one transport-neutral pair/diff/impact contract without a second runtime or routing framework.
- **`ProjectVersionService` + immutable source snapshots:** authorize and validate ordered version pairs; source manifests/snapshots are the authoritative file-diff evidence.
- **Joern 4.0.594 through `QueryLoader` and `QueryExecutor`:** gather declaration/call/dataflow facts using reviewed templates, escaping, clamps, locks, auto-wake, and existing global caps.
- **PostgreSQL 16 + Redis 7:** retain catalog persistence and per-CPG coordination; defer a comparison cache until measured demand proves it necessary.
- **FastMCP/ASGI REST, audit/correlation/quota middleware:** expose exactly one behavior via REST and MCP; no parallel business logic.

Keep the checked-in Joern/JDK pin throughout v0.8. A Joern upgrade changes traversal/CPG behavior and is a separate migration, not a prerequisite.

### Expected Features

**Must have (table stakes):**

- **Same-project, tenant-scoped ready pair resolution** — authorize both opaque version IDs before checking their relationship; conceal foreign/missing IDs consistently.
- **Deterministic file and symbol change inventories** — add/delete/modify in stable order, source citations on both sides, stable totals/returned/truncated metadata.
- **Stable symbol identity with honest ambiguity** — normalize path, kind, qualified name/signature, span, and fingerprint; never join CPG-local node IDs or silently label heuristic matches as exact.
- **Selected-change impact** — bounded callers/callees and, where the frontend supports it, separately capped dataflow evidence with citations, ranking, coverage, and completion reason.
- **REST/MCP security parity** — shared schemas and codes, tenant isolation, quotas/rate cost, audit, correlation, sanitized diagnostics, and regression coverage.

**Should have after the deterministic baseline is proven:**

- Filtered/paginated change sets, then only a schema-versioned comparison cache keyed by ordered pair, digests, request policy, limits, and CPG/template revision.
- Confidence-scored rename/move detection only after a fixture corpus establishes deterministic behavior.

**Defer (v2+):**

- Embedding/vector reranking, cross-project comparison, automatic PR/review hosting, full patch UI, and raw CPGQL/traversal access.

### Architecture Approach

Use source-first comparison and graph-enriched impact. `VersionIntelligenceService` owns pair preconditions, response composition, citations, budgets, and domain codes; narrow adapters perform immutable snapshot diff, declaration inventory/symbol matching, and approved impact collection. REST and MCP parse/marshal only, then delegate to this service. `ContextRetrievalService` remains single-version retrieval, not a pair-analysis monolith.

**Major components:**

1. **Version pair resolver** — tenant-scoped lookup of both versions; same-project, ready, build-compatible, durable-snapshot preconditions; concealed failure semantics.
2. **Snapshot and symbol diff adapters** — deterministic file bytes/manifests and attributed declaration inventories; exact, heuristic, ambiguous, or unmatched state.
3. **Impact collector** — fixed caller/callee and dataflow templates through `QueryLoader`/`QueryExecutor`; depth/path/row/byte/time caps and evidence kind.
4. **Citation budgeter / shared DTOs** — ordered pair provenance, two-sided citations, per-section budgets, totals, truncation reasons, and stable errors.
5. **REST/MCP adapters and lifespan wiring** — shared service registry, audit events, correlation, quotas, and the existing `ContextRetrievalService` registration fix.

### Critical Pitfalls

1. **Comparing individually ready but incomparable versions** — resolve both under the actor scope before worker access; require same project, ready state, compatible build/frontend metadata, and durable snapshots; return uniform safe not-found semantics for foreign IDs.
2. **Using node IDs or line numbers as cross-version identity** — match source-attributed stable keys and fingerprints in tiers; expose `match_strategy` and confidence instead of inventing a rename/modified relation.
3. **Presenting bounded graph traversal as exhaustive impact** — cap root scope, depth, paths, rows, bytes, wall time, and concurrency; report `complete`, budget-truncated, timeout, unsupported, or no-evidence distinctly.
4. **Interpolating user input into CPGQL or regexes** — accept declarative typed fields only; use `QueryLoader`, centralized escaping, fixed templates, numeric clamps, container controls, and adversarial input tests.
5. **REST/MCP authorization, quota, audit, or cache drift** — one service boundary; audit both opaque IDs and outcomes; define impact cost; never cache errors/partial results or omit tenant, pair order, policy, limits, and algorithm/template version from a cache key.

## Implications for Roadmap

Based on the combined research, suggested milestone structure begins at Phase 9:

### Phase 9: Version Diff Foundation

**Rationale:** Impact is only trustworthy when its selected target comes from a valid, reproducible, cited comparison. Pair validity, immutable evidence, and symbol identity are therefore non-negotiable upstream dependencies.

**Delivers:** `VersionIntelligenceService` and shared DTO/domain codes; lifespan registration of the existing `ContextRetrievalService` and new service; tenant-scoped same-project/ready/build-compatible pair resolver; deterministic source file diff; first source-attributed symbol inventory/matching; two-sided citations; stable sort/budget/truncation semantics; thin REST/MCP compare adapters.

**Addresses:** authorized pair selection, deterministic file/symbol inventory, provenance, citations, and progressive bounded results.

**Avoids:** cross-tenant/version leakage, mutable working-tree reads, CPG-node identity joins, false rename claims, raw-query exposure, and the currently masked service-wiring 503.

### Phase 10: Bounded Change Impact Retrieval

**Rationale:** Once comparison returns a validated changed symbol/location, that concrete target can safely anchor graph work. Direct structural edges are lower risk and should land before optional dataflow.

**Delivers:** approved caller/callee templates and ranking; selected-change validation; direct/heuristic/dataflow evidence kinds; per-section root/depth/path/row/byte/time budgets; explicit completion/coverage states; separately capped dataflow where supported; cache policy/keys only if measurement justifies a cache.

**Addresses:** the change-first impact workflow, caller/callee and dataflow evidence, conservative confidence, and agent-friendly narrowing.

**Avoids:** direct Joern access, queue starvation, N×M symbol matching, unbounded `reachableByFlows`, unsafe cache reuse, and claims that a partial result means no impact.

### Phase 11: Secure Contract & Evaluation

**Rationale:** The feature crosses two public transports and expensive worker infrastructure; its completion condition is behaviorally identical, secure, observable results rather than merely working endpoints.

**Delivers:** golden file/symbol/impact fixtures; REST/MCP success and error parity; tenant/admin/cross-project concealment; quota and rate-cost tests; audit/correlation/sanitization assertions; sleeping/degraded CPG behavior; adversarial escaping and regex tests; documented capability limits.

**Addresses:** public contract parity, security observability, partial-result disclosure, and reliable evaluation across known graph edge cases.

**Avoids:** adapter drift, audit gaps, distinguishable foreign-version errors, CPGQL/regex injection, and regressions hidden by mocked unit tests.

### Phase Ordering Rationale

- Pair authorization, source evidence, and stable symbol identity must precede any graph expansion; otherwise impact lacks a safe/reproducible target.
- File/source comparison is fast and deterministic; CPG impact is serialized and potentially expensive, so it belongs in its own phase with independently enforced budgets.
- Contract security and live-fixture evaluation follow the shared service implementation, validating both adapters against the actual worker lifecycle rather than duplicating logic during feature construction.

### Requirement Implications

The requirements document should explicitly require:

- ordered `base_version_id` → `target_version_id`, same authorized project, both ready/build-compatible, plus safe foreign/missing handling;
- deterministic file and symbol records with base/target citations, match strategy/confidence, counts, budget, and clear truncation/completeness reason;
- selected-change-only structural/dataflow impact through fixed templates, with exact/heuristic/dataflow evidence labels and bounded execution;
- one REST/MCP service/schema/error vocabulary with quotas, audit, correlation, and sanitized failures;
- fixture coverage for add/delete/modify, line shift, overload, move/rename ambiguity, unsupported language, cyclic/high-fanout graph, time/byte caps, injection-shaped input, two tenants, and sleeping CPG recovery.

### Research Flags

Phases likely needing deeper research during planning:

- **Phase 9:** validate actual immutable snapshot/manifest access and build-compatibility metadata, then settle the symbol fingerprint and ambiguity contract against the supported frontends.
- **Phase 10:** validate Joern 4.0.594 template behavior and safe operational thresholds on the bundled core/libxml2 fixtures, especially dataflow, callbacks/virtual dispatch, and worker recovery.
- **Phase 11:** establish exact REST/MCP error and quota-cost mappings from the live service registry; evaluate the `ContextRetrievalService` lifecycle fix in a non-mocked runtime path.

Phases with standard patterns:

- **Phase 9 DTO/service/adapters:** established CodeBadger patterns (`version_context`, `ProjectVersionService`, citation budgets, `QueryLoader`) can be followed directly after the focused validation above.
- **Phase 11 parity fixtures:** existing tenant, audit, correlation, and sanitization integration tests provide the established testing pattern.

## Confidence Assessment

| Area | Confidence | Notes |
|------|------------|-------|
| Stack | HIGH | Checked-in dependencies, Docker pin, worker model, and query controls establish a clear reuse path; official Joern documentation corroborates traversal APIs. |
| Features | HIGH | v0.8 scope and v0.7 public-contract precedents are checked into the repository; product-priority inferences beyond the MVP are MEDIUM. |
| Architecture | HIGH | Existing service/route/worker integration seams were inspected; the unregistered `ContextRetrievalService` is a concrete runtime risk. |
| Pitfalls | HIGH | Tenant, CPGQL, bounds, cache, and transport risks are evidenced in the repository and Joern documentation; exact workload thresholds remain MEDIUM. |

**Overall confidence:** HIGH for the proposed three-phase shape and implementation boundaries.

### Gaps to Address

- **Cross-language declaration fidelity:** use a representative fixture matrix before promising uniform symbol/impact coverage; expose unsupported, ambiguous, and heuristic outcomes in the contract.
- **Build compatibility semantics:** determine the exact existing build-config/frontend/overlay fields available for pair comparison and codify whether incompatibility rejects or returns a scoped warning.
- **Snapshot-access behavior:** verify source snapshot retention and binary/unreadable-file handling against real immutable records rather than assuming the CPG can reproduce source evidence.
- **Operational budgets and cache value:** benchmark representative small and high-fanout CPGs first; do not persist comparison or impact results until the cache key and performance need are proven.
- **Lifespan regression:** reproduce and fix the unregistered `ContextRetrievalService` in a live service registry test before relying on its pattern for v0.8.

## Sources

### Primary (HIGH confidence)

- [Stack research](STACK.md) — checked-in runtime pins, service seams, QueryLoader/QueryExecutor constraints, and official Joern CPGQL references.
- [Feature research](FEATURES.md) — v0.8 MVP boundaries, public contract expectations, and v0.7 parity/citation precedents.
- [Architecture research](ARCHITECTURE.md) — concrete service registry/route integration, component boundaries, data flow, and the context-service wiring gap.
- [Pitfalls research](PITFALLS.md) — repository-specific tenant, cache, escaping, bounded-query, and evaluation failure modes.
- [Graph report](../graphs/GRAPH_REPORT.md) — 3,529 nodes/6,471 edges, no import cycles, and core hubs including `ProjectVersionService` and `QueryExecutor`.

### Secondary (MEDIUM confidence)

- Official Joern CPGQL/dataflow documentation linked from the stack and pitfalls reports — confirms traversal vocabulary and cost characteristics, but not identical frontend behavior for every supported language.

---
*Research completed: 2026-09-17*
*Ready for roadmap: yes*
