#!/usr/bin/env bash
# Publishes build/webgl to the gh-pages branch. Requires a GitHub remote named origin.
set -euo pipefail
REPO="$(cd "$(dirname "$0")/.." && pwd)"
cd "$REPO"; [[ -d build/webgl ]] || { echo "no build"; exit 1; }
git remote get-url origin >/dev/null || { echo "add a GitHub remote first"; exit 1; }
TMP="$(mktemp -d)"; cp -R build/webgl/. "$TMP/"; touch "$TMP/.nojekyll"
cd "$TMP"; git init -q; git checkout -q -b gh-pages; git add -A; git commit -qm "deploy $(date -u +%FT%TZ)"
git push -f "$(git -C "$REPO" remote get-url origin)" gh-pages
echo "pushed gh-pages. Enable Pages → gh-pages branch in repo settings. Note: Pages does not send Content-Encoding for .gz; decompression fallback is enabled in the build."
