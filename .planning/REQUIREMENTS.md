# Requirements: CodeBadger

**Defined:** 2026-08-05
**Core Value:** AI agents can query and analyze production codebases through CPGs with memory-safe, scalable infrastructure

## v1 Requirements

### Deployment

- [ ] **DEPLOY-01**: Docker images are built as immutable artifacts tagged by git SHA (e.g. `ghcr.io/lekssays/codebadger-mcp:a1b2c3d`), not `latest`
- [ ] **DEPLOY-02**: Multi-arch build targets `linux/amd64` to match VPS architecture
- [ ] **DEPLOY-03**: Images are pushed to GHCR (GitHub Container Registry) from dev machine or CI
- [ ] **DEPLOY-04**: VPS pulls images via `docker compose pull` and redeploys with `docker compose up -d --no-build`
- [ ] **DEPLOY-05**: Rollback is achievable by changing `IMAGE_TAG` to a previous SHA and re-running `pull`/`up`
- [ ] **DEPLOY-06**: Compose file references images (not `build:` context) for production

### Configuration

- [ ] **CONF-01**: `.env` file with `IMAGE_TAG` variable controls which image version is deployed
- [ ] **CONF-02**: `docker-compose.yml` uses `${IMAGE_TAG}` for both MCP and Joern image tags
- [ ] **CONF-03**: VPS has `docker login` to GHCR with a GitHub PAT for private image access

### Data Integrity

- [ ] **DATA-01**: All persistent data (`playground/`, `pgdata/`, `logs/`) survive redeploys via volume mounts
- [ ] **DATA-02**: `pgdata/` remains outside `playground/` (security — Joern containers mount playground)
- [ ] **DATA-03**: Existing deployed CPGs and Postgres catalog continue working after image-based deploy

### Build

- [ ] **BUILD-01**: A build script or documented commands exist for building both images (`codebadger-mcp`, `codebadger-joern-server`)
- [ ] **BUILD-02**: Build script tags images with current `git rev-parse HEAD` short SHA
- [ ] **BUILD-03**: Build script pushes both images to GHCR

### Health & Validation

- [ ] **HEAL-01**: After deploy, `GET /health` returns `status: "up"` with all dependencies healthy
- [ ] **HEAL-02**: A smoke test generates a CPG from a small snippet and queries it successfully post-deploy

## v2 Requirements

- **CI-01**: GitHub Actions workflow auto-builds and pushes images on push to `main`
- **CI-02**: Automated smoke test in CI pipeline after deploy
- **MON-01**: Health check monitoring with alert on degraded state

## Out of Scope

| Feature | Reason |
|---------|--------|
| Multi-host scheduling (roadmap "next") | Separate phase; deployment foundation needed first |
| Findings as first-class data | Separate phase after deployment is productionized |
| Kubernetes Helm chart | Single VPS, Docker Compose sufficient |
| Auto-scaling worker pool | Single VPS, manual scaling adequate |

## Traceability

| Requirement | Phase | Status |
|-------------|-------|--------|
| DEPLOY-01 | Phase 1 | Pending |
| DEPLOY-02 | Phase 1 | Pending |
| DEPLOY-03 | Phase 1 | Pending |
| DEPLOY-04 | Phase 1 | Pending |
| DEPLOY-05 | Phase 1 | Pending |
| DEPLOY-06 | Phase 1 | Pending |
| CONF-01 | Phase 1 | Pending |
| CONF-02 | Phase 1 | Pending |
| CONF-03 | Phase 1 | Pending |
| DATA-01 | Phase 1 | Pending |
| DATA-02 | Phase 1 | Pending |
| DATA-03 | Phase 1 | Pending |
| BUILD-01 | Phase 1 | Pending |
| BUILD-02 | Phase 1 | Pending |
| BUILD-03 | Phase 1 | Pending |
| HEAL-01 | Phase 1 | Pending |
| HEAL-02 | Phase 1 | Pending |

**Coverage:**
- v1 requirements: 17 total
- Mapped to phases: 17
- Unmapped: 0 ✓

---

*Requirements defined: 2026-08-05*
*Last updated: 2026-08-05 after initialization*
