using NUnit.Framework;
using UnityEngine;
using PracticeRange.AI;

namespace PracticeRange.Tests
{
    public class RewardShapingTests
    {
        [Test] public void Hit_IsTerminalPenalty() => Assert.AreEqual(-1f, RewardShaping.Step(true, 1f, 0f, true, false));

        [Test]
        public void Surviving_IsPositive_And_MovingCostsSomething()
        {
            float still = RewardShaping.Step(false, 0f, 0f, false, false);
            float moving = RewardShaping.Step(false, 1f, 0f, false, false);
            Assert.Greater(still, 0f);
            Assert.Less(moving, still);
        }

        [Test]
        public void BeingMissed_IsRewarded_OnlyWhenShotFired()
        {
            float missed = RewardShaping.Step(false, 0f, 20f, true, false);
            float noShot = RewardShaping.Step(false, 0f, 20f, false, false);
            Assert.AreEqual(RewardShaping.MissedBonus, missed - noShot, 1e-6f);
        }

        [Test]
        public void NearMiss_BelowThreshold_GetsNoBonus()
        {
            Assert.AreEqual(RewardShaping.AlivePerStep, RewardShaping.Step(false, 0f, 2f, true, false), 1e-6f);
        }

        [Test]
        public void DirectionBuckets_Cover_AllSectors()
        {
            Assert.AreEqual(0, RewardShaping.DirectionBucket(new Vector2(1, 0)));
            Assert.AreEqual(2, RewardShaping.DirectionBucket(new Vector2(0, 1)));
            Assert.AreEqual(4, RewardShaping.DirectionBucket(new Vector2(-1, 0)));
            Assert.AreEqual(6, RewardShaping.DirectionBucket(new Vector2(0, -1)));
            Assert.AreEqual(8, RewardShaping.DirectionBucket(Vector2.zero));
            for (int b = 0; b < 8; b++) Assert.AreEqual(b, RewardShaping.DirectionBucket(RewardShaping.BucketToDirection(b)));
        }
    }
}
