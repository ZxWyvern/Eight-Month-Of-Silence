using NUnit.Framework;
using Yumiwa.Domain;

namespace Yumiwa.Application.Tests
{
    /// <summary>EditMode tests for the example bounded inventory.</summary>
    [TestFixture]
    public sealed class SimpleInventoryServiceTests
    {
        [Test]
        public void AddStacksSameItem()
        {
            var inventory = new SimpleInventoryService(16, 99);
            Assert.IsTrue(inventory.TryAdd(new ItemDefinition("herb", "Herb", "Heals.", 2), out _));
            Assert.IsTrue(inventory.TryAdd(new ItemDefinition("herb", "Herb", "Heals.", 3), out _));
            Assert.AreEqual(5, inventory.GetCount("herb"));
        }

        [Test]
        public void FullSlotsRejectNewItemTypeButAcceptExistingStack()
        {
            var inventory = new SimpleInventoryService(1, 99);
            Assert.IsTrue(inventory.TryAdd(new ItemDefinition("a", "A", "First.", 1), out _));
            Assert.IsFalse(inventory.TryAdd(new ItemDefinition("b", "B", "Second.", 1), out string reason));
            Assert.AreEqual("Inventory full.", reason);
            Assert.IsTrue(inventory.TryAdd(new ItemDefinition("a", "A", "First.", 1), out _));
            Assert.AreEqual(2, inventory.GetCount("a"));
        }

        [Test]
        public void StackCapRejectsOverflow()
        {
            var inventory = new SimpleInventoryService(16, 2);
            Assert.IsTrue(inventory.TryAdd(new ItemDefinition("key", "Key", "Opens.", 2), out _));
            Assert.IsFalse(inventory.TryAdd(new ItemDefinition("key", "Key", "Opens.", 1), out _));
            Assert.AreEqual(2, inventory.GetCount("key"));
        }

        [Test]
        public void CanAcceptDoesNotMutate()
        {
            var inventory = new SimpleInventoryService(16, 99);
            var item = new ItemDefinition("gem", "Gem", "Shiny.", 1);
            Assert.IsTrue(inventory.CanAccept(item, out _));
            Assert.AreEqual(0, inventory.GetCount("gem"));
        }

        [Test]
        public void InvalidItemIsRejected()
        {
            var inventory = new SimpleInventoryService(16, 99);
            Assert.IsFalse(inventory.TryAdd(default, out _));
        }
    }
}
