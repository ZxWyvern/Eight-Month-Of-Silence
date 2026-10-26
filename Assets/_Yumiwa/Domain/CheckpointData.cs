namespace Yumiwa.Domain
{
    /// <summary>
    /// Engine-agnostic checkpoint snapshot.
    /// Uses raw floats instead of UnityEngine types to honour Domain's noEngineReferences contract.
    /// Presentation-layer code packs/unpacks Unity transforms when calling Save/Load.
    /// </summary>
    [System.Serializable]
    public struct CheckpointData
    {
        public float PosX;
        public float PosY;
        public float PosZ;
        public float RotX;
        public float RotY;
        public float RotZ;
        public float RotW;
        public float Health;
        public float Stamina;
        public MovementState MovementState;
        public string ActiveScene;
        public long Timestamp;
        public CheckpointMilestone Milestone;
        public string SaveVersion;
        // ADR-0012: pending objective recoveries (itemIds) — null/empty means none. Version 2+.
        public string[] PendingRecoveredIds;
        // P1-M7: discovered hotspot keys "itemId|hotspotId" persisting Cancel progress across death. Version 3+.
        public string[] DiscoveredHotspotKeys;
        // P1-F05: full progression snapshot — inventory, evidence, puzzles, mission. Version 4+.
        public CheckpointInventoryItem[] Inventory;
        public CheckpointEvidence[] EvidenceLog;
        public PuzzleSnapshot[] Puzzles;
        public int MissionIndex;
    }

    [System.Serializable]
    public struct CheckpointInventoryItem
    {
        public string ItemId;
        public string DisplayName;
        public string Description;
        public int Quantity;
        public int Kind;
        public bool IsObjectiveItem;
    }

    [System.Serializable]
    public struct CheckpointEvidence
    {
        public string EvidenceId;
        public string DisplayName;
        public string Description;
        public string Hotspot;
        public string Fact;
        public string Dependency;
        public string Consequence;
        public int Type;
        public int Category;
    }

    [System.Serializable]
    public struct PuzzleSnapshot
    {
        public string PuzzleId;
        public PuzzleState State;
    }
}
