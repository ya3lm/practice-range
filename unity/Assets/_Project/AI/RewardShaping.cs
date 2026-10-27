using UnityEngine;

namespace PracticeRange.AI
{
    /// <summary>Pure reward function, shared by the ML-Agents agent and the bandit. Tested in EditMode.</summary>
    public static class RewardShaping
    {
        public const float AlivePerStep = 0.02f;
        public const float HitPenalty = -1.0f;
        public const float ActionCost = -0.001f;
        public const float MissedBonus = 0.1f;
        public const float MissedAngleThresholdDeg = 6f;
        public const float HidePenaltyPerStep = -0.005f; // hiding is not free, or far mode learns to never show

        /// <param name="wasHit">target took a hit this step</param>
        /// <param name="actionMagnitude">|a| of the continuous action, 0..1.41</param>
        /// <param name="aimAngleDeg">angle between crosshair ray and the direction to the target</param>
        /// <param name="shotFiredThisStep">the player fired this step</param>
        /// <param name="hidden">target is behind cover</param>
        public static float Step(bool wasHit, float actionMagnitude, float aimAngleDeg, bool shotFiredThisStep, bool hidden)
        {
            if (wasHit) return HitPenalty;
            float r = AlivePerStep + ActionCost * actionMagnitude;
            if (shotFiredThisStep && aimAngleDeg > MissedAngleThresholdDeg) r += MissedBonus;
            if (hidden) r += HidePenaltyPerStep;
            return r;
        }

        /// <summary>Direction bucket 0..7 (45° sectors) of a 2D vector; 8 = zero vector.</summary>
        public static int DirectionBucket(Vector2 v)
        {
            if (v.sqrMagnitude < 1e-6f) return 8;
            float a = Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg; // -180..180
            if (a < 0) a += 360f;
            return Mathf.Clamp(Mathf.FloorToInt((a + 22.5f) / 45f) % 8, 0, 7);
        }

        public static Vector2 BucketToDirection(int bucket)
        {
            if (bucket >= 8) return Vector2.zero;
            float a = bucket * 45f * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(a), Mathf.Sin(a));
        }

        public static int DistanceBucket(float d) => d < 9f ? 0 : d < 28f ? 1 : 2;
    }
}
