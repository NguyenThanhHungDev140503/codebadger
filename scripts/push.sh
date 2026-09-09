#!/usr/bin/env bash
#
# Push both CodeBadger images to GHCR.
# Requires: docker login ghcr.io (run once on dev machine).
#
# Usage:
#   scripts/push.sh
#
# Pushes:
#   - ghcr.io/lekssays/codebadger-mcp:<sha> + :latest
#   - ghcr.io/lekssays/codebadger-joern-server:<sha> + :latest
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

REGISTRY="ghcr.io/nguyenthanhhungdev140503"
SHA=$(git rev-parse --short HEAD)

echo "Tagging images for GHCR (sha=$SHA) ..."

docker tag "codebadger-mcp:$SHA"             "$REGISTRY/codebadger-mcp:$SHA"
docker tag "codebadger-mcp:latest"           "$REGISTRY/codebadger-mcp:latest"
docker tag "codebadger-joern-server:$SHA"    "$REGISTRY/codebadger-joern-server:$SHA"
docker tag "codebadger-joern-server:latest"  "$REGISTRY/codebadger-joern-server:latest"

echo "Pushing codebadger-mcp ..."
docker push "$REGISTRY/codebadger-mcp:$SHA"
docker push "$REGISTRY/codebadger-mcp:latest"

echo "Pushing codebadger-joern-server ..."
docker push "$REGISTRY/codebadger-joern-server:$SHA"
docker push "$REGISTRY/codebadger-joern-server:latest"

echo ""
echo "✅ Pushed to GHCR:"
echo "   $REGISTRY/codebadger-mcp:$SHA"
echo "   $REGISTRY/codebadger-mcp:latest"
echo "   $REGISTRY/codebadger-joern-server:$SHA"
echo "   $REGISTRY/codebadger-joern-server:latest"
