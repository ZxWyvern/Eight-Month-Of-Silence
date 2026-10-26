namespace Yumiwa.Domain
{
    /// <summary>Raised exactly once per confirmed Take that the inventory accepted.</summary>
    public readonly struct ItemPickedUpEvent
    {
        public readonly string ItemId;
        public readonly int Quantity;

        public ItemPickedUpEvent(string itemId, int quantity)
        {
            ItemId = itemId;
            Quantity = quantity;
        }
    }
}
