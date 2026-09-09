#!/usr/bin/env bash
#
# Build the CodeBadger MCP server image with SHA tag.
#
# Usage:
#   scripts/build-mcp.sh
#
# Output:
#   - codebadger-mcp:latest
#   - codebadger-mcp:<git-sha>
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

SHA=$(git rev-parse --short HEAD)
echo "Building codebadger-mcp:$SHA ..."

docker build \
  --platform linux/amd64 \
  -f Dockerfile.mcp \
  -t "codebadger-mcp:latest" \
  -t "codebadger-mcp:$SHA" \
  .

echo "✅ codebadger-mcp:$SHA built successfully"
echo "   Tags: codebadger-mcp:latest, codebadger-mcp:$SHA"
