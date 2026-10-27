using UnityEngine;

namespace PracticeRange.Audio
{
    /// <summary>All SFX synthesized at startup. No recorded audio in the project, by content policy. Three variants of each kind.</summary>
    public static class ProceduralSfx
    {
        const int Rate = 44100;

        public static AudioClip Tick(int variant)
        {
            // Hit: short high tick. Variants: 2.0k, 2.6k, 1.6k Hz.
            float f = variant == 1 ? 2600f : variant == 2 ? 1600f : 2000f;
            return Synth("hit" + variant, 0.035f, t => Mathf.Sin(2f * Mathf.PI * f * t) * Env(t, 0.001f, 0.03f));
        }

        public static AudioClip Thump(int variant)
        {
            // Kill: low thump with pitch drop plus a tiny noise burst. Variants change the drop.
            float f0 = variant == 1 ? 220f : variant == 2 ? 140f : 180f;
            float f1 = f0 * 0.35f;
            var rng = new System.Random(7);
            return Synth("kill" + variant, 0.22f, t =>
            {
                float k = t / 0.22f;
                float f = Mathf.Lerp(f0, f1, k * k);
                float s = Mathf.Sin(2f * Mathf.PI * f * t) * Env(t, 0.002f, 0.2f);
                float n = ((float)rng.NextDouble() * 2f - 1f) * Env(t, 0.0005f, 0.02f) * 0.5f;
                return s * 0.9f + n;
            });
        }

        public static AudioClip Shoot(int variant)
        {
            // Shoot: a filtered noise "tap", deliberately un-gunlike. Nerf variant is a softer "poof".
            var rng = new System.Random(3 + variant);
            float len = variant == 1 ? 0.09f : 0.05f;
            float lp = 0f;
            float alpha = variant == 1 ? 0.08f : 0.25f;
            return Synth("shoot" + variant, len, t =>
            {
                float n = (float)rng.NextDouble() * 2f - 1f;
                lp += alpha * (n - lp);
                return lp * Env(t, 0.001f, len * 0.8f) * 1.5f;
            });
        }

        public static AudioClip LevelUp()
        {
            // Policy switched to a smarter checkpoint: ascending three-note arpeggio.
            float[] notes = { 523.25f, 659.25f, 783.99f };
            return Synth("levelup", 0.36f, t =>
            {
                int i = Mathf.Min(2, (int)(t / 0.12f));
                float lt = t - i * 0.12f;
                return Mathf.Sin(2f * Mathf.PI * notes[i] * t) * Env(lt, 0.005f, 0.11f) * 0.6f;
            });
        }

        public static AudioClip Countdown(bool final)
        {
            float f = final ? 880f : 440f;
            return Synth(final ? "cd_go" : "cd_tick", final ? 0.18f : 0.07f, t => Mathf.Sin(2f * Mathf.PI * f * t) * Env(t, 0.002f, final ? 0.16f : 0.06f) * 0.5f);
        }

        static float Env(float t, float attack, float decay)
        {
            if (t < attack) return t / attack;
            float d = (t - attack) / decay;
            return Mathf.Exp(-4f * d);
        }

        static AudioClip Synth(string name, float seconds, System.Func<float, float> f)
        {
            int n = Mathf.CeilToInt(seconds * Rate);
            var data = new float[n];
            for (int i = 0; i < n; i++) data[i] = Mathf.Clamp(f(i / (float)Rate), -1f, 1f);
            var clip = AudioClip.Create(name, n, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
