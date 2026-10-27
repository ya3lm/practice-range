# Content safety spec (constraint)

Primary goal is hiring: a portfolio that demonstrates engineering depth to software and hardware teams. The video exists
to make a technical claim; the shooting is evidence for the claim.

## Three rules for every post
1. The first on-screen caption names an engineering concept, never a game result. "PD joint controller", "episode 4,000". Never "headshot".
2. Nothing realistic appears: no faces, skin, blood, gun models, or recorded gunshot audio.
3. The video ends on a chart, a limitation, or a diagram.

## Per target
- **Sphere** (far): shrink-pop / fragments. The RL post. No ragdoll (no joints).
- **Cube** (mid): rigidbody tumble, shatter on kill. The physics post. Torque = r × F is legible in 2 s.
- **Mannequin** (close): faceless, matte, two-tone, ball joints. The only ragdoll. Headshot = slow-mo fall only. Never a thumbnail without an engineering caption burned in.
- **Imported**: skinned meshes rejected (rigid statue, impulse + shatter). Only ducks, logos, mugs, Suzanne appear in posts. Never an uploaded realistic human, even if the system allows it.

## Weapon
No gun model, ever, in posted content. Tracer skin by default; Nerf dart skin when the tone is funny.

## Series order
Sphere and cube posts before any mannequin appears. By the time ragdoll shows up the audience reads the clips as engineering.

## Code enforcement
- `SettingsData` defaults: target = Cube, weapon = Tracer (tested in `SettingsDataTests`).
- `CustomTargetImporter` rejects `SkinnedMeshRenderer`.
- `ProceduralSfx` is the only audio source; no clips in the repo.
