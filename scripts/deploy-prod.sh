#!/usr/bin/env bash
#
# Deploy a specific image tag to the production VPS.
#
# Usage:
#   IMAGE_TAG=<sha> scripts/deploy-prod.sh [vps-host]
#
# Default VPS host: codebadger (SSH config alias)
#
# Flow:
#   1. Sync code (docker-compose.yml, .env.defaults, scripts/) to VPS
#      ⚠ .env is NEVER synced — it's host-specific and created once
#   2. First deploy: create .env on VPS from .env.defaults + host overrides
#   3. Save current IMAGE_TAG for rollback
#   4. Update IMAGE_TAG in VPS .env (only this line is touched)
#   5. docker compose pull + up --no-build
#   6. Wait for /health
#   7. Run smoke test
#   8. On success: persist .last-deploy
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

VPS="${1:-codebadger}"
VPS_APP_DIR="/opt/codebadger"
VPS_PLAYGROUND_DIR="/opt/codebadger/playground"

IMAGE_TAG="${IMAGE_TAG:-}"
if [[ -z "$IMAGE_TAG" ]]; then
  echo "ERROR: IMAGE_TAG is required (e.g. IMAGE_TAG=a1b2c3d scripts/deploy-prod.sh)" >&2
  exit 1
fi

echo "🚀 Deploying IMAGE_TAG=$IMAGE_TAG to $VPS ..."

# --- 1. Sync code to VPS (excluding .env) ---
echo "→ Syncing code to VPS..."
ssh "$VPS" "mkdir -p $VPS_APP_DIR/scripts $VPS_PLAYGROUND_DIR"
rsync -avz --delete \
  --exclude='.env' \
  --exclude='playground/' \
  --exclude='pgdata/' \
  --exclude='logs/' \
  --exclude='.git/' \
  --exclude='*.pyc' \
  --exclude='__pycache__/' \
  docker-compose.yml \
  .env.defaults \
  scripts \
  "$VPS:$VPS_APP_DIR/"
echo "   Code synced (excluding .env)."

# --- 2. Create VPS .env on first deploy ---
echo "→ Checking .env on VPS..."
if ! ssh "$VPS" "test -f $VPS_APP_DIR/.env"; then
  echo "   First deploy — creating .env from .env.defaults with VPS overrides..."
  # Detect Docker socket type on VPS
  VPS_DOCKER_SOCK="/var/run/docker.sock"
  VPS_DOCKER_HOST="unix:///var/run/docker.sock"
  if ssh "$VPS" "test -S /run/user/1000/docker.sock" 2>/dev/null; then
    VPS_DOCKER_SOCK="/run/user/1000/docker.sock"
    VPS_DOCKER_HOST="unix:///run/user/1000/docker.sock"
  fi

  ssh "$VPS" "cat > $VPS_APP_DIR/.env" <<VPSENV
# ═══════════════════════════════════════════════════════════════════════════════
# .env — VPS-specific configuration (created once, NEVER overwritten by deploys).
#
# To change config: SSH into the VPS and edit /opt/codebadger/.env directly,
# then run: docker compose up -d
#
# This file overrides .env.defaults (git-tracked, synced with deploys).
# ═══════════════════════════════════════════════════════════════════════════════

# --- VPS host paths (differs from dev) ---
PLAYGROUND_HOST_PATH=$VPS_PLAYGROUND_DIR
DOCKER_HOST=$VPS_DOCKER_HOST
DOCKER_SOCK=$VPS_DOCKER_SOCK

# --- Image registry (GHCR for prod) ---
IMAGE_REGISTRY=ghcr.io/nguyenthanhhungdev140503/
IMAGE_TAG=$IMAGE_TAG

# --- Memory tuning (override if VPS has different RAM than default 10g) ---
# JOERN_MEM_LIMIT=10g
# JOERN_MEMORY_BUDGET_MB=5120

# --- GitHub token for private repo clones (optional) ---
# GITHUB_TOKEN=ghp_xxx
VPSENV
  echo "   .env created."
else
  echo "   .env exists — keeping it (not overwritten)."
fi

# --- 3. Save current tag for rollback ---
echo "→ Saving current tag on VPS..."
CURRENT_TAG=$(ssh "$VPS" "cd $VPS_APP_DIR && grep '^IMAGE_TAG=' .env | cut -d= -f2" 2>/dev/null || echo "unknown")
echo "   Current: $CURRENT_TAG"

# --- 4. Update only IMAGE_TAG in VPS .env ---
echo "→ Updating IMAGE_TAG in .env on VPS..."
ssh "$VPS" "cd $VPS_APP_DIR && sed -i 's/^IMAGE_TAG=.*/IMAGE_TAG=$IMAGE_TAG/' .env"

# --- 5. Pull and redeploy ---
echo "→ Pulling images..."
ssh "$VPS" "cd $VPS_APP_DIR && docker compose pull"

echo "→ Redeploying (up -d --no-build)..."
ssh "$VPS" "cd $VPS_APP_DIR && docker compose up -d --no-build"

# --- 6. Health check ---
echo "→ Waiting for /health ..."
MCP_PORT=$(ssh "$VPS" "cd $VPS_APP_DIR && grep '^MCP_PORT=' .env | cut -d= -f2" 2>/dev/null || echo "4242")
HEALTH_URL="http://localhost:${MCP_PORT}/health"

for i in $(seq 1 30); do
  if ssh "$VPS" "curl -fsS '$HEALTH_URL' 2>/dev/null | grep -q '\"status\"'"; then
    echo "   Health check passed."
    break
  fi
  if [[ $i -eq 30 ]]; then
    echo "❌ Health check timed out." >&2
    exit 1
  fi
  sleep 2
done

# --- 7. Smoke test ---
echo "→ Running smoke test..."
if ssh "$VPS" "cd $VPS_APP_DIR && bash scripts/smoke-test.sh"; then
  echo "   Smoke test passed."
else
  echo "❌ Smoke test failed." >&2
  exit 1
fi

# --- 8. Persist rollback state ---
echo "→ Saving rollback state..."
ssh "$VPS" "echo '$CURRENT_TAG' > /opt/codebadger/.last-deploy"

echo ""
echo "✅ Deployed $IMAGE_TAG to $VPS"
echo "   Rollback tag: $CURRENT_TAG"
echo ""
echo "💡 To change VPS config (memory, ports, etc.), SSH in and edit:"
echo "   ssh $VPS"
echo "   vim $VPS_APP_DIR/.env"
echo "   cd $VPS_APP_DIR && docker compose up -d"
