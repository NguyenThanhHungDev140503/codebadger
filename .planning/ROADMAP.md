# Roadmap: CodeBadger

## Milestones

- ✅ **v0.7 Codebase Context Backend** — Phases 5–8 shipped 2026-09-17.
- 🚧 **v0.8 Version Intelligence & Change Impact** — Phases 9–11 planned.

## Overview

v0.8 turns CodeBadger's immutable version catalog and CPG lifecycle into a safe agent workflow: first establish a tenant-authorized, reproducible and cited change inventory; then enrich one selected change with bounded graph evidence; finally prove that REST and MCP expose the same secure, observable contract. It deliberately keeps raw CPGQL, cross-project comparison, vector retrieval, and hosting workflows outside this milestone.

## Phases

**Phase Numbering:** Integer phases are planned milestone work. Decimal phases, if needed later, are urgent insertions between their surrounding integer phases.

### 🚧 v0.8 Version Intelligence & Change Impact

**Milestone Goal:** An authorized AI agent can compare two immutable codebase versions and receive bounded, cited evidence of what changed and the likely structural/data-flow impact.

- [ ] **Phase 9: Version Diff Foundation** — Safely resolve an ordered version pair and return deterministic, cited file and symbol changes.
- [ ] **Phase 10: Bounded Change Impact Retrieval** — Explain the structural and supported data-flow impact of one selected change within explicit budgets.
- [ ] **Phase 11: Secure Contract & Evaluation** — Prove REST/MCP parity, tenant safety, observability, and fixture-backed behavior end to end.

## Phase Details

### Phase 9: Version Diff Foundation
**Goal**: Authorized agents can compare an ordered pair of immutable versions and receive a reproducible, bounded, two-sided source evidence inventory before any impact traversal is attempted.
**Depends on**: Phase 8 (completed)
**Requirements**: DIFF-01, DIFF-02, DIFF-03
**Success Criteria** (what must be TRUE):
  1. An authenticated client can compare only two ready, durable, build-compatible versions of the same authorized project; foreign, missing, unready, and incompatible IDs are indistinguishable through stable sanitized errors and do not trigger worker access.
  2. A successful comparison returns deterministically ordered added, modified, and deleted file records with applicable base/target citations, truthful totals and returned counts, and an explicit truncation/completion state.
  3. A successful comparison returns source-attributed changed symbols rather than CPG-local node IDs, with base/target citations and an honest exact, heuristic, ambiguous, or unmatched matching result.
  4. The same shared comparison service is reachable through thin REST and MCP adapters, and the runtime service registry also makes the existing single-version context retrieval service available rather than returning a lifecycle-wiring failure.
**Plans**: 3 plans expected

Plans:
- [ ] 09-01: Define shared version-pair, citation, error, and budget domain contracts; enforce concealed authorization and comparison preconditions.
- [ ] 09-02: Build deterministic snapshot file and source-attributed symbol comparison with stable ordering, matching strategy, and bounded metadata.
- [ ] 09-03: Register lifecycle services and expose the shared comparison contract through REST and MCP adapters.

**Research flag**: Verify durable snapshot/manifest and build-compatibility fields in real catalog records; settle the source fingerprint and ambiguity contract against representative supported frontends.

### Phase 10: Bounded Change Impact Retrieval
**Goal**: Authorized agents can select a comparison change and receive cited, ranked structural and supported data-flow impact without treating bounded graph work as exhaustive analysis.
**Depends on**: Phase 9
**Requirements**: IMPACT-01, IMPACT-02, IMPACT-03
**Success Criteria** (what must be TRUE):
  1. An authorized client can select a valid change from its comparison and receive ranked caller/callee evidence tied to that change, with source citations and clear resolved, indirect, or other evidence-kind labels.
  2. For a frontend that supports it, the client can receive separately bounded data-flow evidence; unsupported, unavailable, partial, budget-truncated, timed-out, and complete no-impact outcomes are distinguishable.
  3. Comparison and impact requests clamp root, depth, path, row, byte, time, and concurrency limits and report totals, returned results, coverage, completion reason, and truncation without accepting or exposing raw CPGQL.
**Plans**: 3 plans expected

Plans:
- [ ] 10-01: Validate selected comparison changes and collect cited caller/callee impact through reviewed templates and deterministic ranking.
- [ ] 10-02: Add separately bounded, capability-aware data-flow impact with explicit coverage and completion semantics.
- [ ] 10-03: Apply and test feature-specific budget, cancellation, and worker-recovery controls across comparison and impact responses.

**Research flag**: Validate Joern 4.0.594 caller/callee and data-flow template behavior, safe thresholds, callback/virtual-dispatch limitations, and recovery on the bundled core/libxml2 fixtures.

### Phase 11: Secure Contract & Evaluation
**Goal**: Public REST and MCP users observe one secure, diagnosable comparison-and-impact contract proven against realistic source, graph, lifecycle, and adversarial fixtures.
**Depends on**: Phase 9, Phase 10
**Requirements**: API-05, EVAL-01
**Success Criteria** (what must be TRUE):
  1. Equivalent REST and MCP calls use the same service, schemas, stable error vocabulary, result semantics, and sanitized diagnostics for both successful and rejected comparison/impact requests.
  2. Both transports preserve tenant concealment, authorization, quota/rate cost, audit outcomes, and correlation IDs for each opaque version pair and selected change.
  3. Fixture and end-to-end coverage demonstrates deterministic file changes, line shifts, overload/rename/move ambiguity, unsupported language, cyclic or high-fanout graphs, sleeping/degraded CPG recovery, all budget outcomes, injection-shaped input, two-tenant concealment, and REST/MCP parity.
**Plans**: 2 plans expected

Plans:
- [ ] 11-01: Harden shared public-contract observability: transport parity, tenant/authorization concealment, quota cost, audit events, correlation, and sanitization.
- [ ] 11-02: Build the fixture and end-to-end regression matrix for diff, impact, adversarial input, bounded execution, and lifecycle recovery.

**Research flag**: Confirm exact REST/MCP error and quota-cost mappings against the live service registry and run the context-service registration regression through a non-mocked runtime path.

## Requirement Coverage

| Requirement | Phase | Status |
|-------------|-------|--------|
| DIFF-01 | Phase 9 | Planned |
| DIFF-02 | Phase 9 | Planned |
| DIFF-03 | Phase 9 | Planned |
| IMPACT-01 | Phase 10 | Planned |
| IMPACT-02 | Phase 10 | Planned |
| IMPACT-03 | Phase 10 | Planned |
| API-05 | Phase 11 | Planned |
| EVAL-01 | Phase 11 | Planned |

**Coverage:** 8/8 v1 requirements mapped exactly once; no orphaned or duplicated requirements.

## Progress

**Execution Order:** 9 → 10 → 11

| Phase | Milestone | Plans Complete | Status | Completed |
|-------|-----------|----------------|--------|-----------|
| 9. Version Diff Foundation | v0.8 | 0/3 | Not started | - |
| 10. Bounded Change Impact Retrieval | v0.8 | 0/3 | Not started | - |
| 11. Secure Contract & Evaluation | v0.8 | 0/2 | Not started | - |

## Milestone History

### v0.7 — Codebase Context Backend

- **Status:** Completed 2026-09-17 (Phases 5–8; 16/16 requirements satisfied)
- **Goal:** Turn CodeBadger from an interactive Joern analysis server into a source-backed, immutable codebase context backend for AI agents.
- **Archived artifacts:** `.planning/milestones/v0.7-ROADMAP.md`, `.planning/milestones/v0.7-REQUIREMENTS.md`, `.planning/milestones/v0.7-MILESTONE-AUDIT.md`, and `.planning/milestones/v0.7-phases/`.

---
*Roadmap created: 2026-09-17 for v0.8 Version Intelligence & Change Impact*
