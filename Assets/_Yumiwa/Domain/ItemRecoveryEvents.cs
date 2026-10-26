namespace Yumiwa.Domain
{
    /// <summary>Published when an objective item is queued for auto-return (DropWithRecovery).</summary>
    public readonly struct ItemDroppedEvent
    {
        public readonly string ItemId;
        public readonly bool IsObjective;
        public ItemDroppedEvent(string itemId, bool isObjective) { ItemId = itemId; IsObjective = isObjective; }
    }

    /// <summary>Published when a world item is restored to its recovery point.</summary>
    public readonly struct ItemRecoveredEvent
    {
        public readonly string ItemId;
        public ItemRecoveredEvent(string itemId) { ItemId = itemId; }
    }
}
