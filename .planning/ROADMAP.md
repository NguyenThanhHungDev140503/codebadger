# Roadmap: CodeBadger v0.7 Codebase Context Backend

**Created:** 2026-08-09
**Milestone:** v0.7 Codebase Context Backend
**Granularity:** Standard

## Phase 5: Secure Ingestion & Version Catalog

**Goal:** Synchronize an authenticated Git remote branch into an immutable, content-addressed project version safely.

**Requirements:** INGEST-01, INGEST-02, INGEST-03

**Success Criteria:**
1. An authenticated client can register a GitHub, GitLab, or Azure DevOps remote with a selected branch and trigger an explicit update without waiting for Joern.
2. The update uses validated Git CLI fetch/ref resolution in an isolated workspace; credentials never persist in URLs, Git config, logs, or responses.
3. Each resolved commit produces an immutable version with commit SHA and manifest; an unchanged branch returns the existing version rather than creating a duplicate.

## Phase 6: Durable CPG Lifecycle & Backend Contract

**Goal:** Connect immutable versions to the existing durable CPG queue and expose REST/MCP lifecycle operations with idempotent status semantics.

**Requirements:** CPG-01, CPG-02, CPG-03, CPG-04, API-01, API-02

**Depends on:** Phase 5

**Success Criteria:**
1. A version enqueues one durable CPG build and reuses existing Joern pool/admission/locks.
2. REST and MCP return identical lifecycle states, progress, queue position, retry information, and sanitized errors.
3. Duplicate submissions deduplicate; failed jobs retry safely; cancelled/interrupted jobs leave no partial artifact; restart reconciliation is tested.
4. Equivalent content and build options reuse the CPG cache while a ready version remains immutable.

## Phase 7: Cited Hybrid Context Retrieval

**Goal:** Turn a ready CPG and source snapshot into bounded, explainable context packs for AI agents.

**Requirements:** CTX-01, CTX-02, CTX-03, CTX-04, CTX-05

**Depends on:** Phase 6

**Success Criteria:**
1. Ready versions expose symbol/file/source-span indexes with stable relative-path and line metadata.
2. Context queries combine exact symbol resolution, Postgres lexical/trigram search, and capped Joern graph expansion with deterministic ranking/deduplication.
3. Every response enforces explicit item/byte/token/node/time budgets and marks truncation; every item cites version digest, path, inclusive lines, symbol, and selection reason.
4. Public context operations reject raw CPGQL while internal/admin access remains separately gated.

## Phase 8: Authorization, Quotas & Production Verification

**Goal:** Make the new backend safe and diagnosable under untrusted uploads and agent traffic.

**Requirements:** API-03, API-04

**Depends on:** Phase 6, Phase 7

**Success Criteria:**
1. Authentication and project/version authorization are enforced consistently across REST and MCP; cross-project access tests fail closed.
2. Upload, build, and retrieval quotas/backpressure return typed errors and expose correlation IDs, metrics, audit events, and sanitized diagnostics.
3. End-to-end tests cover malicious archives, restart/retry, cache reuse, context citations/truncation, and REST/MCP parity.
4. Deployment documentation states the single-tenant/Docker-socket posture and required reverse-proxy/worker isolation controls.

---
*Roadmap created: 2026-08-09 for v0.7*
