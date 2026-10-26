using System.Collections.Generic;
using Yumiwa.Domain;

namespace Yumiwa.Domain
{
    public interface IObjectiveService
    {
        ChapterState CurrentChapter { get; }
        ObjectiveStatus ChapterStatus { get; }
        IReadOnlyList<Objective> CurrentObjectives { get; }
        IReadOnlyList<Objective> CompletedObjectives { get; }

        void StartChapter(ChapterState chapter);
        void AddObjective(Objective objective);
        void UpdateObjectiveStatus(string objectiveId, ObjectiveStatus status);
        bool IsObjectiveActive(string objectiveId);
        bool IsObjectiveCompleted(string objectiveId);
        void CompleteChapter(ChapterState chapter);
    }
}