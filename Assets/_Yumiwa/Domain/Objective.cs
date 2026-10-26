namespace Yumiwa.Domain
{
    public readonly struct Objective
    {
        public readonly string ObjectiveId;
        public readonly string Description;
        public readonly ObjectiveStatus Status;
        public readonly ChapterState Chapter;

        public Objective(string objectiveId, string description, ObjectiveStatus status, ChapterState chapter)
        {
            ObjectiveId = objectiveId;
            Description = description;
            Status = status;
            Chapter = chapter;
        }

        public bool IsValid => !string.IsNullOrEmpty(ObjectiveId);
    }
}