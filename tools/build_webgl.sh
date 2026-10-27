#!/usr/bin/env bash
# Usage: tools/build_webgl.sh [--dev] [--webgpu]
source "$(dirname "$0")/common.sh"
OUT="$REPO/build/webgl"; LOG="$LOGDIR/build-webgl.log"
EXTRA=()
for a in "$@"; do case "$a" in --dev) EXTRA+=(-dev);; --webgpu) EXTRA+=(-webgpu);; esac; done
echo "Building WebGL with Unity $UNITY_VERSION → $OUT (log: $LOG)"
"$UNITY_BIN" -batchmode -nographics -quit -projectPath "$PROJECT" -buildTarget WebGL \
  -executeMethod PracticeRange.Editor.BuildScript.WebGL -outputPath "$OUT" ${EXTRA[@]+"${EXTRA[@]}"} -logFile "$LOG"
STATUS=$?
grep -E "\[Build\]|error CS|Build Failed|Exception" "$LOG" | tail -20 || true
[[ $STATUS -eq 0 ]] && du -sh "$OUT" && echo "Serve locally: tools/serve.sh"
exit $STATUS
