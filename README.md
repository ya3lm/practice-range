# Practice Range

A first-person reflex trainer in the browser whose targets learn to dodge you.
Unity 6 · ML-Agents (PPO) trained offline · Sentis inference in WebGL/WebGPU · contextual bandit adapting live · procedural everything.

**Status:** builds and runs in the browser (WebGL 2, Unity 6000.6.7f1); policies not yet trained. **Source:** https://github.com/ya3lm/practice-range · **Play:** _(link after first deploy)_ · **Plan:** [PLAN.md](PLAN.md) · **Posts:** [content/posts.md](content/posts.md)

## Quick start
```bash
tools/bootstrap.sh          # one-time: generates scenes, assets, build settings (needs Unity 6000.6.x installed)
tools/run_tests.sh          # EditMode tests
tools/build_webgl.sh        # → build/webgl
tools/serve.sh              # http://localhost:8080
```
Train a policy: `tools/train.sh mid`, then `tools/install_model.sh <run-id> mid`. See [ml/README.md](ml/README.md).

## What's where
- `unity/Assets/_Project/Core` game loop, settings (cm/360 sensitivity), per-mode config
- `unity/Assets/_Project/AI` ML-Agents agent, reward shaping, synthetic player, bandit learner, policy registry
- `unity/Assets/_Project/Targets` cube / sphere / mannequin (procedural ragdoll) / imported, dodge motor, shatter
- `unity/Assets/_Project/Import` glTF + image upload, quickhull convex collider (≤255 tris)
- `unity/Assets/_Project/Audio` synthesized SFX, no audio files
- `unity/Assets/_Project/UI` UI Toolkit HUD, settings, scoreboard
- `tools/` everything runnable from a terminal, including the click-to-photon latency rig
- `content/` post series, shot list, content safety spec

## Content policy
No faces, no blood, no gun models, no recorded gunshots. Default targets are cubes and spheres; the mannequin is opt-in.
Imported meshes are rigid statues. Details in [content/content-safety.md](content/content-safety.md).
