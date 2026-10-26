using System.Collections.Generic;
using Yumiwa.Domain;

namespace Yumiwa.Application
{
    /// <summary>
    /// Minimal bounded inventory behind IInventoryService: slot-count capacity plus
    /// a per-item stack cap. Swap for a save-backed implementation without touching
    /// the inspection system. Legacy shim for tests — delegates hand check to GetCount.
    /// </summary>
    public sealed class SimpleInventoryService : IInventoryService
    {
        private readonly Dictionary<string, int> counts = new Dictionary<string, int>();
        private readonly int maxSlots;
        private readonly int maxStackPerSlot;

        public SimpleInventoryService(int maxSlots = 16, int maxStackPerSlot = 99)
        {
            this.maxSlots = maxSlots < 1 ? 1 : maxSlots;
            this.maxStackPerSlot = maxStackPerSlot < 1 ? 1 : maxStackPerSlot;
        }

        public bool CanAccept(ItemDefinition item, out string rejectReason) =>
            Check(item, out rejectReason);

        public bool TryAdd(ItemDefinition item, out string rejectReason)
        {
            if (!Check(item, out rejectReason))
                return false;
            if (counts.TryGetValue(item.ItemId, out int held))
                counts[item.ItemId] = held + item.Quantity;
            else
                counts[item.ItemId] = item.Quantity;
            rejectReason = null;
            return true;
        }

        public int GetCount(string itemId)
        {
            if (string.IsNullOrEmpty(itemId))
                return 0;
            return counts.TryGetValue(itemId, out int held) ? held : 0;
        }

        public bool IsInHand(string itemId) => GetCount(itemId) > 0;

        public string GetHandItemId(int handIndex) => null; // Simple shim has no hand slots (always pooled)

        public ItemDefinition[] ExportInventory()
        {
            var list = new System.Collections.Generic.List<ItemDefinition>();
            foreach (var kv in counts) list.Add(new ItemDefinition(kv.Key, kv.Key, "", kv.Value, ItemKind.PhysicalTool, false));
            return list.ToArray();
        }

        public void ImportInventory(ItemDefinition[] items)
        {
            counts.Clear();
            if (items == null) return;
            foreach (var it in items) if (ItemDefinition.IsValid(it)) counts[it.ItemId] = it.Quantity;
        }

        public void Clear() => counts.Clear();

        private bool Check(ItemDefinition item, out string rejectReason)
        {
            if (!ItemDefinition.IsValid(item))
            {
                rejectReason = "Invalid item.";
                return false;
            }
            if (item.IsCriticalEvidence)
            {
                rejectReason = "Critical evidence goes to Evidence Log, not inventory.";
                return false;
            }
            if (item.Kind == ItemKind.OptionalEvidence)
            {
                rejectReason = "Optional evidence goes to Evidence Log, not inventory.";
                return false;
            }
            int held = GetCount(item.ItemId);
            if (held + item.Quantity > maxStackPerSlot)
            {
                rejectReason = "Cannot carry more of this item.";
                return false;
            }
            if (held == 0 && counts.Count >= maxSlots)
            {
                rejectReason = "Inventory full.";
                return false;
            }
            rejectReason = null;
            return true;
        }
    }
}
