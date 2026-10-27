# ml/ — training the dodge policies

```bash
cd ml && uv venv && uv pip install -r requirements.txt
../tools/train.sh mid            # opens trainer; press Play in Training_Mid scene (or build a headless player first)
uv run scripts/plot_training.py results/<run-id>
../tools/install_model.sh <run-id> mid
```

Three behaviours, one per range mode, each with its own curriculum: the synthetic player starts sloppy (6° aim
noise, 320 ms reaction) and sharpens as the policy's reward crosses thresholds. Rewards are defined once in
`unity/Assets/_Project/AI/RewardShaping.cs` and covered by EditMode tests.

Observation (12 floats, stacked ×2): player position (2), distance, aim angle, aim offset (2), aim angular velocity,
time since shot, shot-this-step, own velocity (2), cover state. Actions: continuous (vx, vz) + discrete peek/hide.

Expect ~1–2 h on CPU per mode for 3M steps with 16 arenas at time_scale 20. The first post-worthy checkpoint
usually appears around 300k steps, when the policy learns to strafe perpendicular to the aim ray.
