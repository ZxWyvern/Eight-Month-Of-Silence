using System.Collections.Generic;
using Yumiwa.Domain;

namespace Yumiwa.Application
{
    /// <summary>
    /// Physical inventory per GDD §03:
    /// Left Hand (1) + Right Hand (1) + Pocket (4) = 6 total items max.
    /// Only hand items can be used directly; pocket items need transfer first.
    /// Anti soft-lock: objective drop via DropWithRecovery + IRecoveryPoint auto-return (ADR-0012).
    /// Visual pivot (EquipmentPivotPresenter) queries IsInHand via IInventoryService — no slot exposure needed.
    /// </summary>
    public sealed class PhysicalInventoryService : IInventoryService
    {
        private readonly InventorySlot[] slots = new InventorySlot[6];
        private readonly IEventBus bus;
        private readonly IRecoveryPoint recovery;

        public PhysicalInventoryService(IEventBus bus = null, IRecoveryPoint recovery = null)
        {
            this.bus = bus;
            this.recovery = recovery;
            slots[0] = new InventorySlot(InventorySlotType.HandLeft, default);
            slots[1] = new InventorySlot(InventorySlotType.HandRight, default);
            slots[2] = new InventorySlot(InventorySlotType.Pocket0, default);
            slots[3] = new InventorySlot(InventorySlotType.Pocket1, default);
            slots[4] = new InventorySlot(InventorySlotType.Pocket2, default);
            slots[5] = new InventorySlot(InventorySlotType.Pocket3, default);
        }

        public bool CanAccept(ItemDefinition item, out string rejectReason)
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
            if (IsFull())
            {
                rejectReason = "Inventory full (6 slots max: 2 hands + 4 pockets).";
                return false;
            }
            rejectReason = null;
            return true;
        }

        public bool TryAdd(ItemDefinition item, out string rejectReason)
        {
            if (!CanAccept(item, out rejectReason))
                return false;

            int pocketIdx = FindFirstEmptyPocket();
            if (pocketIdx >= 0)
            {
                slots[pocketIdx] = new InventorySlot((InventorySlotType)pocketIdx, item);
                rejectReason = null;
                return true;
            }

            for (int i = 0; i < 2; i++)
            {
                if (!slots[i].IsOccupied)
                {
                    slots[i] = new InventorySlot((InventorySlotType)i, item);
                    rejectReason = null;
                    return true;
                }
            }

            rejectReason = "No slot available.";
            return false;
        }

        public int GetCount(string itemId)
        {
            int count = 0;
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i].IsOccupied && slots[i].Item.ItemId == itemId)
                    count += slots[i].Item.Quantity;
            }
            return count;
        }

        public bool IsInHand(string itemId)
        {
            if (string.IsNullOrEmpty(itemId)) return false;
            for (int i = 0; i < 2; i++)
                if (slots[i].IsOccupied && slots[i].Item.ItemId == itemId)
                    return true;
            return false;
        }

        public string GetHandItemId(int handIndex)
        {
            if (handIndex < 0 || handIndex > 1) return null;
            return slots[handIndex].IsOccupied ? slots[handIndex].Item.ItemId : null;
        }

        public bool IsHand(ItemDefinition item) => IsInHand(item.ItemId);

        public bool IsInPocket(ItemDefinition item)
        {
            for (int i = 2; i < slots.Length; i++)
            {
                if (slots[i].IsOccupied && slots[i].Item.ItemId == item.ItemId)
                    return true;
            }
            return false;
        }

        public bool CanUse(ItemDefinition item) => IsHand(item);

        public bool TransferToHand(ItemDefinition item, HandType hand, out string rejectReason)
        {
            rejectReason = null;
            int sourceIdx = FindItemInPocket(item.ItemId);
            if (sourceIdx < 0)
            {
                rejectReason = "Item not found in pocket.";
                return false;
            }
            int handIdx = hand == HandType.Left ? 0 : 1;
            if (slots[handIdx].IsOccupied)
            {
                rejectReason = $"Hand {hand} is occupied. Use ReplaceHand or SwapHands.";
                return false;
            }
            slots[handIdx] = new InventorySlot((InventorySlotType)handIdx, slots[sourceIdx].Item);
            slots[sourceIdx] = new InventorySlot((InventorySlotType)sourceIdx, default);
            return true;
        }

        public bool ReplaceHand(ItemDefinition item, HandType hand, out string rejectReason)
        {
            rejectReason = null;
            int sourceIdx = FindItemInPocket(item.ItemId);
            if (sourceIdx < 0)
            {
                rejectReason = "Item not found in pocket.";
                return false;
            }
            int handIdx = hand == HandType.Left ? 0 : 1;
            if (!slots[handIdx].IsOccupied)
            {
                slots[handIdx] = new InventorySlot((InventorySlotType)handIdx, slots[sourceIdx].Item);
                slots[sourceIdx] = new InventorySlot((InventorySlotType)sourceIdx, default);
                return true;
            }
            if (slots[sourceIdx].Item.IsObjectiveItem)
            {
                var displaced = slots[handIdx].Item;
                slots[handIdx] = new InventorySlot((InventorySlotType)handIdx, slots[sourceIdx].Item);
                slots[sourceIdx] = new InventorySlot((InventorySlotType)sourceIdx, displaced);
                return true;
            }
            var toReplace = slots[handIdx].Item;
            slots[handIdx] = new InventorySlot((InventorySlotType)handIdx, slots[sourceIdx].Item);
            slots[sourceIdx] = new InventorySlot((InventorySlotType)sourceIdx, toReplace);
            return true;
        }

        public bool SwapHands(out string rejectReason)
        {
            rejectReason = null;
            if (!slots[0].IsOccupied && !slots[1].IsOccupied)
            {
                rejectReason = "Both hands empty.";
                return false;
            }
            var left = slots[0];
            var right = slots[1];
            slots[0] = new InventorySlot(InventorySlotType.HandLeft, right.IsOccupied ? right.Item : default);
            slots[1] = new InventorySlot(InventorySlotType.HandRight, left.IsOccupied ? left.Item : default);
            return true;
        }

        public bool QuickSwapToHand(string itemId, out string rejectReason)
        {
            rejectReason = null;
            int pocketIdx = FindItemInPocket(itemId);
            if (pocketIdx < 0)
            {
                rejectReason = "Item not found in pocket.";
                return false;
            }
            for (int i = 0; i < 2; i++)
            {
                if (!slots[i].IsOccupied)
                {
                    slots[i] = new InventorySlot((InventorySlotType)i, slots[pocketIdx].Item);
                    slots[pocketIdx] = new InventorySlot((InventorySlotType)pocketIdx, default);
                    return true;
                }
            }
            return ReplaceHand(slots[pocketIdx].Item, HandType.Left, out rejectReason);
        }

        public bool TransferToPocket(ItemDefinition item, out string rejectReason)
        {
            rejectReason = null;
            int sourceIdx = FindItemInHand(item.ItemId);
            if (sourceIdx < 0)
            {
                rejectReason = "Item not found in hand.";
                return false;
            }
            int pocketIdx = FindFirstEmptyPocket();
            if (pocketIdx < 0)
            {
                rejectReason = "No pocket slot available.";
                return false;
            }
            slots[pocketIdx] = new InventorySlot((InventorySlotType)pocketIdx, slots[sourceIdx].Item);
            slots[sourceIdx] = new InventorySlot((InventorySlotType)sourceIdx, default);
            return true;
        }

        public bool Drop(ItemDefinition item, out string rejectReason)
        {
            rejectReason = null;
            int idx = FindItemIndex(item.ItemId);
            if (idx < 0)
            {
                rejectReason = "Item not found.";
                return false;
            }
            if (slots[idx].Item.IsObjectiveItem)
            {
                rejectReason = "Objective item cannot be dropped permanently — use recovery point.";
                return false;
            }
            slots[idx] = new InventorySlot((InventorySlotType)idx, default);
            return true;
        }

        public bool DropWithRecovery(ItemDefinition item, out ItemDefinition recovered, out string rejectReason)
        {
            recovered = default;
            rejectReason = null;
            int idx = FindItemIndex(item.ItemId);
            if (idx < 0)
            {
                rejectReason = "Item not found.";
                return false;
            }
            recovered = slots[idx].Item;
            slots[idx] = new InventorySlot((InventorySlotType)idx, default);
            if (recovered.IsObjectiveItem)
            {
                recovery?.EnqueueRecovery(recovered);
            }
            else
            {
                bus?.Publish(new ItemDroppedEvent(recovered.ItemId, false));
            }
            return true;
        }

        public bool Contains(string itemId) => FindItemIndex(itemId) >= 0;

        public InventorySlot[] GetSlots() => (InventorySlot[])slots.Clone();

        public ItemDefinition[] ExportInventory()
        {
            var arr = new ItemDefinition[slots.Length];
            for (int i = 0; i < slots.Length; i++) arr[i] = slots[i].IsOccupied ? slots[i].Item : default;
            return arr;
        }

        public void ImportInventory(ItemDefinition[] items)
        {
            for (int i = 0; i < slots.Length; i++) slots[i] = new InventorySlot((InventorySlotType)i, default);
            if (items == null) return;
            for (int i = 0; i < items.Length && i < slots.Length; i++)
                if (ItemDefinition.IsValid(items[i])) slots[i] = new InventorySlot((InventorySlotType)i, items[i]);
        }

        public void Clear()
        {
            for (int i = 0; i < slots.Length; i++) slots[i] = new InventorySlot((InventorySlotType)i, default);
        }

        private bool IsFull()
        {
            for (int i = 0; i < slots.Length; i++)
                if (!slots[i].IsOccupied)
                    return false;
            return true;
        }

        private int FindFirstEmptyPocket()
        {
            for (int i = 2; i < slots.Length; i++)
                if (!slots[i].IsOccupied)
                    return i;
            return -1;
        }

        private int FindItemInPocket(string itemId)
        {
            for (int i = 2; i < slots.Length; i++)
                if (slots[i].IsOccupied && slots[i].Item.ItemId == itemId)
                    return i;
            return -1;
        }

        private int FindItemInHand(string itemId)
        {
            for (int i = 0; i < 2; i++)
                if (slots[i].IsOccupied && slots[i].Item.ItemId == itemId)
                    return i;
            return -1;
        }

        private int FindItemIndex(string itemId)
        {
            for (int i = 0; i < slots.Length; i++)
                if (slots[i].IsOccupied && slots[i].Item.ItemId == itemId)
                    return i;
            return -1;
        }
    }
}
