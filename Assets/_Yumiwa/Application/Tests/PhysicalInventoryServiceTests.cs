using NUnit.Framework;
using Yumiwa.Domain;
using Yumiwa.Application;

namespace Yumiwa.Application.Tests
{
    [TestFixture]
    public class PhysicalInventoryServiceTests
    {
        private ItemDefinition CreateItem(string id) => new ItemDefinition(id, id, "Desc");

        [Test]
        public void Inventory_LimitsTo_6_Slots()
        {
            var inventory = new PhysicalInventoryService();
            
            for (int i = 0; i < 6; i++)
            {
                Assert.IsTrue(inventory.TryAdd(CreateItem($"item{i}"), out _));
            }

            Assert.IsFalse(inventory.TryAdd(CreateItem("item7"), out string reason));
            Assert.AreEqual("Inventory full (6 slots max: 2 hands + 4 pockets).", reason);
        }

        [Test]
        public void TransferToHand_Occupies_HandSlot()
        {
            var inventory = new PhysicalInventoryService();
            var item = CreateItem("key");
            
            inventory.TryAdd(item, out _); // Goes to pocket first
            Assert.IsTrue(inventory.IsInPocket(item));
            Assert.IsFalse(inventory.IsHand(item));

            Assert.IsTrue(inventory.TransferToHand(item, HandType.Right, out _));
            Assert.IsTrue(inventory.IsHand(item));
            Assert.IsFalse(inventory.IsInPocket(item));
        }

        [Test]
        public void Drop_RemovesItem()
        {
            var inventory = new PhysicalInventoryService();
            var item = CreateItem("key");
            inventory.TryAdd(item, out _);
            
            Assert.IsTrue(inventory.Drop(item, out _));
            Assert.AreEqual(0, inventory.GetCount("key"));
        }

        [Test]
        public void IsInHand_ReflectsHandSlotsOnly()
        {
            var inventory = new PhysicalInventoryService();
            var pistol = CreateItem("pistol");
            var flash = CreateItem("flashlight");
            // Add pistol to pocket, not yet hand
            Assert.IsTrue(inventory.TryAdd(pistol, out _));
            Assert.IsFalse(inventory.IsInHand("pistol"), "Should not be in hand after TryAdd (goes to pocket)");
            Assert.IsFalse(inventory.IsInHand("weapon"), "Alias weapon should not match pistol");
            // Move to hand
            Assert.IsTrue(inventory.QuickSwapToHand("pistol", out _));
            Assert.IsTrue(inventory.IsInHand("pistol"));
            Assert.IsTrue(((IInventoryService)inventory).IsInHand("pistol"), "Interface IsInHand should match");
            // flashlight not yet added
            Assert.IsFalse(inventory.IsInHand("flashlight"));
            Assert.IsTrue(inventory.TryAdd(flash, out _));
            Assert.IsFalse(inventory.IsInHand("flashlight"));
            Assert.IsTrue(inventory.QuickSwapToHand("flashlight", out _));
            Assert.IsTrue(inventory.IsInHand("flashlight"));
            // Drop pistol removes hand state
            Assert.IsTrue(inventory.Drop(pistol, out _));
            Assert.IsFalse(inventory.IsInHand("pistol"));
            Assert.IsTrue(inventory.IsInHand("flashlight"), "Flash still in hand after pistol drop");
        }

        [Test]
        public void TransferToPocket_SetsPocketSlotType()
        {
            var inventory = new PhysicalInventoryService();
            var item = CreateItem("crowbar");

            Assert.IsTrue(inventory.TryAdd(item, out _));
            Assert.IsTrue(inventory.TransferToHand(item, HandType.Right, out _));
            Assert.IsTrue(inventory.IsHand(item));

            Assert.IsTrue(inventory.TransferToPocket(item, out string reject), reject);
            Assert.IsFalse(inventory.IsHand(item));
            Assert.IsTrue(inventory.IsInPocket(item));

            var slots = inventory.GetSlots();
            InventorySlot pocketSlot = default;
            bool found = false;
            for (int i = 2; i < slots.Length; i++)
            {
                if (slots[i].IsOccupied && slots[i].Item.ItemId == "crowbar")
                {
                    pocketSlot = slots[i];
                    found = true;
                    Assert.AreEqual((InventorySlotType)i, slots[i].Type);
                }
            }
            Assert.IsTrue(found, "Transferred item must occupy a pocket slot.");
            Assert.AreNotEqual(InventorySlotType.HandLeft, pocketSlot.Type);
            Assert.AreNotEqual(InventorySlotType.HandRight, pocketSlot.Type);
        }

        [Test]
        public void TransferToPocket_RoundTrip_PreservesSlotTypes()
        {
            var inventory = new PhysicalInventoryService();
            var item = CreateItem("key");

            Assert.IsTrue(inventory.TryAdd(item, out _));
            Assert.IsTrue(inventory.TransferToHand(item, HandType.Left, out _));
            Assert.IsTrue(inventory.TransferToPocket(item, out _));
            Assert.IsTrue(inventory.TransferToHand(item, HandType.Left, out _));

            var slots = inventory.GetSlots();
            Assert.IsTrue(slots[0].IsOccupied);
            Assert.AreEqual(InventorySlotType.HandLeft, slots[0].Type);
            Assert.AreEqual("key", slots[0].Item.ItemId);
            for (int i = 2; i < slots.Length; i++)
                Assert.IsFalse(slots[i].IsOccupied && slots[i].Item.ItemId == "key");
        }

        [Test]
        public void IsInHand_UsesCanonicalPistolId()
        {
            var inventory = new PhysicalInventoryService();
            var pistol = CreateItem("pistol");
            inventory.TryAdd(pistol, out _);
            inventory.QuickSwapToHand("pistol", out _);
            // EquipmentPivotPresenter canonical check is IsInHand("pistol") only (no weapon alias after F3)
            Assert.IsTrue(inventory.IsInHand("pistol"));
            Assert.IsFalse(inventory.IsInHand("weapon"));
            Assert.IsFalse(inventory.IsInHand(""), "Empty id false");
            Assert.IsFalse(inventory.IsInHand(null), "Null id false");
        }
    }
}
