namespace Yumiwa.Domain
{
    /// <summary>Raised when Take was pressed but the inventory rejected the item.</summary>
    public readonly struct InspectionTakeFailedEvent
    {
        public readonly string ItemId;
        public readonly string Reason;

        public InspectionTakeFailedEvent(string itemId, string reason)
        {
            ItemId = itemId;
            Reason = reason;
        }
    }
}
