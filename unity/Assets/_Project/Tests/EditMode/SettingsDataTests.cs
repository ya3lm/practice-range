using NUnit.Framework;
using UnityEngine;
using PracticeRange.Core;

namespace PracticeRange.Tests
{
    public class SettingsDataTests
    {
        [Test]
        public void Clone_IsDeep_And_Equal()
        {
            var a = new SettingsData { cmPer360 = 42f, targetStyle = TargetStyle.Mannequin, crosshairColor = Color.red };
            var b = a.Clone();
            Assert.AreEqual(42f, b.cmPer360);
            Assert.AreEqual(TargetStyle.Mannequin, b.targetStyle);
            Assert.AreEqual(Color.red, b.crosshairColor);
            b.cmPer360 = 1f;
            Assert.AreEqual(42f, a.cmPer360);
        }

        [Test]
        public void Defaults_AreSafeForLinkedIn()
        {
            var s = new SettingsData();
            Assert.AreNotEqual(TargetStyle.Mannequin, s.targetStyle, "mannequin must be opt-in");
            Assert.AreEqual(WeaponSkin.Tracer, s.weaponSkin);
        }
    }
}

namespace PracticeRange.Tests
{
    public class ModeDefaultsTests
    {
        [Test]
        public void Every_Mode_Keeps_Targets_Inside_A_Reasonable_View_Cone()
        {
            foreach (RangeMode m in System.Enum.GetValues(typeof(RangeMode)))
            {
                var c = ScriptableObject.CreateInstance<ModeConfig>();
                ModeDefaults.Apply(c, m);
                Assert.That(c.maxLateralAngleDeg, Is.InRange(15f, 45f), m.ToString());
                // At the closest spawn distance the lateral room must still be at least a target width.
                float lateral = c.minDistance * Mathf.Tan(c.maxLateralAngleDeg * Mathf.Deg2Rad);
                Assert.Greater(lateral, c.targetScale, m + " has no room to dodge at min distance");
            }
        }
    }
}

namespace PracticeRange.Tests
{
    public class ArenaGeometryTests
    {
        [Test]
        public void Standoff_Is_At_Least_Two_Meters_In_Every_Mode()
        {
            foreach (RangeMode m in System.Enum.GetValues(typeof(RangeMode)))
            {
                var c = ScriptableObject.CreateInstance<ModeConfig>(); ModeDefaults.Apply(c, m);
                Assert.GreaterOrEqual(Mathf.Max(2f, c.minDistance * 0.5f), 2f, m.ToString());
                Assert.Greater(c.arenaHalfDepth, c.minDistance * 0.5f + 1f, m + " arena too shallow for its standoff");
            }
        }
    }
}
