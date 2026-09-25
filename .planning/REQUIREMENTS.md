# Requirements: CodeBadger v0.8 Version Intelligence & Change Impact

**Defined:** 2026-09-17
**Core Value:** An authorized AI agent can compare two immutable codebase versions and receive bounded, cited evidence of what changed and the likely structural/data-flow impact.

## v1 Requirements

### Version Comparison

- [ ] **DIFF-01**: An authenticated client can compare an ordered `base_version_id` and `target_version_id` only when both are ready, durable, build-compatible versions of the same authorized project; foreign, missing, unready, or incompatible versions receive stable sanitized errors without revealing ownership.
- [ ] **DIFF-02**: A comparison returns deterministic added, modified, and deleted file records with stable ordering, base/target source citations where applicable, totals, returned counts, and truthful truncation/completion metadata.
- [ ] **DIFF-03**: A comparison returns changed symbol records using source-attributed identity rather than CPG-local node IDs, including base/target citations and an explicit exact, heuristic, ambiguous, or unmatched match strategy/confidence.

### Change Impact

- [ ] **IMPACT-01**: An authorized client can select a valid comparison change and obtain bounded, ranked caller/callee impact evidence tied to that selected change, with citations and evidence-kind labels.
- [ ] **IMPACT-02**: Where the language frontend supports it, a selected change can return separately bounded data-flow impact evidence; unsupported, absent, partial, budget-truncated, and timed-out results are distinguishable from a complete no-impact result.
- [ ] **IMPACT-03**: Comparison and impact operations clamp request limits and enforce root, depth, path, row, byte, time, and concurrency budgets, reporting totals, returned results, completion reason, coverage, and truncation without exposing raw CPGQL.

### Public Contract & Verification

- [ ] **API-05**: REST and MCP comparison/impact adapters use one shared application service, schemas, and stable error vocabulary while preserving tenant isolation, authorization, quota/rate cost, audit events, correlation IDs, and sanitized diagnostics.
- [ ] **API-06**: Every public MCP lifecycle, context, comparison, and impact operation derives tenant scope and admin privileges solely from the verified caller identity. A conflicting caller-supplied `owner_scope` fails closed with the same concealed not-found semantics as REST, and cross-tenant access is allowed only for a verified admin role.
- [ ] **EVAL-01**: A fixture and end-to-end regression suite covers file add/delete/modify, line shifts, overload and rename/move ambiguity, unsupported language, high-fanout/cyclic graphs, sleeping or degraded CPG recovery, budget caps, injection-shaped input, two-tenant concealment, and REST/MCP success and error parity.

## v2 Requirements

- **RETR-01**: Embedding/vector retrieval and reranking for large repositories.
- **ISOL-01**: Full multi-tenant worker sandbox with per-tenant storage isolation and quotas.
- **OPS-01**: Kubernetes or multi-host scheduling and automated deployment pipeline.
- **DIFF-04**: Confidence-scored rename/move detection once a representative fixture corpus proves deterministic behavior.
- **CACHE-01**: Persistent comparison/impact cache only after operational measurements justify a versioned, tenant-safe cache key.

## Out of Scope

| Feature | Reason |
|---------|--------|
| Cross-project version comparison | Violates the v0.8 project- and tenant-scoped evidence model. |
| Raw public CPGQL or traversal access | Joern's Scala execution surface is not a public security boundary. |
| General code hosting, PR/review workflow, or patch UI | CodeBadger remains an analysis backend. |
| Embedding/vector database | Source-first deterministic comparison and graph evidence must be proven first. |
| Implicit heuristic rename claims | Ambiguous identity must remain explicit until measured against fixtures. |
| Kubernetes/multi-host orchestration | Requires a dedicated infrastructure milestone. |

## Traceability

| Requirement | Phase | Status |
|-------------|-------|--------|
| DIFF-01 | Phase 9 | Planned |
| DIFF-02 | Phase 9 | Planned |
| DIFF-03 | Phase 9 | Planned |
| IMPACT-01 | Phase 10 | Planned |
| IMPACT-02 | Phase 10 | Planned |
| IMPACT-03 | Phase 10 | Planned |
| API-05 | Phase 11 | Planned |
| API-06 | Phase 9 | Planned |
| EVAL-01 | Phase 11 | Planned |

**Coverage:** 9 v1 requirements; 9 mapped; 0 unmapped ✓.

---
*Requirements defined: 2026-09-17*
*Last updated: 2026-09-26 to make authenticated MCP tenant binding an explicit v0.8 requirement.*
