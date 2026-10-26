namespace Yumiwa.Domain
{
    /// <summary>
    /// Persistence boundary per ARCHITECTURE.md §4 — implemented in Infrastructure.
    /// Separate stores per GDD §52: ChapterSave (Ch 1-2) vs Checkpoint (final house milestones).
    /// </summary>
    public interface ISaveRepository
    {
        bool HasChapterSave { get; }
        void SaveChapter(CheckpointData data);
        CheckpointData LoadChapter();
        void ClearChapter();

        bool HasHouseCheckpoint { get; }
        void SaveHouseCheckpoint(CheckpointData data, CheckpointMilestone milestone);
        CheckpointData LoadHouseCheckpoint(out CheckpointMilestone milestone);
        void ClearHouseCheckpoint();
        void ClearAll();
    }
}
