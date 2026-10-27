using System;
using UnityEngine;

namespace PracticeRange.AI
{
    /// <summary>The "learns while you play" layer. A contextual bandit: context = (where the last miss went, distance band),
    /// arms = 9 dodge directions (8 compass + stay). Epsilon-greedy with incremental mean. Pure C#, serializable, runs in-browser.</summary>
    [Serializable]
    public class BanditLearner
    {
        public const int Contexts = 9 * 3;   // missBucket (0..8) × distanceBucket (0..2)
        public const int Arms = 9;
        public float epsilon = 0.15f;
        public float[] values = new float[Contexts * Arms];
        public int[] counts = new int[Contexts * Arms];
        public int totalUpdates;

        [NonSerialized] System.Random _rng;

        public BanditLearner(int seed = 0) { _rng = new System.Random(seed); }

        public static int Context(int missBucket, int distanceBucket) => Mathf.Clamp(missBucket, 0, 8) * 3 + Mathf.Clamp(distanceBucket, 0, 2);

        public int Choose(int context, out bool explored)
        {
            _rng ??= new System.Random(0);
            if (_rng.NextDouble() < epsilon) { explored = true; return _rng.Next(Arms); }
            explored = false;
            int best = 0; float bv = float.MinValue;
            for (int a = 0; a < Arms; a++)
            {
                float v = values[context * Arms + a];
                if (v > bv) { bv = v; best = a; }
            }
            return best;
        }

        public void Update(int context, int arm, float reward)
        {
            int i = context * Arms + arm;
            counts[i]++;
            float n = counts[i];
            values[i] += (reward - values[i]) / n;   // incremental mean
            totalUpdates++;
        }

        public float Value(int context, int arm) => values[context * Arms + arm];

        /// <summary>Mean of max-arm values, a single number the HUD can show rising as it learns.</summary>
        public float Confidence()
        {
            float s = 0f;
            for (int c = 0; c < Contexts; c++)
            {
                float m = float.MinValue;
                for (int a = 0; a < Arms; a++) m = Mathf.Max(m, values[c * Arms + a]);
                s += m;
            }
            return s / Contexts;
        }

        public string ToJson() => JsonUtility.ToJson(this);
        public static BanditLearner FromJson(string json, int seed = 0)
        {
            var b = JsonUtility.FromJson<BanditLearner>(json) ?? new BanditLearner(seed);
            b._rng = new System.Random(seed);
            return b;
        }
    }
}
