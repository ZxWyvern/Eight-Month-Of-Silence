namespace Yumiwa.Domain
{
    /// <summary>Raised when an inspection session opens for a world item.</summary>
    public readonly struct InspectionStartedEvent
    {
        public readonly string ItemId;

        public InspectionStartedEvent(string itemId)
        {
            ItemId = itemId;
        }
    }
}
