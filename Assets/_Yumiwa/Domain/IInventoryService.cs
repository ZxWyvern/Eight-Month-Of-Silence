namespace Yumiwa.Domain
{
    /// <summary>
    /// Inventory boundary for the inspection system. The inspection code never
    /// touches a concrete inventory; any capacity rules live behind this contract.
    /// Presentation (pivot visuals) checks hand state without knowing slots — via IsInHand/GetHandItemId (0B per-frame).
    /// </summary>
    public interface IInventoryService
    {
        bool CanAccept(ItemDefinition item, out string rejectReason);
        bool TryAdd(ItemDefinition item, out string rejectReason);
        int GetCount(string itemId);
        /// <summary>True if itemId is currently equipped in either hand (HandLeft/HandRight).</summary>
        bool IsInHand(string itemId);
        /// <summary>Returns ItemId in hand slot 0=Left (Hand2) / 1=Right (Hand1), or null if empty. 0B per-frame (no alloc).</summary>
        string GetHandItemId(int handIndex);
        ItemDefinition[] ExportInventory();
        void ImportInventory(ItemDefinition[] items);
        void Clear();
    }
}
