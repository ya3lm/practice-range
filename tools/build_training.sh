#!/usr/bin/env bash
# Usage: tools/build_training.sh close|mid|far   → build/training/PracticeRangeTraining_<Mode>.app (headless-capable macOS player)
source "$(dirname "$0")/common.sh"
MODE="${1:?mode}"; Mode="$(tr '[:lower:]' '[:upper:]' <<< "${MODE:0:1}")${MODE:1}"
OUT="$REPO/build/training/PracticeRangeTraining_$Mode.app"; LOG="$LOGDIR/build-training-$Mode.log"
"$UNITY_BIN" -batchmode -nographics -quit -projectPath "$PROJECT" -buildTarget OSXUniversal \
  -executeMethod PracticeRange.Editor.BuildScript.MacTraining -mode "$Mode" -outputPath "$OUT" -logFile "$LOG"
grep -E "\[Build\]|error CS" "$LOG" | tail -10
