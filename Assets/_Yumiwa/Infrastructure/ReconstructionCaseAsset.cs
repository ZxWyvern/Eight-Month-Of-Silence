using UnityEngine;
using Yumiwa.Domain;

namespace Yumiwa.Infrastructure
{
    /// <summary>
    /// Data-driven deduction case (B1-03). Designers author options and the
    /// deduction fact; code never hardcodes conclusions (DATA-001).
    /// </summary>
    [CreateAssetMenu(menuName = "Yumiwa/ReconstructionCase", fileName = "ReconstructionCase")]
    public sealed class ReconstructionCaseAsset : ScriptableObject
    {
        public string CaseId = "case_new";
        public string Title = "New Case";
        [Tooltip("Evidence ids that must be logged AND examined (1:1 with inspectable item ids).")]
        public string[] RequiredEvidenceIds = new string[0];
        [Tooltip("Min examined hotspots per required evidence (B1-02 documentation gate).")]
        public int MinHotspotsPerEvidence = 1;
        [Tooltip("Must match OptionTexts length.")]
        public string[] OptionIds = new string[0];
        [TextArea(1, 2)] public string[] OptionTexts = new string[0];
        public string CorrectOptionId = "";
        [Header("Deduction fact logged on a correct conclusion")]
        public EvidenceData Deduction;

        public ReconstructionCase ToCase() => new ReconstructionCase(
            CaseId, Title, RequiredEvidenceIds, MinHotspotsPerEvidence,
            OptionIds, OptionTexts, CorrectOptionId,
            Deduction != null ? Deduction.ToEvidence() : default);

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(CaseId))
                Debug.LogWarning($"[Yumiwa] ReconstructionCase '{name}' has an empty CaseId.", this);
            if (OptionIds != null && OptionTexts != null && OptionIds.Length != OptionTexts.Length)
                Debug.LogWarning($"[Yumiwa] ReconstructionCase '{name}' option ids/texts length mismatch.", this);
            if (Deduction == null)
                Debug.LogWarning($"[Yumiwa] ReconstructionCase '{name}' has no Deduction evidence.", this);
        }
    }
}
