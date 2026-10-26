using System;
using Yumiwa.Domain;

namespace Yumiwa.Application
{
    public struct InventorySlot
    {
        public readonly InventorySlotType Type;
        public readonly ItemDefinition Item;
        public readonly bool IsOccupied;

        public InventorySlot(InventorySlotType type, ItemDefinition item)
        {
            Type = type;
            Item = item;
            IsOccupied = ItemDefinition.IsValid(item);
        }
    }
}