using NUnit.Framework;
using UnityEngine;
using Yumiwa.Domain;

namespace Yumiwa.Presentation.Tests
{
    [TestFixture]
    public sealed class InventoryGridViewTests
    {
        [Test]
        public void SlotView_Set_HandHighlight_And_Qty(){
          var go = new GameObject();
          var view = go.AddComponent<Yumiwa.Presentation.InventorySlotView>();
          var item = new Yumiwa.Domain.ItemDefinition("crowbar","Crowbar","",1,Yumiwa.Domain.ItemKind.PhysicalTool,false);
          view.Set(item, true, false);
          Assert.IsTrue(view.IsHandHighlighted);
          Assert.AreEqual("crowbar", view.CurrentItemId);
          Object.DestroyImmediate(go);
        }
    }
}
