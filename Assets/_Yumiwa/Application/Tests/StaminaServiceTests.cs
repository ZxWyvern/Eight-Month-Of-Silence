using NUnit.Framework;
using Yumiwa.Domain;

namespace Yumiwa.Application.Tests
{
    /// <summary>EditMode tests for stamina service event behavior.</summary>
    [TestFixture]
    public sealed class StaminaServiceTests
    {
        private FakeTimeSource clock;
        private EventBus bus;
        private StaminaService service;

        [SetUp]
        public void SetUp()
        {
            clock = new FakeTimeSource();
            bus = new EventBus();
            service = new StaminaService(new StaminaPool(100f, 10f, 1.5f), clock, bus);
        }

        [Test]
        public void SuccessfulSpendPublishesChanged()
        {
            StaminaChangedEvent evt = default;
            bus.Subscribe<StaminaChangedEvent>(e => evt = e);
            Assert.IsTrue(service.TrySpend(15f));
            Assert.AreEqual(85f, evt.Current);
        }

        [Test]
        public void FailedSpendPublishesDepleted()
        {
            StaminaDepletedEvent evt = default;
            bus.Subscribe<StaminaDepletedEvent>(e => evt = e);
            Assert.IsFalse(service.TrySpend(500f));
            Assert.AreEqual(500f, evt.AttemptedCost);
        }

        [Test]
        public void RegenTickPublishesChangedOnlyOnChange()
        {
            int calls = 0;
            bus.Subscribe<StaminaChangedEvent>(e => calls++);
            clock.Now = 5f;
            service.Tick(1f);
            Assert.AreEqual(0, calls);
            service.TrySpend(20f);
            Assert.AreEqual(1, calls);
            clock.Now = 7f;
            service.Tick(1f);
            Assert.AreEqual(2, calls);
        }
    }
}
