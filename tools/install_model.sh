#!/usr/bin/env bash
# Usage: tools/install_model.sh <run-id> close|mid|far   → copies the ONNX into Resources/Models so PolicyRegistry finds it.
REPO="$(cd "$(dirname "$0")/.." && pwd)"
RUN="${1:?run-id}"; MODE="${2:?mode}"; Mode="$(tr '[:lower:]' '[:upper:]' <<< "${MODE:0:1}")${MODE:1}"
SRC="$REPO/ml/results/$RUN/Dodge$Mode.onnx"
[[ -f "$SRC" ]] || { echo "missing $SRC"; ls "$REPO/ml/results/$RUN"; exit 1; }
cp "$SRC" "$REPO/unity/Assets/_Project/Resources/Models/Dodge$Mode.onnx" && echo "installed Dodge$Mode.onnx"
