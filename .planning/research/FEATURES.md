# Feature Research: Version Intelligence & Change Impact

**Domain:** authenticated, CPG-backed code-version comparison API for AI agents
**Researched:** 2026-09-17
**Confidence:** HIGH for repository constraints; MEDIUM for product-priority inference

## Feature Landscape

CodeBadger already treats a project version as an immutable, content-addressed snapshot with an explicit build lifecycle. Its public context operation is tenant-scoped, bounded, cited, and intentionally hides CPGQL. v0.8 should extend that same agent-facing contract to a comparison of *two ready versions from the same authorized project*, then offer a separate bounded explanation of the structural/data-flow impact of a selected changed symbol.

The useful unit of work is an evidence-backed review question: “what changed between these two snapshots and which known program relationships might this change affect?” It is not a Git hosting UI, a general semantic diff engine, or an unbounded graph exploration surface.

### Table Stakes (Users Expect These)

| Feature | Why Expected | Complexity | Notes |
|---------|--------------|------------|-------|
| Authorized two-version selection and readiness validation | A comparison is meaningless if either snapshot is unavailable or belongs to a different tenant/project. | MEDIUM | Require `base_version_id` and `target_version_id`; resolve both through existing owner scope; return fail-closed 404 for missing/unauthorized IDs and stable invalid/not-ready errors for lifecycle violations. Never silently compare versions from different projects. |
| Deterministic file change inventory | An agent must reliably identify added, deleted, and modified paths before deciding where to look. | MEDIUM | Stable ordering plus `total`, `returned`, and `truncated`; each item has change kind and base/target source evidence where applicable. Treat rename as delete+add until a deterministic rule exists. |
| Deterministic symbol change inventory | File-level diffs alone cannot drive code review or impact questions. | HIGH | Use qualified name/signature plus path and line span, not transient Joern node IDs. Explicitly classify unmatched/ambiguous symbols rather than guessing. |
| Source citations and version provenance | AI agents need to quote and re-fetch evidence. | MEDIUM | Every file/symbol/impact item identifies version ID/digest, relative path, 1-based line range when known, symbol/signature, and selection/change reason. Preserve both sides of a modification. |
| Bounded structural impact | “What breaks if this changes?” is the core action after a diff. | HIGH | For a selected changed symbol/location, return callers, callees, and data-flow relationships with depth/result/byte/time budgets, deterministic ranking/deduplication, counts, and `truncated`. |
| Partial-result and analysis-limitation disclosure | CPG resolution varies by language and dataflow traversals are intrinsically expensive. | HIGH | Distinguish no relationships from incomplete/timed-out/unsupported analysis; include coverage metadata and safe narrowing hints. Never imply no impact from a truncated result. |
| REST/MCP contract parity and security observability | Existing lifecycle/context tools already expose both transports and tenant/audit controls. | MEDIUM | Both transports call the same application service and response schema/error vocabulary. Apply auth, tenant isolation, rate limits, correlation IDs, sanitized diagnostics, and audit events. |

### Differentiators (Competitive Advantage)

| Feature | Value Proposition | Complexity | Notes |
|---------|-------------------|------------|-------|
| Change-first impact workflow | Joins immutable source diff evidence to CPG relationships, allowing a move from “what changed” to “what may be affected” without raw graph queries. | HIGH | Make impact input reference a returned changed symbol/file hunk (or validated location), not arbitrary graph IDs. |
| Two-sided, cited evidence | Lets an agent compare exact before/after spans and explain why an item was selected. | MEDIUM | Reuse v0.7 citation vocabulary; include `base` and `target` citations rather than a prose-only summary. |
| Conservative confidence/coverage semantics | Bounded impact can safely inform agent automation only when it declares what was analyzed and omitted. | HIGH | Label structural versus data-flow evidence; return analysis status/budget use and no invented certainty. |
| Agent-friendly progressive narrowing | Supports small initial summaries and deliberate drill-down while preserving Joern capacity. | MEDIUM | Summary first; follow with filtered/paginated changes and impact for a selected change. |

### Anti-Features (Commonly Requested, Often Problematic)

| Feature | Why Requested | Why Problematic | Alternative |
|---------|---------------|-----------------|-------------|
| Public raw CPGQL or arbitrary graph traversal | It appears maximally flexible. | Violates existing security boundary and permits expensive/unpredictable queries. | Small validated compare/impact vocabulary with clamped limits. |
| Full patch viewer, Git hosting, comments, or PR workflow | A visual review experience is familiar. | Turns CodeBadger into a repository/collaboration product and duplicates SCM responsibilities. | Return cited structured change evidence for the caller/UI to render. |
| Claiming complete semantic equivalence or “zero impact” | Consumers want a definitive answer. | Cross-language CPG coverage, resolution ambiguity, and budgets make this unsound. | Return bounded evidence plus analysis status, coverage, and partial flags. |
| Automatic rename/move detection based on heuristics | Rename labels look cleaner than add/delete. | Heuristics are unstable and undermine deterministic output. | Start with content/path changes; consider an explicitly confidence-scored capability later. |
| Cross-project or cross-tenant comparison | It sounds useful for migrations. | Weakens authorization expectations and makes ownership semantics unclear. | Limit v0.8 to two ready versions of one authorized project. |
| Embedding/vector reranking for comparison | Semantic search could feel smarter. | Adds infrastructure and non-determinism before proving deterministic diff + CPG workflow. | Use exact manifest/source and CPG evidence; retain `RETR-01` for later. |

## Feature Dependencies

```
Ready immutable version + tenant/project authorization
    └──requires──> deterministic file change inventory
                         └──requires──> stable symbol matching and two-sided citations
                                              └──enables──> selected-change impact analysis
                                                                    └──requires──> bounded CPG traversal

Shared comparison/impact application service
    └──requires──> REST/MCP schema parity + audit/correlation controls

Truncation/coverage metadata ──qualifies──> every list and impact conclusion
```

### Dependency Notes

- **Comparison requires ready immutable versions:** v0.7 catalog/lifecycle is the authority for commit SHA, digest, owner scope, and whether a CPG may be queried; v0.8 must not introduce another snapshot model.
- **Impact requires a selected, validated change:** relationships are interpretable only when anchored to a comparison result, avoiding public Joern node IDs.
- **Data-flow requires strict limits:** `QueryExecutor` specially constrains `reachableByFlows` because it can return huge results and run for minutes; the public feature must propagate budget and partial status.
- **Parity requires one service boundary:** separate REST and MCP implementations would drift in validation/citations/authorization. `version_context` is the precedent.

## MVP Definition

### Launch With (v0.8)

- [ ] **Same-project ready-version comparison** — validate two version IDs under tenant scope and return provenance, stable errors, and a deterministic bounded file/symbol summary.
- [ ] **Two-sided citations and pagination/budgets** — every detailed change is attributable to base/target source span; every capped collection reports limit, returned count, and truncation.
- [ ] **Selected-change structural impact** — return bounded, ranked callers and callees for an eligible changed symbol/location, with coverage and partial-result semantics.
- [ ] **Selected-change data-flow impact where supported** — add strictly bounded flow evidence, distinguish it from structural edges, and surface unavailable/partial analysis honestly.
- [ ] **REST/MCP parity with regression coverage** — matching schemas plus authorization, cross-tenant concealment, quotas/rate limits, audit, correlation IDs, and sanitized failures in both transports.

### Add After Validation (v0.8.x)

- [ ] **Explicit filters and pagination over change sets** — path, change-kind, and symbol filters after fixture corpus confirms schema stability/order.
- [ ] **Confidence-scored rename/move detection** — only if users show delete+add is insufficient and fixtures establish deterministic confidence behavior.
- [ ] **Comparison-result cache keyed by both immutable digests and request parameters** — when repeated analysis measurably loads Joern; cache only bounded schema-versioned output.

### Future Consideration (v2+)

- [ ] **Embedding/reranked semantic change retrieval** — defer until deterministic comparisons are adopted and large-repo recall is inadequate.
- [ ] **Cross-project/migration impact analysis** — defer pending explicit authorization and identity model.
- [ ] **Interactive review/hosting workflow** — defer: CodeBadger is an agent backend, not SCM product.

## Feature Prioritization Matrix

| Feature | User Value | Implementation Cost | Priority |
|---------|------------|---------------------|----------|
| Ready-version validation and deterministic file diff | HIGH | MEDIUM | P1 |
| Symbol matching with two-sided citations | HIGH | HIGH | P1 |
| Bounded caller/callee impact | HIGH | HIGH | P1 |
| Bounded data-flow evidence with partial semantics | HIGH | HIGH | P1 |
| REST/MCP parity, auth/audit/quota regression suite | HIGH | MEDIUM | P1 |
| Filters/pagination refinements and cache | MEDIUM | MEDIUM | P2 |
| Rename/move heuristics | MEDIUM | HIGH | P3 |
| Vector reranking or cross-project compare | LOW for v0.8 | HIGH | P3 |

## Comparable Product/Contract Patterns

| Pattern | Established in CodeBadger | v0.8 Approach |
|---------|---------------------------|---------------|
| Agent-facing operation | `version_context` uses validated budgeted inputs and `ContextRetrievalService`. | `version_compare` and `version_change_impact` delegate to one domain service; no query language. |
| Cited bounded response | Context includes version digest, source location, budget, `truncated`. | Require base/target citations and consumption/partial status. |
| Tenant failure semantics | REST returns 404 for absent/unauthorized scoped version. | Resolve both inputs under one scope before comparison; never disclose another tenant's version. |
| Expensive graph work | Data-flow execution has result/time limits; templates cap flows. | Use bounded evidence and safe partial outcome, never auto-broaden/retry. |

## Sources

- Repository evidence (HIGH): `.planning/PROJECT.md` — v0.8 goal, scope, and out-of-scope constraints.
- Repository evidence (HIGH): `.planning/milestones/v0.7-REQUIREMENTS.md` and `v0.7-MILESTONE-AUDIT.md` — immutable lifecycle, citations, public-CPGQL boundary, auth, quota, and parity.
- Repository evidence (HIGH): `src/api/rest_routes.py` (`get_version_context`) and `src/tools/lifecycle_tools.py` (`version_context`) — REST/MCP service delegation, tenant, and audit patterns.
- Repository evidence (HIGH): `src/services/context_retrieval_service.py` — ready-version validation, deterministic dedupe, item/byte truncation.
- Repository evidence (HIGH): `src/services/query_executor.py` and `src/tools/queries/taint_flows.scala` — data-flow needs special timeout/result caps and emits source locations.

---
*Feature research for: CodeBadger v0.8 Version Intelligence & Change Impact*
*Researched: 2026-09-17*
