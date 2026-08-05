# Roadmap: CodeBadger

**Created:** 2026-08-05
**Granularity:** Standard
**Project:** CodeBadger — containerized MCP server for Joern CPG analysis

---

### Phase 1: Production Docker Deployment (Immutable Images)
**Goal:** Replace `docker compose build` on VPS with immutable images from GHCR, enabling reproducible deploys and instant rollback.
**Success Criteria:**
1. Both `codebadger-mcp` and `codebadger-joern-server` images are built, tagged with git SHA, and pushed to GHCR
2. `docker-compose.yml` references images (not `build:` context) with `${IMAGE_TAG}`
3. VPS at 160.250.4.40 can `docker compose pull && docker compose up -d --no-build`
4. Rollback works by changing `IMAGE_TAG` to previous SHA
5. `/health` returns `up` after deploy; smoke test CPG generation succeeds
6. All persistent data (`playground/`, `pgdata/`, `logs/`) intact after redeploy

**Requirements:** DEPLOY-01, DEPLOY-02, DEPLOY-03, DEPLOY-04, DEPLOY-05, DEPLOY-06, CONF-01, CONF-02, CONF-03, DATA-01, DATA-02, DATA-03, BUILD-01, BUILD-02, BUILD-03, HEAL-01, HEAL-02

### Phase 2: CI/CD Pipeline
**Goal:** Automate build and push via GitHub Actions on push to `main`, with smoke test validation.
**Success Criteria:**
1. GitHub Actions workflow builds both images for `linux/amd64` on push to `main`
2. Images are pushed to GHCR automatically
3. VPS can be configured to auto-deploy (or manual trigger from CI)

### Phase 3: Multi-Host Scheduling
**Goal:** Spread the worker pool across multiple machines coordinated through shared Postgres + Redis state.
**Success Criteria:**
1. Multiple VPS instances share one Postgres/Redis backend
2. Worker pool scales horizontally across hosts
3. Memory budget and LRU eviction work cross-host

### Phase 4: Findings as First-Class Data
**Goal:** Surface persisted `findings` store through MCP tools — query, dedup, and track analysis results across runs.
**Success Criteria:**
1. MCP tools for listing, querying, and deduplicating findings
2. Findings survive across runs and CPG rebuilds
3. Cross-referencing findings across codebases

---

*Roadmap created: 2026-08-05*
