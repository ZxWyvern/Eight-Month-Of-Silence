using NUnit.Framework;
using UnityEngine;
using Yumiwa.Domain;
using Yumiwa.Application;

namespace Yumiwa.Presentation.Tests
{
    /// <summary>
    /// FIX-01 Presentation side: a consumed world item restores when the loaded
    /// snapshot does not own its id, and stays consumed when it does — reload
    /// can neither lose nor duplicate world items.
    /// </summary>
    [TestFixture]
    public sealed class WorldItemCheckpointReconcileTests
    {
        private sealed class FakeConfig : IConfigProvider
        {
            private readonly ItemDefinition def;
            public FakeConfig(ItemDefinition def) { this.def = def; }
            public MovementConfigData GetMovement() => default;
            public StaminaConfigData GetStamina() => default;
            public CensorEffectData GetCensor() => default;
            public InspectionTuningData GetInspection() => default;
            public bool HasItem(string itemId) => itemId == def.ItemId;
            public ItemDefinition GetItemDefinition(string itemId) => def;
            public Evidence GetEvidence(string itemId) => default;
            public InspectionHotspot[] GetHotspots(string itemId) => System.Array.Empty<InspectionHotspot>();
        }

        private static void SetField(object o, string name, object v) =>
            typeof(WorldInspectableItem)
                .GetField(name, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(o, v);

        private static void Fire(WorldInspectableItem item, string method) =>
            typeof(WorldInspectableItem)
                .GetMethod(method, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .Invoke(item, null);

        private static WorldInspectableItem Spawn(string id, ItemDefinition def, EventBus bus, AutoReturnService recovery)
        {
            var go = new GameObject("TestItem_" + id, typeof(BoxCollider));
            var item = go.AddComponent<WorldInspectableItem>();
            SetField(item, "itemId", id);
            SetField(item, "destroyOnTake", false);
            item.Construct(recovery, bus, new FakeConfig(def));
            Fire(item, "OnDisable");
            Fire(item, "OnEnable");
            return item;
        }

        [Test]
        public void ConsumedItem_Restores_WhenSnapshotDoesNotOwnId()
        {
            var bus = new EventBus();
            var recovery = new AutoReturnService(bus);
            var def = new ItemDefinition("b1_body", "Tubuh", "", 1, ItemKind.CriticalEvidence, false);
            var item = Spawn("b1_body", def, bus, recovery);
            try
            {
                item.Consume();
                Assert.IsFalse(item.IsAvailable);

                // Snapshot taken BEFORE the Take owns neither inventory nor evidence ids.
                bus.Publish(new CheckpointLoadedEvent(
                    System.Array.Empty<string>(), System.Array.Empty<string>()));

                Assert.IsTrue(item.IsAvailable);
            }
            finally { Object.DestroyImmediate(item.gameObject); }
        }

        [Test]
        public void ConsumedItem_StaysConsumed_WhenSnapshotOwnsId()
        {
            var bus = new EventBus();
            var recovery = new AutoReturnService(bus);
            var def = new ItemDefinition("b1_body", "Tubuh", "", 1, ItemKind.CriticalEvidence, false);
            var item = Spawn("b1_body", def, bus, recovery);
            try
            {
                item.Consume();
                Assert.IsFalse(item.IsAvailable);

                // Snapshot taken AFTER the Take: evidence owns the id — no duplicate.
                bus.Publish(new CheckpointLoadedEvent(
                    System.Array.Empty<string>(), new[] { "b1_body" }));

                Assert.IsFalse(item.IsAvailable);
            }
            finally { Object.DestroyImmediate(item.gameObject); }
        }

        [Test]
        public void AvailableItem_Consumes_WhenSnapshotOwnsId()
        {
            var bus = new EventBus();
            var recovery = new AutoReturnService(bus);
            var def = new ItemDefinition("crowbar", "Crowbar", "", 1, ItemKind.PhysicalTool, false);
            var item = Spawn("crowbar", def, bus, recovery);
            try
            {
                Assert.IsTrue(item.IsAvailable);

                // Reloading a snapshot whose inventory holds the item must not
                // leave a second copy in the world.
                bus.Publish(new CheckpointLoadedEvent(
                    new[] { "crowbar" }, System.Array.Empty<string>()));

                Assert.IsFalse(item.IsAvailable);
            }
            finally { Object.DestroyImmediate(item.gameObject); }
        }
    }
}
