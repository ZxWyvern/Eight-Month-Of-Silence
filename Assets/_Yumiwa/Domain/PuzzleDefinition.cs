namespace Yumiwa.Domain
{
    /// <summary>
    /// Pure puzzle definition per GDD §03 Puzzle (Environmental, Evidence-based, Combination, Trap Observation).
    /// Puzzle is solvable from clues available, never pixel-hunting; data-driven via ScriptableObject.
    /// </summary>
    public readonly struct PuzzleDefinition
    {
        public readonly string PuzzleId;
        public readonly string DisplayName;
        public readonly string Description;
        public readonly string RequiredEvidenceId;
        public readonly string RequiredItemId;
        public readonly string SolutionCode;

        public PuzzleDefinition(string puzzleId, string displayName, string description, string requiredEvidenceId, string requiredItemId, string solutionCode)
        {
            PuzzleId = puzzleId;
            DisplayName = displayName;
            Description = description;
            RequiredEvidenceId = requiredEvidenceId;
            RequiredItemId = requiredItemId;
            SolutionCode = solutionCode;
        }

        public bool IsValid => !string.IsNullOrEmpty(PuzzleId);
    }

    public enum PuzzleState
    {
        Locked,
        Solvable,
        Solved,
        Failed
    }
}
