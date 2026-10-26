namespace Yumiwa.Domain
{
    /// <summary>Raised when an inspection session closes, taken or cancelled.</summary>
    public readonly struct InspectionEndedEvent
    {
        public readonly string ItemId;
        public readonly bool Taken;

        public InspectionEndedEvent(string itemId, bool taken)
        {
            ItemId = itemId;
            Taken = taken;
        }
    }
}
