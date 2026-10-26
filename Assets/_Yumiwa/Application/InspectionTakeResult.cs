namespace Yumiwa.Application
{
    /// <summary>Outcome of a single ConfirmTake call; session stays active on rejection.</summary>
    public enum InspectionTakeResult
    {
        Success,
        SessionNotActive,
        AlreadyCompleted,
        RejectedByInventory
    }
}
