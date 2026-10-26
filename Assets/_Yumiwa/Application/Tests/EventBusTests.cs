using NUnit.Framework;
using Yumiwa.Domain;

namespace Yumiwa.Application.Tests
{
    /// <summary>EditMode tests for struct event broadcast semantics.</summary>
    [TestFixture]
    public sealed class EventBusTests
    {
        [Test]
        public void PublishReachesSubscriber()
        {
            var bus = new EventBus();
            StaminaChangedEvent received = default;
            bus.Subscribe<StaminaChangedEvent>(e => received = e);
            bus.Publish(new StaminaChangedEvent(80f, 100f));
            Assert.AreEqual(80f, received.Current);
            Assert.AreEqual(100f, received.Max);
        }

        [Test]
        public void UnsubscribeStopsDelivery()
        {
            var bus = new EventBus();
            int calls = 0;
            void Handler(StaminaChangedEvent e) => calls++;
            bus.Subscribe<StaminaChangedEvent>(Handler);
            bus.Unsubscribe<StaminaChangedEvent>(Handler);
            bus.Publish(new StaminaChangedEvent(80f, 100f));
            Assert.AreEqual(0, calls);
        }

        [Test]
        public void EventTypesAreIsolated()
        {
            var bus = new EventBus();
            int calls = 0;
            bus.Subscribe<StaminaDepletedEvent>(e => calls++);
            bus.Publish(new StaminaChangedEvent(80f, 100f));
            Assert.AreEqual(0, calls);
        }
    }
}
