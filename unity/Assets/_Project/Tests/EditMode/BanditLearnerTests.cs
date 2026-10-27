using NUnit.Framework;
using PracticeRange.AI;

namespace PracticeRange.Tests
{
    public class BanditLearnerTests
    {
        [Test]
        public void Update_ComputesIncrementalMean()
        {
            var b = new BanditLearner(1);
            b.Update(0, 3, 1f); b.Update(0, 3, 0f); b.Update(0, 3, -1f);
            Assert.AreEqual(0f, b.Value(0, 3), 1e-6f);
            Assert.AreEqual(3, b.counts[3]);
        }

        [Test]
        public void Greedy_PicksBestArm_WhenNotExploring()
        {
            var b = new BanditLearner(1) { epsilon = 0f };
            b.Update(5, 2, 1f); b.Update(5, 7, 0.5f);
            Assert.AreEqual(2, b.Choose(5, out bool explored));
            Assert.IsFalse(explored);
        }

        [Test]
        public void Converges_ToRewardingArm_UnderNoise()
        {
            var b = new BanditLearner(7) { epsilon = 0.1f };
            var rng = new System.Random(3);
            for (int i = 0; i < 2000; i++)
            {
                int a = b.Choose(4, out _);
                float r = (a == 6 ? 0.8f : 0.1f) + (float)(rng.NextDouble() - 0.5) * 0.4f;
                b.Update(4, a, r);
            }
            b.epsilon = 0f;
            Assert.AreEqual(6, b.Choose(4, out _));
        }

        [Test]
        public void Json_RoundTrips()
        {
            var b = new BanditLearner(1);
            b.Update(2, 2, 0.75f);
            var c = BanditLearner.FromJson(b.ToJson());
            Assert.AreEqual(0.75f, c.Value(2, 2), 1e-6f);
            Assert.AreEqual(1, c.totalUpdates);
        }

        [Test]
        public void Context_IsInRange()
        {
            Assert.AreEqual(BanditLearner.Contexts - 1, BanditLearner.Context(8, 2));
            Assert.AreEqual(0, BanditLearner.Context(0, 0));
        }
    }
}
