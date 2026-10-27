# Practice Range — Project Plan

Goal: a first-person reflex trainer (Aimlabs-style) whose targets learn to dodge you with
reinforcement learning. Built in public to earn attention and credibility with hiring managers
and recruiters for software/hardware engineering roles.

Working name: **Practice Range** (keep "trainer / range / reflex" language, avoid "shooter" in posts).

---

## 1. Decisions, in one table

| Question | Recommendation | Why |
|---|---|---|
| Engine | Unity 6 (6000.0.39f1, already installed), URP, C# | ML-Agents, Sentis, ragdoll physics, WebGL and Steam all in one toolchain. Godot/Three.js would mean hand-rolling RL. |
| First platform | **Browser (WebGL 2)** hosted on itch.io + GitHub Pages | Zero-friction link is the whole attention play. A recruiter will click a link; they will not install Steam. |
| Second platform | Steam, after ~3 dev-log posts land | Steam page is a credibility badge and gives a second wave of posts (wishlists, review, launch). Costs $100 fee. |
| RL framework | Unity ML-Agents 4.x (PPO), trained in Editor via Python; ship the `.onnx` policy, run with Sentis CPU/Burst backend | Only mature RL path inside Unity. Browser can run inference, not training. |
| "It learns while you play" | Thin in-game learner in C# (contextual bandit / tabular Q over 8–16 discrete dodge moves), layered on top of the PPO policy | Honest live adaptation the viewer can see in a 20-second clip. PPO policy = depth for engineers; bandit = visible learning for everyone else. |
| Ragdoll | Unity PhysX ragdoll on a 9-bone low-poly mannequin; impulse at hit point scaled by weapon | Ragdoll is the single most shareable visual. Mannequin (not human) keeps it LinkedIn-safe. |
| Custom targets | Runtime glTF import (glTFast) + PNG/JPG billboards; auto convex-hull collider | Lets people shoot their own logo / mesh. Huge comment bait, low engineering cost. |
| Art | Flat-shaded, unlit, 3 colors per mode, high contrast | Readable at 320px muted in a feed. Fast to make. |
| Claude Code integration | Repo-first: CLAUDE.md, Unity batch-mode CLI, Unity Test Framework from CLI, Unity MCP (mcp-unity) for scene edits, Python ML-Agents runs launched from Claude Code | Claude Code is a terminal tool; make the project fully drivable from the terminal and it becomes a dev partner instead of a code autocomplete. |

---

## 2. Why this project can work (and what works against it)

### What works for you
- **Visible intelligence.** A target that dodges is a story in one frame. Most portfolio projects need a paragraph.
- **Measurable.** Hit rate vs. training episodes is a real chart. Engineers trust charts.
- **Hardware crossover.** Input latency, mouse polling, photodiode click-to-photon measurement, RL compute profiling: these are hardware-engineer posts, not game-dev posts.
- **Serial.** Every subsystem (RL, physics, upload pipeline, audio, WebGL perf) is its own post.

### What works against you (take these seriously)
1. **"Shooter" on LinkedIn.** Some hiring managers in defense/enterprise will be fine; others will scroll past or quietly judge. Mitigation: no humans, no blood, call it a reflex/aim trainer, lead with the AI.
2. **External links are throttled.** LinkedIn suppresses posts whose body contains a link. Put the playable link in the first comment, native-upload the video.
3. **Virality is not the goal; the right 50 people are.** A post with 300 likes from the right engineers beats 30k from gamers. Write every post for a staff engineer, not for Reddit.
4. **Controversy cuts both ways.** "Humor" aimed at the viewer ("my AI thinks you're bad at this") is fine. Edgy controversy (politics, real people as targets) kills credibility. Keep controversy *technical*: "RL is overkill for this and I did it anyway, here's what I learned."
5. **Scope creep is the real enemy.** Aimlabs has hundreds of settings. Ship 8.

---

## 3. Attention psychology, applied honestly

What the evidence supports (not marketing folklore):
- **Information gap (Loewenstein, 1994).** Curiosity spikes when someone sees a gap between what they know and what they want to know. The hook should pose a gap, not state a result.
- **Muted autoplay.** Most feed video is watched without sound. The first frame and first on-screen caption carry everything; audio is a bonus for people who tap in.
- **Carousel preview.** LinkedIn staff state the opening seconds appear in the "Videos for You" carousel, so a static-ish first second is wasted.
- **Only ~100 characters of post copy show** before "see more". Those 100 characters are the second hook.

Hook templates for this project (first 2–5 seconds, muted, 1:1 or 4:5 crop):
1. **Dodge-at-fire.** Slow-mo: crosshair on target, muzzle flash, target sidesteps the tracer by inches, ragdoll of the *previous* target still falling in the background. Caption: "Episode 4,000. It stopped getting hit."
2. **Before/after split.** Left: episode 0, target gets shredded. Right: episode 10k, target weaving. No words for 2s, then "same code, 40 minutes of training."
3. **Score reversal.** HUD shows accuracy dropping 94% → 61% over 20s while the target gets smarter. Caption: "I built an aim trainer. Then I taught the targets to fight back."
4. **The upload gag.** Someone drags a `.glb` of a rubber duck in, it gets a collider, learns to dodge. Caption: "Yes, you can upload your own target. No, it will not hold still."
5. **Hardware hook.** Photodiode taped to the monitor, oscilloscope trace, "click → photon: 11.3 ms". Different audience, same project.

Credibility layer (seconds 5–30): one chart, one line of architecture, one honest limitation. Every post ends with a limitation. That is what separates "engineer" from "content creator" in a hiring manager's head.

Color and sound:
- **Color.** One mode = one hue family. Close range: warm (orange target on dark blue). Mid: cyan on charcoal (the Aimlabs look; familiar = credible). Far: lime on near-black. Hit flash is always white. Avoid red/green pairings (color-blind users, and red reads as gore).
- **Sound.** Hit: short high "tick" (~2 kHz, 30 ms). Kill: low "thump" + pitch-down pop. RL "level-up": ascending 3-note arpeggio when the policy switches to a smarter checkpoint. All synthesized in Unity (no asset-store clips) so you can post "I synthesized the SFX procedurally" as its own post.

---

## 4. Game design (minimal, shippable)

### Modes (one RL archetype each)
| Mode | Distance | Player skill | Target behavior | RL observation | RL actions |
|---|---|---|---|---|---|
| Close | 3–8 m | Flicks | Strafes, jukes, occasional lunge | Player aim ray angle to self, own velocity, time since last shot | 8 directions + hold |
| Mid | 10–25 m | Tracking | Smooth weaving, speed changes | Same + crosshair angular velocity | Continuous 2D velocity |
| Far | 30–80 m | Precision | Peek/hide behind cover, timing | Same + cover occupancy | Peek / hide / relocate |

Reward (all modes): `+0.02 per step alive`, `-1.0 on hit`, `-0.001 * |action|` (so it doesn't jitter), `+0.1 when crosshair-to-self angle > threshold` (rewards *being* missed, not just surviving). Episode = 10 s or hit.

### Session loop
Countdown (3 s) → 60 s round → scoreboard (accuracy, reaction time, hits, policy checkpoint used) → restart (F6, matching Aimlabs muscle memory).

### Settings (v1, mirroring the screenshot but trimmed)
Ship these eight:
1. Sensitivity: cm/360 (and derived eDPI) with input DPI field
2. FOV (horizontal)
3. Crosshair: size, thickness, color, dot on/off
4. Target color
5. Hit sound, kill sound (3 choices each)
6. Countdown duration
7. Restart key
8. Mode select (Close / Mid / Far) and AI level (Dumb / Trained / Learning)

Defer: themes, controller, graphics quality, personalized difficulty, "pro" anything.

### Custom targets
- Accept `.glb` (glTFast runtime import works in WebGL) and `.png/.jpg` (billboard quad).
- Limits: 50k triangles, 10 MB, 2048² texture. Auto-scale to fit a 2 m bounding box.
- Collider: quickhull convex hull, decimated to ≤255 tris (Unity's convex MeshCollider cap). Compound-box fallback for very concave meshes.
- Ragdoll for custom meshes: no skeleton, so do rigidbody impulse + spin + a 6-piece Voronoi shatter on kill. Looks great, costs nothing.
- Browser file picker via a small `.jslib` plugin passing bytes to C#.

### Ragdoll (built-in mannequin)
- 9 rigidbodies (pelvis, spine, head, 2 upper arms, 2 forearms, 2 thighs; lower legs merged). Configurable joints.
- Animated by a simple procedural walk/strafe (no mocap) while alive; `isKinematic` off on death.
- Impulse: `hitDir * weaponForce` at hit point; headshot gets 2x and a slow-mo 0.3 s.
- Stretch goal post: "active ragdoll" (PD controllers keep it upright while alive). That is a *great* control-systems post for hardware people.

---

## 5. Architecture

```
practice-range/
├── CLAUDE.md                  # how Claude Code builds/tests/trains this project
├── unity/                     # Unity 6 project
│   ├── Assets/
│   │   ├── _Project/
│   │   │   ├── Core/          # GameLoop, Settings (ScriptableObject + JSON persist)
│   │   │   ├── Player/        # FPS controller, input, weapon, hitscan + tracer
│   │   │   ├── Targets/       # TargetBase, Mannequin, CustomTarget, Ragdoll, Shatter
│   │   │   ├── AI/            # DodgeAgent (ML-Agents), BanditLearner, PolicyLoader
│   │   │   ├── Modes/         # Close/Mid/Far ScriptableObjects
│   │   │   ├── UI/            # Settings, HUD, Scoreboard (UI Toolkit)
│   │   │   ├── Audio/         # Procedural SFX synth
│   │   │   └── Import/        # glTFast wrapper, collider builder, jslib bridge
│   │   ├── Models/            # trained .onnx policies (versioned by date)
│   │   └── Tests/             # EditMode + PlayMode tests
│   ├── Packages/manifest.json # ml-agents, sentis, gltfast, inputsystem
│   └── ProjectSettings/
├── ml/                        # Python: ML-Agents trainer configs, curricula, plotting
│   ├── configs/dodge_close.yaml ...
│   ├── results/               # tensorboard runs → charts for posts
│   └── scripts/plot_training.py
├── tools/
│   ├── build_webgl.sh         # unity -batchmode -executeMethod BuildScript.WebGL
│   ├── build_steam.sh
│   ├── run_tests.sh           # unity -runTests -testPlatform PlayMode -testResults ...
│   ├── deploy_itch.sh         # butler push
│   └── latency/               # Arduino photodiode sketch + analysis (hardware posts)
├── web/                       # landing page wrapper for the WebGL build (GitHub Pages)
└── content/                   # post drafts, clip scripts, charts (never shipped)
```

Package choices: `com.unity.ml-agents` 4.x, `com.unity.ai.inference` (Sentis) 2.x, `com.unity.cloud.gltfast`, `com.unity.inputsystem`, `com.unity.render-pipelines.universal`, `com.unity.test-framework`.

### RL pipeline
1. `DodgeAgent : Agent` collects observations, applies actions, emits rewards.
2. Training scene: 32 parallel arenas with a **scripted "synthetic player"** (aim with noise + reaction delay sampled from human-like distributions; aim noise drops across curriculum). You cannot train against a human; you train against a model of one.
3. `mlagents-learn ml/configs/dodge_close.yaml --run-id=close_v3` from the terminal, Unity in play mode or headless `-batchmode` build.
4. Export `.onnx` → `Assets/Models/` → `PolicyLoader` picks checkpoint by "AI level".
5. **Live layer:** `BanditLearner` keeps a small table keyed by (player's recent miss direction, distance bucket) → dodge direction, updated every episode in-browser. Visible learning in under a minute of play, no Python.

### WebGL specifics
- WebGL 2 on 6000.0. Upgrade to 6.6 later for WebGPU (compute = GPU Sentis + GPU particles). Serve over HTTPS or WebGPU silently falls back anyway.
- Sentis backend: `BackendType.CPU` (Burst) in WebGL. A 2-layer 128-unit policy inferred every 5th frame is trivial.
- Pointer lock + raw mouse via Input System; test in Chrome/Safari/Firefox because pointer-lock behavior differs.
- Target 60 fps on an M1 MacBook Air in Chrome; budget ~20 MB compressed build (Brotli), stripped engine.

### Steam specifics
- Same project, `StandaloneOSX` + `StandaloneWindows64` builds. Steamworks.NET for overlay/achievements later.
- Steam Direct fee $100, ~2–4 weeks review for a Coming Soon page.

---

## 6. Claude Code integration (make the project terminal-drivable)

1. **CLAUDE.md** at repo root telling Claude Code: Unity path (`~/.unity/bin/unity` or `/Applications/Unity/Hub/Editor/6000.0.39f1/...`), how to build, test, train, deploy, and the coding conventions (one behaviour per file, ScriptableObjects for tunables, no magic numbers in MonoBehaviours).
2. **Batch-mode scripts** in `tools/` so Claude Code can run builds and tests and read `Editor.log` / test XML to fix failures itself:
   ```
   unity -batchmode -nographics -quit -projectPath unity -executeMethod BuildScript.WebGL -logFile -
   unity -batchmode -projectPath unity -runTests -testPlatform EditMode -testResults results.xml
   ```
3. **Unity MCP** for scene-level work (creating prefabs, wiring components, inspecting hierarchy). Use `CoderGamester/mcp-unity` (MIT, Claude Code supported) or Unity's official MCP beta if you have a Unity AI subscription. Add with `claude mcp add`. Treat as convenience; everything must also be possible from scripts so the project does not depend on the Editor being open.
4. **Python side** lives in `ml/` with a `requirements.txt`; Claude Code launches `mlagents-learn`, tails TensorBoard CSVs, and generates the training charts for posts with matplotlib.
5. **Tests as the contract.** EditMode tests for: collider builder (hull ≤255 tris), settings persistence, reward function, bandit update math. PlayMode tests for: hitscan registers on ragdoll bones, episode reset. Claude Code runs them before every commit.
6. **Git from day one** (the folder isn't a repo yet). Use Unity's `.gitignore`, Git LFS for `.onnx`, `.glb`, audio.
7. **Content pipeline.** `content/` holds post drafts and a `clips.md` shot list. Claude Code can draft posts from the training CSVs and commit messages, but you record and narrate.

---

## 7. Phased plan (≈10 weeks part-time)

| Phase | Weeks | Deliverable | Post |
|---|---|---|---|
| 0. Setup | 0.5 | Repo, CLAUDE.md, URP template, batch build to WebGL running on itch | "Why I'm building an aim trainer whose targets learn to dodge" (text + 1 image, sets up the series) |
| 1. Core loop | 1.5 | FPS controller, hitscan, tracer, mid-range static targets, HUD, 8 settings, countdown/restart | Short clip: pure Aimlabs-clone feel, muted, "step 1 of N" |
| 2. Ragdoll | 1 | Mannequin ragdoll, impulse, slow-mo on headshot, kill sound | **First hook post** (ragdoll + synthesized sound) |
| 3. RL v1 | 2 | ML-Agents agent, synthetic player, curriculum, first .onnx, Sentis in WebGL | Before/after training split; chart of hit rate vs. episodes |
| 4. Live learner | 1 | Bandit layer, "AI level" selector, accuracy-reversal HUD | Score-reversal hook |
| 5. Custom targets | 1 | glTF/image upload, auto collider, shatter | Rubber-duck hook; invite people to send their logos |
| 6. Close + Far | 1 | Two more modes, two more policies | "Three policies, three behaviours, same reward" |
| 7. Hardware | 1 | Photodiode click-to-photon rig, polling-rate test, perf profile | Oscilloscope hook (hardware audience) |
| 8. Steam + polish | 1 | Steam Coming Soon, settings persistence, color-blind palette | "Shipped to Steam" + retrospective |

Posting cadence: one substantive post per week, ~12 total. Every post: native video ≤45 s, hook in first 2 s, one chart or diagram, one stated limitation, link in first comment.

Additional post ideas beyond the table: reward-hacking failures ("it learned to stand behind the camera"), the 255-triangle collider problem, WebGL pointer-lock quirks across browsers, procedural SFX synthesis, PD-controlled active ragdoll, "what Claude Code was good and bad at on this project" (hiring managers in 2026 want to see you drive AI tools well).

---

## 8. Open questions that decide success

1. **Who exactly is the audience?** Embedded/hardware teams, game studios, or general software? This changes the ratio of RL posts to hardware posts. *My recommendation:* target senior/staff engineers at product companies; they are the ones who forward posts to hiring managers. Hardware posts are the differentiator, keep 2 of 12.
2. **Are you comfortable with a weapon on screen at all?** Alternative is a "launcher" with a visible projectile (paintball/ping-pong). Hitscan rifle reads best for aim training; a paint launcher is safer on LinkedIn. *My recommendation:* hitscan with a tracer, no visible gun model, first-person hands only. Reads as "range", not "war".
3. **Human mannequin or abstract target for the default?** Mannequin makes ragdoll shine. Spheres/capsules are what Aimlabs users expect and are 100% safe. *My recommendation:* abstract capsule-with-limbs mannequin (think crash-test dummy, no face). Gets ragdoll without a human.
4. **How much of the RL has to be "real"?** The honest story is PPO offline + bandit online. If you say "it learns as you play" and mean the bandit, say so in the post. *My recommendation:* be explicit; engineers will ask, and the explanation is itself a good post.
5. **Timebox.** Is 10 weeks part-time realistic for you? If not, cut phases 6 and 7, not 3 and 4. The RL is the reason this project is different.
6. **Steam, yes or no?** $100 and review time, for a credibility badge and a second post wave. *My recommendation:* yes, but only after phase 5 so the page isn't empty.
7. **Will you use Unity 6.0 or jump to 6.6 now?** 6.6 gives production WebGPU. Upgrading mid-project hurts. *My recommendation:* upgrade now, before any code exists, if the Hub offers 6.6 LTS; otherwise stay on 6.0 and upgrade at phase 8.

---

## 9. Sources used for the technical claims

- Unity 6.6 WebGPU production status, opt-in, secure-context caveat: https://app.cinevva.com/news/2026-09-01-unity-6-6-webgpu-production
- ML-Agents 4.0 inference backends (compute / CPU Burst / pixel shader): https://docs.unity3d.com/Packages/com.unity.ml-agents@4.0/manual/Inference-Engine.html
- Sentis (Inference Engine) package: https://docs.unity3d.com/Packages/com.unity.ai.inference@2.6/manual/index.html
- Unity MCP options for Claude Code: https://gamedev.net/news/4376-unity-ai-open-beta-how-to-get-started-with-mcp/ , https://enterprisedna.co/directories/mcp/codergamester-mcp-unity , https://docs.coplay.dev/coplay-mcp/claude-code-guide.md
- LinkedIn video carousel / first seconds (LinkedIn product staff): https://www.lindseygamble.com/blog/linkedins-senior-director-of-product-shares-key-tips-for-video-during-ongoing-tests
- Hook practitioner guidance (unsourced numbers, treat as advice): https://www.opus.pro/blog/linkedin-video-hooks-formulas , https://contentin.io/blog/linkedin-video-best-practices/

---

## 10. Content safety spec (hard constraints for every post and build)

Answer to "primary goal and genre style": **hiring portfolio, stylized/abstract.** The video makes a
technical claim; the shooting is evidence for the claim.

Three rules for every post:
1. First on-screen caption names an engineering concept, never a game result.
2. Nothing realistic: no faces, skin, blood, gun models, recorded gunshot audio, vocal pain sounds.
3. End on a chart, diagram, or stated limitation.

| Target | Mode | Hit reaction | Safety ceiling | Content angle |
|---|---|---|---|---|
| Sphere | Far / precision | Shrink-pop, 6-fragment shatter, dent along normal | Fully abstract, no ragdoll | Pure RL post: peek/hide policy, reward shaping |
| Cube | Mid / tracking | Rigidbody impulse + torque spin, shatter on kill | Fully abstract | Physics post (off-center hit = visible torque), color/sound design post |
| Mannequin | Close / flicks | 9-bone ragdoll, slow-mo fall on headshot, white flash, fade after 2 s | Faceless, matte two-tone, ball joints, head stays one rigid piece | Control-systems / active-ragdoll post. Never a thumbnail without an engineering caption burned in |
| Imported | Any | Rigid impulse + Voronoi shatter only | **No skinned meshes accepted** (uploaded humans become rigid statues). Editorial rule: never show an uploaded realistic human in your own content | Pipeline post: glTF import, convex hull, 255-tri collider cap |

Posting order: sphere and cube modes first, so the audience reads the series as RL/physics before
the humanoid appears. Weapon: tracer + muzzle flash + crosshair recoil, first-person hands at most.
Audio: synthesized clicks/ticks/pops only.

---

## 10. Decisions locked (2026-10-26)

| Question | Decision |
|---|---|
| Audience | Broad: software and hardware engineering teams, goal is first role ASAP. Posts are written for a senior engineer who forwards things to hiring managers. Ratio: 7 software/RL posts, 3 physics/visual posts, 2 hardware posts. |
| Weapon | No gun model. First-person hands + hitscan tracer by default. Optional "Nerf" skin (foam dart projectile, orange/blue) selectable in settings, used in any post where tone is funny. Never a realistic pistol in posted content. |
| Targets | Default: cubes and spheres. Mannequin (faceless, two-tone, ragdoll) selectable in settings. Imported targets: rigid statues only (no skinned meshes), impulse + shatter, never ragdoll. |
| Platform | Browser only for now. Steam deferred until after the upload feature ships. |
| Scope | Full build, nothing cut. |
| Engine version | Upgrading to Unity 6000.6.7f1 (WebGPU production). Work that does not need the Editor proceeds on day one; ProjectSettings/scene bootstrap and first build wait for 6.6. |

## 11. Content safety spec (constraint, not guidance)

Three rules applied to every post:
1. The first on-screen caption names an engineering concept, never a game result. "PD joint controller", "episode 4,000", never "headshot".
2. Nothing realistic appears: no faces, skin, blood, gun models, or recorded gunshot audio. All SFX synthesized.
3. The video ends on a chart, a limitation, or a diagram.

Per target type:
- **Sphere (far, precision).** No ragdoll. Shrink-and-pop or six-fragment shatter. The pure RL post: peek/hide behaviour and reward shaping.
- **Cube (mid, tracking).** Rigidbody tumble on hit, shatter on kill. The physics post: off-center hit → visible rotation, torque = r × F.
- **Mannequin (close, flicks).** Faceless, matte, two-tone, ball joints. Only target that ragdolls. Headshot = slow-motion fall only. Never the thumbnail without an engineering caption burned in.
- **Imported.** Hard layer: skinned meshes rejected, so a human model becomes a rigid statue. Editorial layer: only ducks, logos, mugs, Suzanne in posts. "Can I upload my boss?" → "It'll be a statue that shatters, and no, I won't post that."

Series ordering: sphere and cube posts land before any mannequin appears, so the audience already reads the clips as engineering by the time ragdoll shows up.
