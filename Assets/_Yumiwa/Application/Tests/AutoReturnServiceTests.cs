using NUnit.Framework;
using Yumiwa.Domain;

namespace Yumiwa.Application.Tests
{
    [TestFixture]
    public sealed class AutoReturnServiceTests
    {
        private static ItemDefinition ObjectiveItem(string id) =>
            new ItemDefinition(id, id, "Desc", 1, ItemKind.PhysicalTool, true);

        [Test]
        public void EnqueueRecovery_WithRegistrant_RestoresAndClearsPending()
        {
            var bus = new EventBus();
            var recovery = new AutoReturnService(bus);
            bool restored = false;
            int recoveredEvents = 0;
            bus.Subscribe<ItemRecoveredEvent>(_ => recoveredEvents++);

            recovery.Register("obj_key", () => restored = true);
            recovery.EnqueueRecovery(ObjectiveItem("obj_key"));

            Assert.IsTrue(restored);
            Assert.AreEqual(0, recovery.PendingRecoveries.Count);
            Assert.AreEqual(1, recoveredEvents);
        }

        [Test]
        public void EnqueueRecovery_WithoutRegistrant_KeepsPending()
        {
            var bus = new EventBus();
            var recovery = new AutoReturnService(bus);
            int recoveredEvents = 0;
            bus.Subscribe<ItemRecoveredEvent>(_ => recoveredEvents++);

            recovery.EnqueueRecovery(ObjectiveItem("obj_key"));

            Assert.AreEqual(1, recovery.PendingRecoveries.Count);
            Assert.AreEqual("obj_key", recovery.PendingRecoveries[0].ItemId);
            Assert.AreEqual(0, recoveredEvents);
        }

        [Test]
        public void EnqueueRecovery_ThrowingRegistrant_KeepsPending()
        {
            var bus = new EventBus();
            var recovery = new AutoReturnService(bus);
            int recoveredEvents = 0;
            bus.Subscribe<ItemRecoveredEvent>(_ => recoveredEvents++);

            recovery.Register("obj_key", () => throw new System.InvalidOperationException("dead restore"));
            recovery.EnqueueRecovery(ObjectiveItem("obj_key"));

            Assert.AreEqual(1, recovery.PendingRecoveries.Count, "Failed restore must defer, not silently drop the item.");
            Assert.AreEqual(0, recoveredEvents, "Recovered must only publish on actual restore.");
        }

        [Test]
        public void ResolveAll_RetriesPendingAfterRegister()
        {
            var bus = new EventBus();
            var recovery = new AutoReturnService(bus);
            bool restored = false;

            recovery.EnqueueRecovery(ObjectiveItem("obj_key"));
            Assert.AreEqual(1, recovery.PendingRecoveries.Count);

            recovery.Register("obj_key", () => restored = true);
            recovery.ResolveAll();

            Assert.IsTrue(restored);
            Assert.AreEqual(0, recovery.PendingRecoveries.Count);
        }
    }
}
