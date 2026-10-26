namespace Yumiwa.Domain
{
    /// <summary>
    /// Physical vs log routing per GDD §03 Aturan Evidence / Inventory.
    /// Critical evidence never occupies a physical slot and cannot be dropped (anti soft-lock).
    /// </summary>
    public enum ItemKind
    {
        PhysicalTool = 0,
        CriticalEvidence = 1,
        OptionalEvidence = 2
    }
}
