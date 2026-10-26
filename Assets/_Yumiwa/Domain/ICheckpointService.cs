using Yumiwa.Domain;

namespace Yumiwa.Domain
{
    public interface ICheckpointService
    {
        void SaveCheckpoint(CheckpointData data);
        CheckpointData LoadCheckpoint();
        bool HasCheckpoint { get; }
        void ClearCheckpoint();

        // GDD §52 chapter saves (Ch 1-2, non-checkpoint) live on the same
        // service so presenters never touch ISaveRepository directly.
        bool HasChapterSave { get; }
        void SaveChapter(CheckpointData data);
        CheckpointData LoadChapter();
        void ClearChapter();
    }
}
