using UnityEngine;

namespace PracticeRange.Core
{
    /// <summary>Pure sensitivity math. cm/360 is the physical distance the mouse travels for one full turn.</summary>
    public static class Sensitivity
    {
        public const float InchesPerCm = 1f / 2.54f;

        /// <summary>Degrees of yaw per mouse count, given cm per 360° and mouse DPI.</summary>
        public static float DegreesPerCount(float cmPer360, float dpi)
        {
            if (cmPer360 <= 0f || dpi <= 0f) return 0f;
            float countsPer360 = cmPer360 * InchesPerCm * dpi;
            return 360f / countsPer360;
        }

        /// <summary>Inverse: cm per 360° given degrees per count and DPI.</summary>
        public static float CmPer360(float degreesPerCount, float dpi)
        {
            if (degreesPerCount <= 0f || dpi <= 0f) return 0f;
            float countsPer360 = 360f / degreesPerCount;
            return countsPer360 / dpi / InchesPerCm;
        }

        /// <summary>Effective DPI, the common cross-game comparison number (DPI × in-game sens).</summary>
        public static float EDpi(float cmPer360, float dpi) => DegreesPerCount(cmPer360, dpi) * dpi;

        /// <summary>Vertical FOV from horizontal FOV and aspect ratio (Unity cameras take vertical FOV).</summary>
        public static float VerticalFov(float horizontalFovDeg, float aspect)
        {
            float h = horizontalFovDeg * Mathf.Deg2Rad;
            return 2f * Mathf.Atan(Mathf.Tan(h * 0.5f) / aspect) * Mathf.Rad2Deg;
        }
    }
}
