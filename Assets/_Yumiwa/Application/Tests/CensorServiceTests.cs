using NUnit.Framework;
using Yumiwa.Domain;

namespace Yumiwa.Application.Tests
{
    /// <summary>EditMode tests for censor service event behavior.</summary>
    [TestFixture]
    public sealed class CensorServiceTests
    {
        private EventBus bus;
        private CensorService service;

        [SetUp]
        public void SetUp()
        {
            bus = new EventBus();
            service = new CensorService(bus);
        }

        [Test]
        public void DefaultsToEnabledFullIntensity()
        {
            Assert.IsTrue(service.Enabled);
            Assert.AreEqual(1f, service.Intensity);
        }

        [Test]
        public void DisablePublishesEventOnce()
        {
            int calls = 0;
            CensorToggledEvent evt = default;
            bus.Subscribe<CensorToggledEvent>(e => { calls++; evt = e; });
            service.SetEnabled(false);
            Assert.AreEqual(1, calls);
            Assert.IsFalse(evt.Enabled);
            service.SetEnabled(false);
            Assert.AreEqual(1, calls);
        }

        [Test]
        public void IntensityIsClampedToUnitRange()
        {
            CensorToggledEvent evt = default;
            bus.Subscribe<CensorToggledEvent>(e => evt = e);
            service.SetIntensity(2f);
            Assert.AreEqual(1f, service.Intensity);
            Assert.AreEqual(1f, evt.Intensity);
            service.SetIntensity(-1f);
            Assert.AreEqual(0f, service.Intensity);
        }
    }
}
