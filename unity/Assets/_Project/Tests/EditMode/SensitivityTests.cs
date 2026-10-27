using NUnit.Framework;
using PracticeRange.Core;

namespace PracticeRange.Tests
{
    public class SensitivityTests
    {
        [Test]
        public void DegreesPerCount_Matches_KnownValorantConversion()
        {
            // 800 DPI, 0.4 Valorant sens = 0.028 °/count → 12857 counts/360 → 16.07 in → 40.82 cm/360.
            float cm = Sensitivity.CmPer360(0.4f * 0.07f, 800f);
            Assert.AreEqual(40.82f, cm, 0.05f);
            float deg = Sensitivity.DegreesPerCount(cm, 800f);
            Assert.AreEqual(0.028f, deg, 0.0001f);
        }

        [Test]
        public void RoundTrip_IsStable()
        {
            for (float cm = 5f; cm <= 120f; cm += 7.3f)
            {
                float deg = Sensitivity.DegreesPerCount(cm, 1600f);
                Assert.AreEqual(cm, Sensitivity.CmPer360(deg, 1600f), 1e-3f);
            }
        }

        [Test]
        public void InvalidInputs_ReturnZero()
        {
            Assert.AreEqual(0f, Sensitivity.DegreesPerCount(0f, 800f));
            Assert.AreEqual(0f, Sensitivity.DegreesPerCount(30f, 0f));
        }

        [Test]
        public void VerticalFov_16by9_103h_IsAbout71()
        {
            Assert.AreEqual(70.5f, Sensitivity.VerticalFov(103f, 16f / 9f), 0.6f);
        }
    }
}
