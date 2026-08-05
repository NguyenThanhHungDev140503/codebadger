# CodeBadger

## What This Is

CodeBadger is a containerized MCP (Model Context Protocol) server that gives AI agents deep, queryable access to codebase structure and data flow through Joern Code Property Graphs (CPGs). It supports 14+ languages (Java, C/C++, JavaScript, Python, Go, Kotlin, C#, PHP, Ruby, Swift, etc.) for both program analysis and vulnerability analysis — useful for academic research and industry security/engineering work.

## Core Value

AI agents can query and analyze production codebases through CPGs with memory-safe, scalable infrastructure — enabling vulnerability discovery, taint tracking, and deep code understanding at scale.

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

### Active

- [ ] **DEPLOY-01**: Production Docker deployment with immutable images via GHCR registry
- [ ] **DEPLOY-02**: Automated multi-arch image build (`linux/amd64`) and push pipeline
- [ ] **DEPLOY-03**: VPS deployment at 160.250.4.40 with `docker compose pull` + `up` workflow
- [ ] **DEPLOY-04**: Versioned image tags (git SHA) with rollback capability

### Out of Scope

- Kubernetes / multi-node orchestration — single VPS deployment is sufficient for current scale
- CI/CD pipeline with full test gates — manual build + push for now, CI can be added later
- Multi-tenant deployment — single-tenant trust domain per deployment

## Context

- **Current stack:** Python 3.13 (FastMCP), Joern (Docker), Postgres 16, Redis 7, Docker Compose
- **Deployment target:** Ubuntu VPS at 160.250.4.40, Docker Engine with Compose v2
- **Source:** GitHub repo at `lekssays/codebadger`, currently at v0.6.2b0
- **Existing deployment:** `docker compose up -d --build` on VPS with local source sync
- **Goal:** Replace build-on-VPS with immutable images from GHCR for reproducible, rollback-friendly deploys
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
| Immutable Docker images with git SHA tags | Reproducible builds, instant rollback, no VPS compiler deps | — Pending |
| GHCR as registry | Already in GitHub ecosystem, no additional service needed | — Pending |
| Separate MCP and Joern images | Different build dependencies, independent update cadence | — Pending |
| Persistent data outside images | CPGs, Postgres, logs survive redeploys via volume mounts | ✓ Good |
| Pool worker mode as default | Isolated OOMs, per-CPG cgroup caps | ✓ Good |

---

*Last updated: 2026-08-05 after initialization*
