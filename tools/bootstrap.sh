#!/usr/bin/env bash
# First-time setup: opens the project in batch mode (generates ProjectSettings/Library, resolves packages),
# then runs ProjectBootstrap.Run to create mode assets, scenes, prefabs, and build settings.
source "$(dirname "$0")/common.sh"
LOG="$LOGDIR/bootstrap.log"
echo "Bootstrapping with Unity $UNITY_VERSION → $LOG"
"$UNITY_BIN" -batchmode -nographics -quit -projectPath "$PROJECT" \
  -executeMethod PracticeRange.Editor.ProjectBootstrap.Run -logFile "$LOG"
STATUS=$?
grep -E "\[Bootstrap\]|error CS|Exception" "$LOG" | tail -40 || true
exit $STATUS
