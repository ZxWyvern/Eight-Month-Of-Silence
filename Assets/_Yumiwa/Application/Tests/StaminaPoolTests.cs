using NUnit.Framework;
using Yumiwa.Domain;

namespace Yumiwa.Application.Tests
{
    /// <summary>EditMode tests for the pure stamina pool entity.</summary>
    [TestFixture]
    public sealed class StaminaPoolTests
    {
        [Test]
        public void StartsFull()
        {
            var pool = new StaminaPool(100f, 10f, 1.5f);
            Assert.AreEqual(100f, pool.Current);
        }

        [Test]
        public void SpendBelowBalanceSucceeds()
        {
            var pool = new StaminaPool(100f, 10f, 1.5f);
            Assert.IsTrue(pool.TrySpend(15f, 0f));
            Assert.AreEqual(85f, pool.Current);
        }

        [Test]
        public void SpendAboveBalanceFailsWithoutChange()
        {
            var pool = new StaminaPool(100f, 10f, 1.5f);
            Assert.IsFalse(pool.TrySpend(101f, 0f));
            Assert.AreEqual(100f, pool.Current);
        }

        [Test]
        public void RegenBlockedDuringDelayWindow()
        {
            var pool = new StaminaPool(100f, 10f, 1.5f);
            pool.TrySpend(20f, 0f);
            pool.Tick(1f, 1f);
            Assert.AreEqual(80f, pool.Current);
        }

        [Test]
        public void RegenAppliesAfterDelayAtConfiguredRate()
        {
            var pool = new StaminaPool(100f, 10f, 1.5f);
            pool.TrySpend(20f, 0f);
            pool.Tick(1f, 2f);
            Assert.AreEqual(90f, pool.Current);
        }

        [Test]
        public void RegenNeverExceedsMax()
        {
            var pool = new StaminaPool(100f, 10f, 1.5f);
            pool.TrySpend(5f, 0f);
            pool.Tick(10f, 12f);
            Assert.AreEqual(100f, pool.Current);
        }
    }
}
