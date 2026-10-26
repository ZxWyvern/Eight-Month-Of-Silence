namespace Yumiwa.Domain
{
    /// <summary>
    /// Published after any checkpoint/chapter load with the snapshot's content
    /// identity. World items reconcile themselves against it (FIX-01): consumed
    /// items absent from the snapshot restore, snapshot-owned items stay consumed.
    /// Runtime-only; never serialized.
    /// </summary>
    public readonly struct CheckpointLoadedEvent
    {
        public readonly string[] InventoryIds;
        public readonly string[] EvidenceIds;

        public CheckpointLoadedEvent(string[] inventoryIds, string[] evidenceIds)
        {
            InventoryIds = inventoryIds ?? System.Array.Empty<string>();
            EvidenceIds = evidenceIds ?? System.Array.Empty<string>();
        }

        public bool Contains(string id)
        {
            if (string.IsNullOrEmpty(id))
                return false;
            for (int i = 0; i < InventoryIds.Length; i++)
                if (InventoryIds[i] == id)
                    return true;
            for (int i = 0; i < EvidenceIds.Length; i++)
                if (EvidenceIds[i] == id)
                    return true;
            return false;
        }
    }
}
