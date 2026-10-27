# ml/ — training the dodge policies

```bash
cd ml && ./install.sh
../tools/train.sh mid            # opens trainer; press Play in Training_Mid scene (or build a headless player first)
uv run scripts/plot_training.py results/<run-id>
../tools/install_model.sh <run-id> mid
```

Three behaviours, one per range mode, each with its own curriculum: the synthetic player starts sloppy (6° aim
noise, 320 ms reaction) and sharpens as the policy's reward crosses thresholds. Rewards are defined once in
`unity/Assets/_Project/AI/RewardShaping.cs` and covered by EditMode tests.

Observation (12 floats, stacked ×2): player position (2), distance, aim angle, aim offset (2), aim angular velocity,
time since shot, shot-this-step, own velocity (2), cover state. Actions: continuous (vx, vz) + discrete peek/hide.

Expect ~15–30 min on an M-series CPU per mode for 3M steps with 16 arenas × 2 headless envs. `time_scale` is 1 on purpose (see CLAUDE.md gotchas). The first post-worthy checkpoint
usually appears around 300k steps, when the policy learns to strafe perpendicular to the aim ray.

## Results log

| Run | Mode | Steps | Final reward (last 10 summaries) | Notes |
|---|---|---|---|---|
| mid_v1–v3 | mid | – | – | Invalid: trainer `time_scale: 20` made each Update frame 0.33 s; synthetic player never fired after the acquisition fix. See CLAUDE.md gotchas. |
| mid_v4 | mid | 3.0M | 1.99 | First valid run. Learned to maximise distance (angular aim noise → more misses far away) and ran to the arena edge, out of a stationary player's view. Led to the per-mode view-cone clamp. |
| close_v1 | close | 3.0M | 8.55 | Same pre-clamp arena. |
| far_v1 | far | 3.0M | 6.56 | Cover peek/hide works; episode length rose 15 → 70 decision steps at the sharpest lesson. |
| *_v2 | all | 3.0M | – | Retrained with the view-cone clamp (`ModeConfig.maxLateralAngleDeg`). |

Charts: `results/<run>/training.png` (made by `scripts/plot_training.py`). The dashed lines are curriculum lesson changes; the
reward dips at each one and recovers higher, which is the honest version of "it learned".
