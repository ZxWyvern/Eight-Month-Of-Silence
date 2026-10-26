using NUnit.Framework;
using Yumiwa.Domain;
using Yumiwa.Application;

namespace Yumiwa.Application.Tests
{
    [TestFixture]
    public class HealthServiceTests
    {
        [Test]
        public void TakeDamage_ReducesHealth_And_ClampsAtZero()
        {
            var health = new HealthService(100f, 30f, null);
            health.TakeDamage(40f);
            Assert.AreEqual(60f, health.Current);

            health.TakeDamage(100f);
            Assert.AreEqual(0f, health.Current);
            Assert.IsTrue(health.IsDead);
        }

        [Test]
        public void Heal_IncreasesHealth_And_ClampsAtMax()
        {
            var health = new HealthService(100f, 30f, null);
            health.TakeDamage(50f);
            health.Heal(20f);
            Assert.AreEqual(70f, health.Current);

            health.Heal(100f);
            Assert.AreEqual(100f, health.Current);
        }

        [Test]
        public void WoundedThreshold_Triggers_WoundedState()
        {
            var health = new HealthService(100f, 30f, null);
            Assert.IsFalse(health.IsWounded);
            
            health.TakeDamage(80f); // 100 - 80 = 20
            Assert.IsTrue(health.IsWounded);
            Assert.IsFalse(health.IsDead);
        }

        [Test]
        public void TakeDamage_Invokes_OnWoundedEvent()
        {
            var health = new HealthService(100f, 30f, null);
            bool woundedFired = false;
            health.OnWounded += e => woundedFired = true;

            health.TakeDamage(80f);
            Assert.IsTrue(woundedFired);
        }
    }
}
