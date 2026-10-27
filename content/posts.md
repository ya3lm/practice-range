# Post series (12 posts, one per week)

Format for every post: native video ≤45 s, 1:1 or 4:5 crop, hook in first 2 s muted, engineering caption burned in, one chart or diagram,
one stated limitation, link in first comment. ≤100 characters before the fold. Written for a senior engineer who forwards things.

| # | Phase | Hook (first 2 s, muted) | Fold text (≤100 chars) | Engineering payload | Limitation to state |
|---|---|---|---|---|---|
| 1 | 0 | Static image: empty range, a cube, a crosshair, caption "the target is about to get smarter than me" | I'm building an aim trainer where the targets learn to dodge. Here's the plan. | Architecture diagram: Unity ↔ ML-Agents ↔ Sentis in browser | Training can't happen in a browser; inference can |
| 2 | 1 | Timelapse of the settings menu filling in, ends on cm/360 field | cm/360, not "sensitivity 0.4". Why I made the one setting that transfers between games. | Sensitivity math + unit test | No controller support |
| 3 | 2 | Cube hit off-center, spins away in slow-mo, caption "τ = r × F" | A cube rotates when you hit its edge. That one line of physics is the whole clip. | AddForceAtPosition, impulse vs force, 120 Hz fixed step | Shatter fragments are cubes, not a real fracture |
| 4 | 2 | Three hit sounds play as waveforms draw themselves | Every sound in this project is a 30-line function. No audio files. | Synth envelope code + spectrogram | Can't do reverb cheaply |
| 5 | 3 | Split screen: episode 0 target shredded / episode 400k target weaving | Same code. 40 minutes of training. The target on the right has never seen a human. | Reward function + curriculum chart | Trained vs a synthetic player, not people |
| 6 | 3 | Target steps sideways the instant the muzzle flash appears, caption "it learned to read my fire cadence" | The policy's input includes "time since last shot". It figured out the rest. | Observation vector diagram | Policy exploits the 80 ms fire interval; a human doesn't fire that regularly |
| 7 | 3 | Reward-hacking fail: target standing still in the corner, caption "reward: +1.2 (it found a bug)" | My dodging AI found the one spot I forgot to put a wall. Three failure modes. | Three fails with the fix for each | Still finds new ones |
| 8 | 4 | HUD accuracy 94% → 61% over 20 s, caption "contextual bandit, live" | The target adapts to *you* in under a minute. It's a 243-entry table. | Bandit update math, confidence chart | Not RL; a bandit. Say so |
| 9 | 5 | Rubber duck .glb dragged in, gets a wireframe hull, starts dodging | You can upload anything. It gets a convex hull, a rigidbody, and a policy. | Quickhull + 255-tri cap diagram | No skinned meshes, by design |
| 10 | 6 | Sphere peeks from cover, ducks as the tracer passes, caption "peek/hide = discrete action" | Three modes, three policies, one reward. Why far-range needed a discrete action. | Action space comparison table | Far policy over-hides without a hide penalty |
| 11 | 7 | Oscilloscope trace: click edge → photodiode edge, caption "11.3 ms" | I measured click-to-photon on my own game with a $4 photodiode. | Rig schematic, histogram over 200 clicks | One monitor, one GPU |
| 12 | 8 | Mannequin ragdoll in slow-mo, caption "PD joint controller, 9 bodies" | The humanoid is the last thing I added. Here's why it's last, and what the joints are doing. | CharacterJoint limits, active-ragdoll PD gains | It's a mannequin, on purpose |

Bonus: "What Claude Code was good and bad at on this project" (hiring managers in 2026 want to see you drive AI tools well).
Bonus: "WebGL pointer lock differs in Chrome, Safari, Firefox; here is the matrix."
