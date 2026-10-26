namespace Yumiwa.Domain
{
    /// <summary>Pure item identity snapshot shared by inspection, inventory, and world items.</summary>
    public readonly struct ItemDefinition
    {
        public readonly string ItemId;
        public readonly string DisplayName;
        public readonly string Description;
        public readonly int Quantity;
        public readonly ItemKind Kind;
        public readonly bool IsObjectiveItem;

        public ItemDefinition(string itemId, string displayName, string description, int quantity = 1, ItemKind kind = ItemKind.PhysicalTool, bool isObjectiveItem = false)
        {
            ItemId = itemId;
            DisplayName = displayName;
            Description = description;
            Quantity = quantity;
            Kind = kind;
            IsObjectiveItem = isObjectiveItem;
        }

        public bool HasIdentity => !string.IsNullOrEmpty(ItemId) && Quantity > 0;
        public bool IsPhysical => Kind == ItemKind.PhysicalTool;
        public bool IsCriticalEvidence => Kind == ItemKind.CriticalEvidence;

        public static bool IsValid(ItemDefinition item) => item.HasIdentity;
    }
}
