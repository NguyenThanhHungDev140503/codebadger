#!/usr/bin/env bash
#
# Build both CodeBadger images with SHA tags.
#
# Usage:
#   scripts/build.sh
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

echo "=== Building codebadger-mcp ==="
"$ROOT/scripts/build-mcp.sh"

echo ""
echo "=== Building codebadger-joern-server ==="
"$ROOT/scripts/build-joern.sh"

SHA=$(git rev-parse --short HEAD)
echo ""
echo "✅ Both images built: $SHA"
