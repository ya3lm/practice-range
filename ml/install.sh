#!/usr/bin/env bash
# Creates ml/.venv with Python 3.10.12 and installs ML-Agents 1.1.0 (two steps, see requirements.txt).
set -euo pipefail
cd "$(dirname "$0")"
[[ -d .venv ]] || uv venv --python 3.10.12
uv pip install -r requirements.txt
uv pip install --no-deps mlagents==1.1.0 mlagents-envs==1.1.0
uv run mlagents-learn --help > /dev/null && echo "ml env ready: $(uv run python --version)"
