#!/usr/bin/env bash
#
# Rollback to the previously deployed image tag.
#
# Usage:
#   scripts/rollback.sh [vps-host]
#
# Default VPS host: codebadger (SSH config alias)
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

VPS="${1:-codebadger}"
VPS_APP_DIR="/opt/codebadger"

echo "🔙 Rolling back on $VPS ..."

# Read previous tag
PREV_TAG=$(ssh "$VPS" "cat /opt/codebadger/.last-deploy 2>/dev/null" || true)
if [[ -z "$PREV_TAG" || "$PREV_TAG" == "unknown" ]]; then
  echo "ERROR: No previous deployment tag found (/opt/codebadger/.last-deploy is empty or missing)." >&2
  exit 1
fi
if [[ ! "$PREV_TAG" =~ ^[A-Za-z0-9][A-Za-z0-9._-]*$ ]]; then
  echo "ERROR: Refusing unsafe deployment tag in /opt/codebadger/.last-deploy." >&2
  exit 1
fi

echo "   Rolling back to: $PREV_TAG"

# Revert IMAGE_TAG in .env
ssh "$VPS" "cd $VPS_APP_DIR && sed -i 's/^IMAGE_TAG=.*/IMAGE_TAG=$PREV_TAG/' .env"

# Pull and redeploy
echo "→ Pulling image $PREV_TAG..."
ssh "$VPS" "cd $VPS_APP_DIR && docker compose pull"

# Keep local fallback aliases aligned with the immutable image Compose just
# pulled.  Do not pull mutable registry :latest: it could move to a newer
# release while this rollback is in progress.
IMAGE_REGISTRY=$(ssh "$VPS" "cd $VPS_APP_DIR && sed -n 's/^IMAGE_REGISTRY=//p' .env | tail -1 | sed 's#/$##'")
if [[ ! "$IMAGE_REGISTRY" =~ ^ghcr\.io/[A-Za-z0-9._/-]+$ ]]; then
  echo "ERROR: Expected a valid GHCR IMAGE_REGISTRY in $VPS_APP_DIR/.env." >&2
  exit 1
fi

echo "→ Re-tagging local latest aliases..."
ssh "$VPS" "docker tag '$IMAGE_REGISTRY/codebadger-mcp:$PREV_TAG' codebadger-mcp:latest && \
  docker tag '$IMAGE_REGISTRY/codebadger-joern-server:$PREV_TAG' codebadger-joern-server:latest"

echo "→ Redeploying..."
ssh "$VPS" "cd $VPS_APP_DIR && docker compose up -d --no-build"

# Health check
echo "→ Waiting for /health ..."
MCP_PORT=$(ssh "$VPS" "cd $VPS_APP_DIR && grep '^MCP_PORT=' .env | cut -d= -f2" 2>/dev/null || echo "4242")
HEALTH_URL="http://localhost:${MCP_PORT}/health"

for i in $(seq 1 30); do
  if ssh "$VPS" "curl -fsS '$HEALTH_URL' 2>/dev/null | grep -q '\"status\"'"; then
    echo "   Health check passed."
    break
  fi
  if [[ $i -eq 30 ]]; then
    echo "⚠️  Health check timed out — check manually." >&2
    exit 1
  fi
  sleep 2
done

echo ""
echo "✅ Rolled back to $PREV_TAG"
