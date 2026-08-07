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

echo "   Rolling back to: $PREV_TAG"

# Revert IMAGE_TAG in .env
ssh "$VPS" "cd $VPS_APP_DIR && sed -i 's/^IMAGE_TAG=.*/IMAGE_TAG=$PREV_TAG/' .env"

# Pull and redeploy
echo "→ Pulling image $PREV_TAG..."
ssh "$VPS" "cd $VPS_APP_DIR && docker compose pull"

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
