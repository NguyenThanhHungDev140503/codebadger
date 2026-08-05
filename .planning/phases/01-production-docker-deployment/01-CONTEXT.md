# 01-CONTEXT: Production Docker Deployment (Immutable Images)

**Phase:** 1  
**Status:** Decisions captured from $gsd-discuss-phase  
**Date:** 2026-08-05  
**Goal:** Replace `docker compose build` on VPS with immutable images from GHCR, enabling reproducible deploys and instant rollback.

## Current State

- VPS at 160.250.4.40, Ubuntu, Docker Compose v2
- 4 services: MCP server (Python 3.13), Joern server (Docker), Postgres 16, Redis 7
- Current deploy: `docker compose up -d --build` on VPS — builds from source, no image versioning
- Persistent volumes: `playground/` (sources + CPG caches), `pgdata/` (Postgres), `logs/`
- Existing scripts: `scripts/deploy.sh` (wrapper around docker compose), `scripts/recommend_config.py`

## Decisions (from $gsd-discuss-phase 1)

### 1. Registry Setup

- **Registry:** GHCR (GitHub Container Registry) — private images
- **Image naming:** Flat, repo-level
  - `ghcr.io/lekssays/codebadger-mcp`
  - `ghcr.io/lekssays/codebadger-joern-server`
- **Auth:** Fine-grained PAT scoped to `lekssays/codebadger` repo, stored via `docker-credential-secretservice` on VPS (not plaintext `.env`)
- **Tags:** Git short SHA as canonical tag (e.g. `:a1b2c3d`), `latest` as convenience alias. Production `.env` always points to a specific SHA — never `latest`
- **Platform:** Multi-arch manifest for `linux/amd64` (VPS) + `linux/arm64` (future flexibility)

### 2. VPS Deploy Flow

- **Deploy method:** SSH script from dev machine (`scripts/deploy-prod.sh`) — easier to transition to CI/CD later than manual SSH + VPS-local script
- **Health check:** `GET /health` probes all dependencies (Postgres, Redis, Joern), then smoke test generates a CPG from a small snippet and queries it
- **Downtime:** Normal restart (`docker compose up -d`), 5-10s downtime — acceptable for MCP server (not public web app)

### 3. Build Scripts

- **Language:** Bash, in `scripts/` directory (matching existing convention: `deploy.sh`)
- **Structure:**
  - `scripts/build-mcp.sh` — builds `codebadger-mcp` image
  - `scripts/build-joern.sh` — builds `codebadger-joern-server` image
  - `scripts/build.sh` — entry point, runs both
- **Push:** Separate `scripts/push.sh` — push only after review, not automatic after build
- **Tagging:** `git rev-parse --short HEAD` for SHA tag, plus `latest`

### 4. Compose Changes

- **Approach:** 1 file + `.env` override — not separate prod/dev compose files (DRY, less maintenance)
- **`IMAGE_REGISTRY` variable:** `ghcr.io/lekssays/` for prod (resolves to GHCR images), empty for dev (resolves to local image, falls back to `build:` context)
- **Pattern:**
  ```yaml
  image: ${IMAGE_REGISTRY:-}codebadger-mcp:${IMAGE_TAG:-latest}
  ```
- Prod workflow: set `IMAGE_REGISTRY=ghcr.io/lekssays/` and `IMAGE_TAG=<sha>` in `.env`, run `docker compose pull && docker compose up -d --no-build`
- Dev workflow: leave `IMAGE_REGISTRY` unset or empty, `IMAGE_TAG=latest`, `docker compose up -d --build` still works

### 5. Rollback

- **Approach:** Script `scripts/rollback.sh` — one-command rollback
- **Mechanism:** `deploy-prod.sh` saves the current tag to `/opt/codebadger/.last-deploy` before deploying a new tag. `rollback.sh` reads `.last-deploy`, sets `IMAGE_TAG`, and runs `pull` + `up`
- **Why script over manual:** Faster, safer (no wrong-tag mistake), CI/CD-ready (auto-rollback if health check fails)

### 6. Data Integrity

- Volumes (`playground/`, `pgdata/`, `logs/`) are host-mounted, not in images — survive any redeploy
- `pgdata/` stays outside `playground/` (Joern containers mount playground, must not reach DB files)

## Requirements Covered

All 17 v1 requirements from `.planning/REQUIREMENTS.md`:
DEPLOY-01 through DEPLOY-06, CONF-01 through CONF-03, DATA-01 through DATA-03, BUILD-01 through BUILD-03, HEAL-01 through HEAL-02

## Files to Create/Modify

| File | Action | Description |
|------|--------|-------------|
| `docker-compose.yml` | Modify | Replace `build:` with `image:` pattern using `${IMAGE_REGISTRY}` and `${IMAGE_TAG}` |
| `.env` | Create/Modify | Add `IMAGE_REGISTRY`, `IMAGE_TAG` variables |
| `scripts/build-mcp.sh` | Create | Build `codebadger-mcp` image with SHA tag |
| `scripts/build-joern.sh` | Create | Build `codebadger-joern-server` image with SHA tag |
| `scripts/build.sh` | Create | Entry point — runs both build scripts |
| `scripts/push.sh` | Create | Push both images to GHCR |
| `scripts/deploy-prod.sh` | Create | SSH to VPS, pull images, up, health check, save `.last-deploy` |
| `scripts/rollback.sh` | Create | Read `.last-deploy`, deploy previous tag |
| `scripts/smoke-test.sh` | Create | Post-deploy CPG generation + query test |

## Out of Scope (for Phase 1)

- GitHub Actions CI/CD pipeline — Phase 2
- Multi-host scheduling — Phase 3
- Kubernetes / Helm chart
- Auto-scaling worker pool
