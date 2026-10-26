using UnityEngine;
using Yumiwa.Domain;

namespace Yumiwa.Infrastructure
{
    [CreateAssetMenu(menuName = "Yumiwa/Puzzle/Puzzle Definition", fileName = "Puzzle")]
    public sealed class PuzzleDefinitionAsset : ScriptableObject
    {
        public string PuzzleId = "puzzle_new";
        public string DisplayName = "New Puzzle";
        [TextArea(2,3)] public string Description = "";
        public string RequiredEvidenceId = "";
        public string RequiredItemId = "";
        public string SolutionCode = "0000";

        public PuzzleDefinition ToDefinition() => new PuzzleDefinition(PuzzleId, DisplayName, Description, RequiredEvidenceId, RequiredItemId, SolutionCode);

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(PuzzleId))
                Debug.LogWarning($"[Yumiwa] Puzzle '{name}' has empty id.", this);
        }
    }
}
