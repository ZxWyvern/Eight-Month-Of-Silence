using UnityEngine;
using Yumiwa.Domain;

namespace Yumiwa.Infrastructure
{
    /// <summary>
    /// Bab 1 chapter content (B1-04): mission, objectives and evidence wiring as
    /// data (ADR-0003), never as hardcoded id switches in the chain service.
    /// </summary>
    [CreateAssetMenu(menuName = "Yumiwa/Act1Chapter", fileName = "Act1Chapter")]
    public sealed class Act1DefinitionAsset : ScriptableObject
    {
        public string MissionId = "M01_Alley";
        public ChapterState Chapter = ChapterState.Chapter1_BloodTrail;
        public string EnterObjectiveId = "b1_enter";
        [TextArea(1, 2)] public string EnterDescription = "Masuki TKP di gang.";
        public string ObserveObjectiveId = "b1_observe";
        [TextArea(1, 2)] public string ObserveDescription = "Dokumentasikan 3 bukti: putar, zoom, examine.";
        public string DeduceObjectiveId = "b1_deduce";
        [TextArea(1, 2)] public string DeduceDescription = "Susun rekonstruksi kejadian.";
        [Tooltip("Evidence ids (1:1 with inspectable item ids) required for observasi + deduksi.")]
        public string[] EvidenceIds = new string[0];
        public string DeductionEvidenceId = "ev_b1_deduction";
        public string CaseId = "case_b1";

        public Act1ChapterConfig ToConfig() => new Act1ChapterConfig(
            MissionId, Chapter,
            EnterObjectiveId, EnterDescription,
            ObserveObjectiveId, ObserveDescription,
            DeduceObjectiveId, DeduceDescription,
            EvidenceIds, DeductionEvidenceId, CaseId);

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(MissionId))
                Debug.LogWarning($"[Yumiwa] Act1Definition '{name}' has an empty MissionId.", this);
            if (EvidenceIds == null || EvidenceIds.Length == 0)
                Debug.LogWarning($"[Yumiwa] Act1Definition '{name}' lists no evidence.", this);
        }
    }
}
