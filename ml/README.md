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
| mid_v5 | mid | 3.0M | 0.98 | View-cone clamp + 5 m standoff, but the 1.2°/160 ms final lesson was an aimbot at mid range; policy degenerated (parked in a corner). |
| close_v2 | close | 3.0M | 4.60 | Same geometry fixes. |
| close_v3 | close | 3.0M | 4.40 | **Shipped.** Final lesson relaxed to 2.5° / 180 ms. |
| mid_v6 | mid | 3.0M | 1.25 | **Shipped.** Final lesson relaxed to 2.0° / 190 ms. |
| far_v2 | far | 3.0M | 6.43 | **Shipped.** Same geometry fixes; cover behaviour intact. |

Curriculum note: the final lesson was 1.2° / 160 ms for every mode. At mid range with no cover that is an aimbot and the
policy degenerated (parks in a corner, ~1 s survival). The final lesson for mid and close is now a strong human
(2.0° / 190 ms and 2.5° / 180 ms); far keeps 0.8° / 160 ms because cover makes it beatable.

Charts: `results/<run>/training.png` (made by `scripts/plot_training.py`). The dashed lines are curriculum lesson changes; the
reward dips at each one and recovers higher, which is the honest version of "it learned".
