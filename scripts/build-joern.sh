#!/usr/bin/env bash
#
# Build the CodeBadger Joern server image with SHA tag.
#
# Usage:
#   scripts/build-joern.sh
#
# Output:
#   - codebadger-joern-server:latest
#   - codebadger-joern-server:<git-sha>
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

SHA=$(git rev-parse --short HEAD)
echo "Building codebadger-joern-server:$SHA ..."

docker build \
  --platform linux/amd64 \
  -f Dockerfile \
  -t "codebadger-joern-server:latest" \
  -t "codebadger-joern-server:$SHA" \
  .

echo "✅ codebadger-joern-server:$SHA built successfully"
echo "   Tags: codebadger-joern-server:latest, codebadger-joern-server:$SHA"
