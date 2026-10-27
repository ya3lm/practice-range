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
