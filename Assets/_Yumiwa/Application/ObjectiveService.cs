using System.Collections.Generic;
using Yumiwa.Domain;

namespace Yumiwa.Application
{
    public sealed class ObjectiveService : IObjectiveService
    {
        private readonly List<Objective> activeObjectives = new List<Objective>();
        private readonly List<Objective> completedObjectives = new List<Objective>();

        public ChapterState CurrentChapter { get; private set; } = ChapterState.None;
        public ObjectiveStatus ChapterStatus { get; private set; } = ObjectiveStatus.Inactive;

        public IReadOnlyList<Objective> CurrentObjectives => activeObjectives;
        public IReadOnlyList<Objective> CompletedObjectives => completedObjectives;

        public void StartChapter(ChapterState chapter)
        {
            CurrentChapter = chapter;
            ChapterStatus = ObjectiveStatus.Active;
            activeObjectives.Clear();
        }

        public void AddObjective(Objective objective)
        {
            if (!objective.IsValid)
                return;
            if (activeObjectives.Exists(o => o.ObjectiveId == objective.ObjectiveId))
                return;
            activeObjectives.Add(objective);
        }

        public void UpdateObjectiveStatus(string objectiveId, ObjectiveStatus status)
        {
            if (string.IsNullOrEmpty(objectiveId))
                return;

            int idx = activeObjectives.FindIndex(o => o.ObjectiveId == objectiveId);
            if (idx >= 0)
            {
                var obj = activeObjectives[idx];
                var updated = new Objective(obj.ObjectiveId, obj.Description, status, obj.Chapter);
                activeObjectives[idx] = updated;

                if (status == ObjectiveStatus.Completed)
                {
                    completedObjectives.Add(updated);
                    activeObjectives.RemoveAt(idx);
                }
            }
        }

        public bool IsObjectiveActive(string objectiveId)
            => activeObjectives.Exists(o => o.ObjectiveId == objectiveId);

        public bool IsObjectiveCompleted(string objectiveId)
            => completedObjectives.Exists(o => o.ObjectiveId == objectiveId);

        public void CompleteChapter(ChapterState chapter)
        {
            if (CurrentChapter == chapter)
            {
                ChapterStatus = ObjectiveStatus.Completed;
                activeObjectives.Clear();
            }
        }
    }
}