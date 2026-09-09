#!/usr/bin/env bash
#
# Sync the local .env file to the VPS.
#
# Use this when you've updated your local .env and want to push those changes
# to the VPS without a full deploy.
#
# Usage:
#   scripts/sync-env.sh [vps-host]
#
# Default VPS host: codebadger (SSH config alias)
#
# ⚠ This REPLACES the VPS .env entirely. To change a single variable, SSH
#   into the VPS and edit /opt/codebadger/.env directly instead.
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

VPS="${1:-codebadger}"
VPS_APP_DIR="/opt/codebadger"

if [[ ! -f "$ROOT/.env" ]]; then
  echo "ERROR: .env not found. Create it first: cp .env.defaults .env" >&2
  exit 1
fi

echo "→ Syncing .env to $VPS:$VPS_APP_DIR/.env ..."

# Warn if VPS .env exists
if ssh "$VPS" "test -f $VPS_APP_DIR/.env" 2>/dev/null; then
  echo "   VPS .env exists — backing up to .env.bak on VPS..."
  ssh "$VPS" "cp $VPS_APP_DIR/.env $VPS_APP_DIR/.env.bak"
fi

# Copy local .env to VPS
scp "$ROOT/.env" "$VPS:$VPS_APP_DIR/.env"

echo "   .env synced."

# Restart stack to pick up changes
echo "→ Restarting stack..."
ssh "$VPS" "cd $VPS_APP_DIR && docker compose up -d --no-build"

echo ""
echo "✅ .env synced and stack restarted."
echo "   Backup: $VPS:$VPS_APP_DIR/.env.bak"
