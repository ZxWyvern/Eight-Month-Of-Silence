namespace Yumiwa.Domain
{
    /// <summary>
    /// Final-house checkpoint milestones per GDD §52 / GDD.md §8.
    /// Chapters 1-2 use separate ChapterSave, not these milestones.
    /// </summary>
    public enum CheckpointMilestone
    {
        None = 0,
        DisplayCaseKey = 1,
        Shovel = 2,
        BasementAccess = 3
    }
}
