namespace Yumiwa.Domain
{
    /// <summary>
    /// Data-driven mission definition per ARCHITECTURE.md §7 + ADR-0003.
    /// Order is defined by MissionSequenceAsset, never enum switch.
    /// Prototype scope: vertical slice contains Bab 1 + segmen rumah Adrian.
    /// </summary>
    public readonly struct MissionDefinition
    {
        public readonly string MissionId;
        public readonly string DisplayName;
        public readonly string Description;
        public readonly ChapterState Chapter;
        public readonly int OrderIndex;

        public MissionDefinition(string missionId, string displayName, string description, ChapterState chapter, int orderIndex)
        {
            MissionId = missionId;
            DisplayName = displayName;
            Description = description;
            Chapter = chapter;
            OrderIndex = orderIndex;
        }

        public bool IsValid => !string.IsNullOrEmpty(MissionId);
    }
}
