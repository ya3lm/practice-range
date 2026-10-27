#!/usr/bin/env bash
# Needs butler (https://itch.io/docs/butler/) and ITCH_USER, ITCH_GAME env vars. Channel: html5.
set -euo pipefail
REPO="$(cd "$(dirname "$0")/.." && pwd)"
: "${ITCH_USER:?set ITCH_USER}"; : "${ITCH_GAME:?set ITCH_GAME}"
command -v butler >/dev/null || { echo "install butler: brew install --cask itch  (or download from itch.io/docs/butler)"; exit 1; }
butler push "$REPO/build/webgl" "$ITCH_USER/$ITCH_GAME:html5" --userversion "$(date +%Y.%m.%d-%H%M)"
