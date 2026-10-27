#!/usr/bin/env bash
# Usage: tools/train.sh close|mid|far [run-id] [--resume]
# Trains in the Editor (Training_<Mode> scene) via mlagents-learn. Press Play when the trainer says "Listening".
# For headless training build a player: tools/build_training.sh <mode>, then pass --env.
source "$(dirname "$0")/common.sh"
MODE="${1:?mode: close|mid|far}"; shift || true
Mode="$(tr '[:lower:]' '[:upper:]' <<< "${MODE:0:1}")${MODE:1}"
RUN_ID="${1:-${MODE}_$(date +%Y%m%d_%H%M)}"; shift || true
cd "$REPO/ml"
[[ -d .venv ]] || { uv venv && uv pip install -r requirements.txt; }
ENV_ARGS=()
if [[ -d "$REPO/build/training/PracticeRangeTraining_$Mode.app" ]]; then
  ENV_ARGS=(--env "$REPO/build/training/PracticeRangeTraining_$Mode.app" --num-envs 2 --no-graphics)
  echo "Using headless training build for $Mode"
else
  echo "No training build found; open unity/Assets/_Project/Scenes/Training_$Mode.unity in the Editor and press Play when prompted."
fi
uv run mlagents-learn "configs/dodge_${MODE}.yaml" --run-id="$RUN_ID" --results-dir=results ${ENV_ARGS[@]+"${ENV_ARGS[@]}"} "$@"
echo "Done. Model: ml/results/$RUN_ID/Dodge$Mode.onnx → copy to unity/Assets/_Project/Resources/Models/Dodge$Mode.onnx"
