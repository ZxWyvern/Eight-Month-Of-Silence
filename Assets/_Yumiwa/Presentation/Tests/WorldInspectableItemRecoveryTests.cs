using NUnit.Framework;
using UnityEngine;
using Yumiwa.Domain;
using Yumiwa.Application;

namespace Yumiwa.Presentation.Tests
{
    [TestFixture]
    public sealed class WorldInspectableItemRecoveryTests
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

        private static string RegistryDump(AutoReturnService recovery)
        {
            var f = typeof(AutoReturnService)
                .GetField("registry", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var reg = (System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<System.Action>>)f.GetValue(recovery);
            var parts = new System.Collections.Generic.List<string>();
            foreach (var kv in reg) parts.Add(kv.Key + ":" + kv.Value.Count);
            return "[" + string.Join(",", parts) + "]";
        }

        private static WorldInspectableItem Spawn(string id, ItemDefinition def, bool destroyOnTake,
            EventBus bus, AutoReturnService recovery)
        {
            // Drive the Unity lifecycle explicitly instead of relying on SetActive
            // timing (which proved unreliable in EditMode): the first Awake/OnEnable
            // from AddComponent is an empty-id guarded no-op; after fields + injection
            // exist we fire OnDisable/OnEnable directly so registration is deterministic.
            // Real activation wiring remains covered by the Play Mode sweep.
            var go = new GameObject("TestItem_" + id, typeof(BoxCollider));
            var item = go.AddComponent<WorldInspectableItem>();
            SetField(item, "itemId", id);
            SetField(item, "destroyOnTake", destroyOnTake);
            item.Construct(recovery, bus, new FakeConfig(def));
            Fire(item, "OnDisable");
            Fire(item, "OnEnable"); // registers the restore callback
            return item;
        }

        private static void ProveRegistered(WorldInspectableItem item, AutoReturnService recovery, ItemDefinition def)
        {
            // A probe restore on the live item must clear pending; otherwise every
            // later assertion in the test would pass vacuously (no callback at all).
            Assert.IsTrue(item.IsAvailable, "Spawn must leave the item available.");
            Debug.Log("[F03probe] before probe: registry=" + RegistryDump(recovery)
                + " active=" + item.gameObject.activeSelf + " id=" + item.ItemId);
            recovery.EnqueueRecovery(def);
            Debug.Log("[F03probe] after probe: pending=" + recovery.PendingRecoveries.Count);
            Assert.AreEqual(0, recovery.PendingRecoveries.Count,
                "Probe restore must clear pending: registration or Restore is broken (see [F03probe] dump above).");
        }

        [Test]
        public void DestroyedItem_Unregisters_NoDeadRestore_PendingKept()
        {
            var bus = new EventBus();
            var recovery = new AutoReturnService(bus);
            var def = new ItemDefinition("tool_key", "Tool", "", 1, ItemKind.PhysicalTool, false);
            var item = Spawn("tool_key", def, true, bus, recovery);
            var go = item.gameObject;
            ProveRegistered(item, recovery, def);

            // Simulate post-Consume taken state: Consume's own Destroy() cannot run
            // in EditMode (Unity rejects Destroy outside play mode), so set taken
            // directly and force the real teardown path below.
            SetField(item, "taken", true);
            Object.DestroyImmediate(go); // OnDisable (taken, kept) + OnDestroy (must unregister)

            recovery.EnqueueRecovery(def);
            Assert.AreEqual(1, recovery.PendingRecoveries.Count,
                "Dead restore target must not silently clear pending.");
        }

        [Test]
        public void Consume_ObjectiveItem_NeverDestroyed_StaysRecoverable()
        {
            var bus = new EventBus();
            var recovery = new AutoReturnService(bus);
            var def = new ItemDefinition("obj_key", "Objective", "", 1, ItemKind.PhysicalTool, true);
            var item = Spawn("obj_key", def, true, bus, recovery);
            var go = item.gameObject;
            ProveRegistered(item, recovery, def);

            item.Consume(); // objective + destroyOnTake must fall back to deactivate

            Assert.IsFalse(item == null, "Objective item must survive Consume (deactivate, not destroy).");
            Assert.IsFalse(go.activeSelf);
            Assert.IsFalse(item.IsAvailable);

            recovery.EnqueueRecovery(def);
            Assert.IsTrue(go.activeSelf, "Restore must re-activate the world item.");
            Assert.IsTrue(item.IsAvailable);
            Assert.AreEqual(0, recovery.PendingRecoveries.Count);
        }
    }
}
