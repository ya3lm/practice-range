using UnityEngine;

namespace PracticeRange.Core
{
    /// <summary>Per-range-mode tunables. Created by ProjectBootstrap from ModeDefaults.</summary>
    [CreateAssetMenu(menuName = "Practice Range/Mode Config")]
    public class ModeConfig : ScriptableObject
    {
        public RangeMode mode;
        [Header("Geometry (meters)")]
        public float minDistance = 10f;
        public float maxDistance = 25f;
        public float arenaHalfWidth = 12f;
        public float arenaHalfDepth = 40f;
        public bool hasCover = false;
        public int coverCount = 0;
        [Tooltip("Targets stay within this angle of the player's forward axis, so a stationary player can always see them. Learned policies otherwise run to the arena edge.")]
        public float maxLateralAngleDeg = 35f;
        [Header("Target")]
        public TargetStyle defaultStyle = TargetStyle.Cube;
        public float targetScale = 1f;
        public float maxSpeed = 6f;
        public float acceleration = 30f;
        public int health = 1;
        [Header("Palette")]
        public Color background = new Color(0.08f, 0.09f, 0.12f);
        public Color floor = new Color(0.12f, 0.14f, 0.18f);
        public Color accent = new Color(0f, 1f, 1f);
        [Header("AI")]
        public string behaviorName = "DodgeMid";
        public float decisionPeriod = 5;
    }

    public static class ModeDefaults
    {
        public static void Apply(ModeConfig c, RangeMode m)
        {
            c.mode = m;
            switch (m)
            {
                case RangeMode.Close:
                    c.minDistance = 3f; c.maxDistance = 8f; c.arenaHalfWidth = 6f; c.arenaHalfDepth = 10f;
                    c.hasCover = false; c.coverCount = 0; c.maxLateralAngleDeg = 40f;
                    c.defaultStyle = TargetStyle.Cube; c.targetScale = 0.8f; c.maxSpeed = 7f; c.acceleration = 60f;
                    c.background = new Color(0.06f, 0.08f, 0.16f); c.floor = new Color(0.08f, 0.11f, 0.20f);
                    c.accent = new Color(1f, 0.56f, 0.12f); c.behaviorName = "DodgeClose"; c.decisionPeriod = 3;
                    break;
                case RangeMode.Mid:
                    c.minDistance = 10f; c.maxDistance = 25f; c.arenaHalfWidth = 12f; c.arenaHalfDepth = 30f;
                    c.hasCover = false; c.coverCount = 0; c.maxLateralAngleDeg = 32f;
                    c.defaultStyle = TargetStyle.Cube; c.targetScale = 1f; c.maxSpeed = 6f; c.acceleration = 25f;
                    c.background = new Color(0.055f, 0.065f, 0.085f); c.floor = new Color(0.075f, 0.09f, 0.115f);
                    c.accent = new Color(0f, 0.94f, 1f); c.behaviorName = "DodgeMid"; c.decisionPeriod = 5;
                    break;
                case RangeMode.Far:
                    c.minDistance = 30f; c.maxDistance = 80f; c.arenaHalfWidth = 20f; c.arenaHalfDepth = 90f;
                    c.hasCover = true; c.coverCount = 6; c.maxLateralAngleDeg = 22f;
                    c.defaultStyle = TargetStyle.Sphere; c.targetScale = 1.4f; c.maxSpeed = 5f; c.acceleration = 20f;
                    c.background = new Color(0.025f, 0.03f, 0.035f); c.floor = new Color(0.05f, 0.06f, 0.065f);
                    c.accent = new Color(0.65f, 1f, 0.25f); c.behaviorName = "DodgeFar"; c.decisionPeriod = 5;
                    break;
            }
        }
    }
}
