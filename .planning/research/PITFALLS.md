# Domain Pitfalls: Version Intelligence & Change Impact

**Domain:** Authenticated, multi-tenant code-version comparison and CPG-backed impact analysis
**Researched:** 2026-09-17
**Confidence:** HIGH for repository-specific risks; MEDIUM for Joern operational thresholds (workload-dependent).

## Critical Pitfalls

### Pitfall 1: Comparing identities instead of comparable immutable source snapshots

**What goes wrong:** A “diff” is calculated for two version IDs that are individually ready but belong to different projects/tenants, or whose CPG/build configuration differs. The result looks valid yet attributes changes to the wrong repository, misses parser-induced differences, or leaks the existence of another tenant’s version.

**Why it happens:** `ProjectVersionService` makes a version ID from project ID, commit SHA, and build configuration; source `manifest`, digest, and snapshot reference are stored separately. “Ready” alone does not establish that two versions are a valid comparison pair.

**How to avoid:** Resolve *both* versions through one service method taking `owner_scope`; require same `project_id`, `build_status == ready`, compatible build-config/schema/frontend identity, and durable source-manifest/snapshot availability. Treat all failures—including foreign IDs—as the same sanitized not-found response. Declare what a modified file/symbol means (manifest-byte change versus CPG structural change) and return both version IDs/digests, comparison algorithm version, and per-item source citations.

**Warning signs:** Cross-tenant comparison returns 403 or different error text; identical source manifests produce non-empty changes; rerunning comparison changes order/count; a rebuilt version yields different results without an algorithm/config change.

**Phase to address:** Phase 9 — comparison foundation.

---

### Pitfall 2: Treating CPG symbol identities as stable across versions

**What goes wrong:** Node IDs, parser-generated names, signatures, filename normalization, or line numbers are used as a cross-version join key. Renames become delete/add noise, overloads collide, and a citation can point at the wrong declaration after a line shift.

**Why it happens:** CPG node IDs are graph-instance-local; static-analysis frontends can represent language constructs differently. Existing context retrieval deduplicates by `filename:lineNumber`, which is useful within one snapshot but unsafe as a version-to-version semantic identity.

**How to avoid:** Build a deterministic comparison record per side: normalized repository-relative path, language, symbol kind, qualified name/signature where available, source span, and normalized declaration/body fingerprint. Match in tiers (exact stable key → rename/move heuristic marked `confidence` → add/delete); never silently call a heuristic match “modified.” Keep original source citations for both sides and expose `match_strategy`/`confidence`.

**Warning signs:** A whitespace-only commit reports broad changes; line insertion remaps unrelated symbols; overloaded methods are merged; results vary by CPG rebuild.

**Phase to address:** Phase 9 — comparison contract and fixture corpus.

---

### Pitfall 3: Unbounded or misleading impact traversal

**What goes wrong:** A small changed symbol fans out through callers/callees or `reachableByFlows`; requests monopolize a Joern JVM, time out, kill a worker, and return a partial result as though it were complete. Conversely, a shallow traversal is presented as “all impact.”

**Why it happens:** Joern documents interprocedural dataflow slicing as computationally expensive and depth-limited. The repository already special-cases `reachableByFlows` with a larger timeout and a 50-result cap, and serializes queries per CPG because a JVM handles one query at a time.

**How to avoid:** Use named, internally authored templates only—never a caller-selected CPGQL fragment. Bound roots, direction, hop/depth, path count, rows, bytes, wall time, and concurrency; preflight root resolution before dataflow; rank before final truncation; and distinguish `complete`, `truncated_by_budget`, `timeout`, `unsupported_language`, and `no_cpg_evidence`. A timeout must not be cached as success. Return which limits were applied and citations for every claimed edge/path.

**Warning signs:** Queue/lock waits rise after broad changes; worker reactivation churn follows requests; response latency tracks graph size; a response with `truncated=true` is described as exhaustive.

**Phase to address:** Phase 10 — bounded impact retrieval.

---

### Pitfall 4: CPGQL/regex injection and escaping regressions in a “safe” endpoint

**What goes wrong:** A changed symbol, filename, or filter is interpolated into a Scala/CPGQL or regex literal. Quotes alter the query; regex features create pathological matching; a new raw-query escape hatch bypasses containment.

**Why it happens:** CPGQL executes in an Ammonite Scala REPL. The existing raw CPGQL validator explicitly says its denylist is defense-in-depth, not a security boundary. `ContextRetrievalService` currently interpolates `query` into method/type regexes, while `escape_scala_string` and regex bounds exist elsewhere.

**How to avoid:** Make the v0.8 API declarative (version IDs, path/symbol IDs, fixed enum directions and numeric bounds). Centralize Scala string escaping and regex validation; prefer exact matching or safely constructed literal regexes; clamp every numeric option through `QueryLoader`/shared validator; and keep Joern workers containerized with restricted mounts, network, CPU/memory, and process lifetime. Add adversarial tests for quote, backslash, newline, regex metacharacters, nested quantifiers, and template injection.

**Warning signs:** Query text contains request input outside a quoted/escaped binding; an input changes query structure; JVM CPU spikes on a short search; validator-only tests are treated as sandbox tests.

**Phase to address:** Phase 9 for input model; Phase 10 for templates and worker containment tests.

---

### Pitfall 5: REST/MCP parity drifting on authorization, quotas, and auditability

**What goes wrong:** REST validates both version IDs and invokes tenant-scoped service access, but MCP directly calls lower-level methods, omits rate/cost charging, or logs one version but not the other. A comparison becomes a cross-tenant inference endpoint.

**Why it happens:** Existing lifecycle features have separate route/tool adapters. The project already tests REST tenant isolation, rate limits, concurrent build quota, correlation IDs, and audit events; future endpoints can accidentally bypass these established seams.

**How to avoid:** Implement one tenant-aware comparison/impact service as the sole authorization boundary; both adapters pass actor/tenant/correlation context to it. Authorize each version separately before checking their relationship; ensure admin elevation is explicit; define per-request cost/quota for CPU-expensive impact calls; and log action, actor, tenant, both opaque version IDs, outcome/error code, applied budgets, result counts, and correlation ID without source content or credentials.

**Warning signs:** Route and tool produce different status/error shapes; a foreign second version yields a distinguishable error; audit has only one resource ID; quota tests cover build but not impact traversal.

**Phase to address:** Phase 11 — contract and evaluation hardening (with service signature designed in Phase 9).

---

### Pitfall 6: Incorrect caching and freshness semantics

**What goes wrong:** A cached impact response from one tenant, CPG state, or budget is reused for another; a partial/timeout response is cached; cache keys omit algorithm/template version; or cache invalidation assumes immutable catalog entries guarantee immutable worker state.

**Why it happens:** Current query caching keys tool output by tool, codebase hash, and caller parameters, and intentionally avoids caching error sentinels. Version comparison adds two operands, matching policy, impact budget, and CPG/frontend semantics.

**How to avoid:** Cache only successful, fully specified results. Key by tenant scope, ordered baseline/candidate version IDs, both content digests, comparison/template version, normalized request, limits, and CPG/frontend version. Make order part of the contract (`base` → `candidate`), omit or separately mark incomplete results, and invalidate on build metadata/CPG schema changes.

**Warning signs:** Changing `max_depth` returns an old response; reversing versions leaves `added` unchanged; a cache hit lacks the current budget/correlation metadata; cache key tests do not include both operands.

**Phase to address:** Phase 10 — result/caching semantics.

## Technical Debt Patterns

| Shortcut | Immediate Benefit | Long-term Cost | When Acceptable |
|---|---|---|---|
| Diff only CPG node IDs/line numbers | Fast prototype | Incorrect semantic joins and unstable citations | Never in public contract |
| Return raw CPGQL output | Minimal mapping code | Leaks internal schema, unbounded payloads, no REST/MCP stability | Never |
| Add a new query path outside `QueryExecutor` | Fewer integration changes | Bypasses locks, timeout recovery, tracing and output cap | Never |
| Use one generic `truncated` flag | Small response schema | User cannot distinguish budget, timeout, unsupported language or no evidence | Only briefly behind an internal experiment |
| Cache by version pair alone | Better hit rate | Cross-policy/budget stale answers | Never |

## Integration Gotchas

| Integration | Common Mistake | Correct Approach |
|---|---|---|
| Project version catalog | Resolve `version_id` without tenant/project pair validation | Resolve both with `owner_scope`, then require same project and ready state |
| Joern worker manager | Run two graph queries concurrently or leave a timeout JVM alive | Use `QueryExecutor` lock, bounded timeout, sleep/reactivate recovery |
| Source manifests/snapshots | Assume a CPG alone can reproduce file-level evidence | Require durable manifest/snapshot references and cite both sides |
| REST and FastMCP | Duplicate auth/budget logic in adapters | One service and shared schema/contract tests |
| Audit/correlation | Log only success, or raw source/query contents | Log both opaque version IDs, outcome, limits, counts and correlation ID |

## Performance Traps

| Trap | Symptoms | Prevention | When It Breaks |
|---|---|---|---|
| `reachableByFlows` from every changed symbol | Long JVM holds, timeouts, worker kills | Cap roots/depth/paths/time; require explicit opt-in broad mode | Large/multi-language repos or high-fanout symbols; measure with fixture CPGs |
| N×M symbol matching across entire versions | Memory/latency grows quadratically | Index fingerprints by normalized stable key; compare changed manifest paths first | Repositories with thousands of declarations |
| Fetch full source snippets before ranking | Byte budget consumed by low-value evidence | Rank structural changes/impact first, hydrate cited snippets last | Large generated files or broad diffs |
| Per-tenant request burst against one CPG | `SERVER_BUSY`, tail latency, reactivation churn | Cost-based rate limit and concurrency budget for comparison/impact | Any concurrent callers on same active CPG JVM |

## Security Mistakes

| Mistake | Risk | Prevention |
|---|---|---|
| Authorize only baseline version | Cross-tenant candidate existence/source inference | Tenant-scope and readiness check both operands before relationship checks; uniform 404 |
| Interpolate path/symbol into CPGQL | Scala/CPGQL injection or regex DoS | Typed request schema, centralized escaping/regex validation, fixed templates, sandboxed worker |
| Return raw worker exception/paths | CPG/source topology disclosure | Stable sanitized codes plus recovery hints; correlation ID for support |
| Cache without scope/policy/version fields | Tenant data leakage or policy bypass | Complete cache key and no error/partial cache entries |
| Treat static-analysis output as proof | Unsafe automated decision based on false positive/negative | Label evidence and limitations; require citations and confidence/completeness fields |

## UX Pitfalls

| Pitfall | User Impact | Better Approach |
|---|---|---|
| “Modified” does not say how matched | Users trust false rename/move claims | Show exact/heuristic match strategy and confidence |
| An empty impact result reads as no risk | May mean unsupported/missing/truncated analysis | Return explicit analysis status and capability/limit metadata |
| Only aggregate count without evidence | Cannot review or reproduce claim | Provide bounded, stable ordered citations for both versions |
| Results reorder between requests | Agents cannot compare/retry safely | Deterministic sorting and pagination/cursor rules |

## "Looks Done But Isn't" Checklist

- [ ] **Version comparison:** Both version IDs are tenant-scoped, ready, same-project, and build-compatible—not merely syntactically valid.
- [ ] **File/symbol changes:** Every item has baseline/candidate citations, deterministic order, a match strategy, and a declared semantic meaning.
- [ ] **Impact:** Every traversal reports root resolution, limits, completeness/truncation reason, and no uncited edge/path.
- [ ] **Safety:** Inputs never form arbitrary CPGQL; negative/adversarial escaping and regex tests run against each template.
- [ ] **Parity:** REST and MCP share service/schema; cross-tenant, quota, audit, correlation, sanitized-error, and cache-key matrices cover both.
- [ ] **Evaluation:** Fixture corpus includes rename, overload, move, whitespace-only, generated code, unsupported language, cyclic call graph, and known dataflow examples.

## Recovery Strategies

| Pitfall | Recovery Cost | Recovery Steps |
|---|---|---|
| Incorrect comparison mapping shipped | HIGH | Version the algorithm, invalidate affected cache entries, preserve raw manifests, rerun comparisons, mark prior result version deprecated |
| Runaway impact query | MEDIUM | Let `QueryExecutor` terminate/sleep worker, return timeout state, reduce limits/template scope, reproduce on fixture CPG |
| Tenant-scope regression | HIGH | Disable endpoint/cache reads, review audit by correlation/resource IDs, rotate cache namespace, add regression matrix before re-enable |
| Source citation unavailable | MEDIUM | Return `evidence_unavailable` rather than fabricate span, repair snapshot retention, rerun build/comparison |

## Pitfall-to-Phase Mapping

| Pitfall | Prevention Phase | Verification |
|---|---|---|
| Comparable immutable snapshots and tenant isolation | 9 — Version Diff Foundation | Same-project/ready/foreign/unsupported pairs; deterministic golden diff fixtures |
| Stable symbol matching and citations | 9 — Version Diff Foundation | Rename/move/overload/line-shift/whitespace fixtures with exact expected evidence |
| Bounded CPG impact | 10 — Change Impact Retrieval | Depth/path/byte/time caps, cycle/fanout fixture, worker recovery and completeness assertions |
| Query/regex safety | 9–10 | Injection, ReDoS-shaped, delimiter and numeric-bound tests; container restriction smoke test |
| REST/MCP security and observability parity | 11 — Secure Contract & Evaluation | Cross-tenant 404, quota/cost, sanitized errors, correlation and audit assertions through both adapters |
| Cache correctness | 10–11 | Key matrix including version order, tenant, limits, template/algorithm version, failure/partial non-caching |

## Sources

- [Joern dataflow engine README](https://github.com/joernio/joern/blob/master/dataflowengineoss/README.md) — **HIGH**: engine configuration and reachability traversal.
- [Joern data-flow slicing guide](https://github.com/joernio/joern/blob/master/joern-cli/JOERN_SLICE.md) — **HIGH**: interprocedural slicing is depth-limited and computationally expensive.
- [Joern traversal migration notes](https://github.com/joernio/joern/blob/master/changelog/traversal_removal.md) — **HIGH**: generated traversal APIs are preferable to string-based traversal.
- Repository evidence — **HIGH**: `src/services/query_executor.py` (per-CPG lock, timeout recovery, dataflow cap), `src/utils/validators.py` (raw CPGQL validator is not a security boundary), `src/services/context_retrieval_service.py` (current interpolation/dedup behavior), `tests/integration/test_security_parity.py` (existing parity expectations).

---
*Pitfalls research for: CodeBadger v0.8 Version Intelligence & Change Impact*
*Researched: 2026-09-17*
