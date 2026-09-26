# CodeBadger

## What This Is

CodeBadger is a containerized MCP (Model Context Protocol) server that gives AI agents deep, queryable access to codebase structure and data flow through Joern Code Property Graphs (CPGs). It supports 14+ languages (Java, C/C++, JavaScript, Python, Go, Kotlin, C#, PHP, Ruby, Swift, etc.) for both program analysis and vulnerability analysis — useful for academic research and industry security/engineering work.

## Core Value

AI agents can query and analyze production codebases through CPGs with memory-safe, scalable infrastructure — enabling vulnerability discovery, taint tracking, and deep code understanding at scale.

## Current Milestone: v0.8 Version Intelligence & Change Impact

**Goal:** Let authenticated agents compare immutable codebase versions and receive bounded, cited change-impact context without exposing raw CPGQL.

**Target features:**
- Deterministic version-to-version file and symbol changes with source citations
- Bounded CPG-backed impact analysis for affected callers, callees, and data-flow relationships
- REST/MCP parity with existing authorization, tenant isolation, quotas, audit records, and regression coverage

## Requirements

### Validated

- ✓ Memory-aware admission — heap reservations by CPG tier, LRU/RSS eviction (Phase 1, shipped)
- ✓ Pool worker mode — cgroup-capped per-CPG containers (Phase 2, shipped)
- ✓ Durable job queue — DB-backed jobs, dedup, backpressure (Phase 3, shipped)
- ✓ Postgres + Redis — shared catalog/cache/findings/jobs store, cross-process pool coordination (Phase 3c, shipped)
- ✓ MCP server over HTTP with 20+ tools (core, code browsing, taint analysis, custom detectors)
- ✓ Health endpoint with concurrent dependency probes
- ✓ Docker Compose full-stack deployment (MCP + Joern + Postgres + Redis)
- ✓ Chat-facing hardening (`CHAT_DEPLOY`, `ALLOWED_SOURCE_ROOTS`)
- ✓ Auto-tuned memory with over-commit guard
- ✓ Paper accepted at SVM Workshop @ ICSE 2026

### Validated in v0.7

- ✓ Secure source archive and Git remote synchronization into immutable versioned codebase snapshots (Phase 5)
- ✓ Asynchronous CPG build tracking with durable Postgres status, retry, and cancellation semantics (Phase 6)
- ✓ Compact, cited hybrid context retrieval with exact symbols, lexical search, and graph traversal (Phase 7)
- ✓ Authenticated REST/MCP interfaces with JWT Bearer, permanent MCP tokens, quotas, rate limiting, and audit logs (Phase 8)

### Active

- [ ] Compare two ready immutable versions and return an attributed, bounded change summary
- [ ] Explain the likely structural and data-flow impact of a selected change
- [ ] Deliver the comparison and impact contract consistently through REST and MCP

### Out of Scope

- Immutable GHCR deployment and VPS rollout — deferred until the backend contract stabilizes
- Vector database/embedding-first retrieval — hybrid lexical + CPG retrieval comes first
- Kubernetes / multi-node orchestration — separate infrastructure milestone
- Unrestricted raw CPGQL for untrusted agents — remains internal/admin only

## Context

- **Current stack:** Python 3.13 (FastMCP), Joern (Docker), Postgres 16, Redis 7, Docker Compose
- **Deployment target:** Ubuntu VPS at 160.250.4.40, Docker Engine with Compose v2
- **Source:** GitHub repo at `lekssays/codebadger`, currently at v0.6.2b0
- **Existing deployment:** `docker compose up -d --build` on VPS with local source sync
- **Goal:** Provide an upload-to-context backend for AI agents on top of CodeBadger's existing CPG engine
- **Data that persists:** `playground/` (repos + CPG caches), `pgdata/` (Postgres), `logs/`
- **Data that's in images:** MCP Python app, Joern + Java/Rust runtime

## Constraints

- **Platform:** Must build for `linux/amd64` (VPS architecture), not ARM64
- **Security:** Docker socket mount required — VPS must be dedicated to CodeBadger
- **Registry:** GHCR (GitHub Container Registry) — private images, VPS needs `docker login` with PAT
- **Downtime:** `docker compose up -d` redeploys with minimal downtime, but CPG builds may restart
- **Image size:** Joern image is large (~1-2GB) — first pull slow, subsequent pulls use layer cache
- **Backward compat:** Existing volumes (`pgdata/`, `playground/`) must remain compatible

## Key Decisions

| Decision | Rationale | Outcome |
|----------|-----------|---------|
| Versioned project snapshots over content-addressed CPG cache | Enables repeatable agent context and deduplicated builds | — Pending |
| ContextService hides Joern/CPGQL behind a small interface | Keeps agent-facing contracts stable while analysis evolves | — Pending |
| Hybrid lexical + graph retrieval before embeddings | Preserves explainability and reduces infrastructure for v0.7 | — Pending |
| GHCR as registry | Already in GitHub ecosystem, no additional service needed | — Pending |
| Separate MCP and Joern images | Different build dependencies, independent update cadence | — Pending |
| Persistent data outside images | CPGs, Postgres, logs survive redeploys via volume mounts | ✓ Good |
| Pool worker mode as default | Isolated OOMs, per-CPG cgroup caps | ✓ Good |

## Evolution

This document evolves at phase transitions and milestone boundaries.

**After each phase transition** (via `$gsd-transition`):
1. Requirements invalidated? → Move to Out of Scope with reason
2. Requirements validated? → Move to Validated with phase reference
3. New requirements emerged? → Add to Active
4. Decisions to log? → Add to Key Decisions
5. "What This Is" still accurate? → Update if drifted

**After each milestone** (via `$gsd-complete-milestone`):
1. Full review of all sections
2. Core Value check — still the right priority?
3. Audit Out of Scope — reasons still valid?
4. Update Context with current state

---

*Last updated: 2026-09-17 after starting v0.8 Version Intelligence & Change Impact*
