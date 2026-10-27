#!/usr/bin/env bash
# Shared helpers: locate the Unity editor for this project.
set -euo pipefail
REPO="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
PROJECT="$REPO/unity"
LOGDIR="$REPO/build/logs"; mkdir -p "$LOGDIR"
VERSION_FILE="$PROJECT/ProjectSettings/ProjectVersion.txt"
if [[ -f "$VERSION_FILE" ]]; then
  UNITY_VERSION="$(sed -n 's/^m_EditorVersion: //p' "$VERSION_FILE")"
else
  UNITY_VERSION="${UNITY_VERSION:-6000.6.7f1}"
fi
UNITY_BIN="/Applications/Unity/Hub/Editor/$UNITY_VERSION/Unity.app/Contents/MacOS/Unity"
if [[ ! -x "$UNITY_BIN" ]]; then
  echo "Unity $UNITY_VERSION not found at $UNITY_BIN" >&2
  echo "Installed:"; ls /Applications/Unity/Hub/Editor/ >&2
  exit 2
fi
