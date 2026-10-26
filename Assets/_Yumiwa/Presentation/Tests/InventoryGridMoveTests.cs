using NUnit.Framework;
using UnityEngine;
using Yumiwa.Domain;
using Yumiwa.Application;

namespace Yumiwa.Presentation.Tests
{
    [TestFixture]
    public sealed class InventoryGridMoveTests
    {
        [Test]
        public void MoveSlot_EmptySlot_NoOp(){
          var bus = new EventBus();
          var recovery = new AutoReturnService(bus);
          var inv = new PhysicalInventoryService(bus, recovery);
          var go = new GameObject();
          var presenter = go.AddComponent<InventoryPresenter>();
          // Inject via reflection for test
          typeof(InventoryPresenter).GetField("physical", System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).SetValue(presenter, inv);
          var item = new ItemDefinition("crowbar","", "",1, ItemKind.PhysicalTool,false);
          string r; inv.TryAdd(item, out r);
          // TryAdd routes to pocket first, so slot 0 is empty: 0->0 and 0->1 must both be no-ops.
          presenter.MoveSlot(0,0);
          presenter.MoveSlot(0,1);
          var after = inv.GetSlots();
          Assert.IsFalse(after[0].IsOccupied, "Empty slot 0 must stay empty after no-op moves.");
          Assert.IsFalse(after[1].IsOccupied, "Empty slot 1 must stay empty after no-op move.");
          Assert.AreEqual("crowbar", after[2].Item.ItemId, "Item must remain at its pocket slot.");
          Assert.AreEqual(1, inv.GetCount("crowbar"));
          // Occupied same-slot move must also be a no-op.
          presenter.MoveSlot(2,2);
          var afterSame = inv.GetSlots();
          Assert.AreEqual("crowbar", afterSame[2].Item.ItemId, "Occupied same-slot move must not displace the item.");
          Assert.AreEqual(1, inv.GetCount("crowbar"));
          Object.DestroyImmediate(go);
        }

        [Test]
        public void MoveSlot_Swap_WhenOccupied(){
          var bus = new EventBus();
          var recovery = new AutoReturnService(bus);
          var inv = new PhysicalInventoryService(bus, recovery);
          var go = new GameObject();
          var presenter = go.AddComponent<InventoryPresenter>();
          typeof(InventoryPresenter).GetField("physical", System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).SetValue(presenter, inv);
          var item1 = new ItemDefinition("crowbar","", "",1, ItemKind.PhysicalTool,false);
          var item2 = new ItemDefinition("brass_key","", "",1, ItemKind.PhysicalTool,false);
          string r; inv.TryAdd(item1, out r); inv.TryAdd(item2, out r);
          // Find indices
          var slots = inv.GetSlots();
          int idxCrowbar = -1, idxBrass = -1;
          for(int i=0;i<slots.Length;i++){
            if(slots[i].Item.ItemId=="crowbar") idxCrowbar=i;
            if(slots[i].Item.ItemId=="brass_key") idxBrass=i;
          }
          presenter.MoveSlot(idxCrowbar, idxBrass);
          var after = inv.GetSlots();
          Assert.AreEqual("crowbar", after[idxBrass].Item.ItemId);
          Assert.AreEqual("brass_key", after[idxCrowbar].Item.ItemId);
          Object.DestroyImmediate(go);
        }
    }
}
