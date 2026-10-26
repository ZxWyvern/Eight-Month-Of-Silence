namespace Yumiwa.Domain
{
    /// <summary>
    /// Data for one investigation chapter's objective chain (B1-04). Authored in
    /// Act1DefinitionAsset; consumed by Act1ObjectiveChain. Ids are content
    /// identifiers, not tuning numbers.
    /// </summary>
    public readonly struct Act1ChapterConfig
    {
        public readonly string MissionId;
        public readonly ChapterState Chapter;
        public readonly string EnterObjectiveId;
        public readonly string EnterDescription;
        public readonly string ObserveObjectiveId;
        public readonly string ObserveDescription;
        public readonly string DeduceObjectiveId;
        public readonly string DeduceDescription;
        public readonly string[] EvidenceIds;
        public readonly string DeductionEvidenceId;
        public readonly string CaseId;

        public Act1ChapterConfig(
            string missionId,
            ChapterState chapter,
            string enterObjectiveId,
            string enterDescription,
            string observeObjectiveId,
            string observeDescription,
            string deduceObjectiveId,
            string deduceDescription,
            string[] evidenceIds,
            string deductionEvidenceId,
            string caseId)
        {
            MissionId = missionId;
            Chapter = chapter;
            EnterObjectiveId = enterObjectiveId;
            EnterDescription = enterDescription;
            ObserveObjectiveId = observeObjectiveId;
            ObserveDescription = observeDescription;
            DeduceObjectiveId = deduceObjectiveId;
            DeduceDescription = deduceDescription;
            EvidenceIds = evidenceIds ?? System.Array.Empty<string>();
            DeductionEvidenceId = deductionEvidenceId;
            CaseId = caseId;
        }

        public bool IsValid =>
            !string.IsNullOrEmpty(MissionId)
            && !string.IsNullOrEmpty(EnterObjectiveId)
            && !string.IsNullOrEmpty(ObserveObjectiveId)
            && !string.IsNullOrEmpty(DeduceObjectiveId)
            && EvidenceIds.Length > 0
            && !string.IsNullOrEmpty(DeductionEvidenceId)
            && !string.IsNullOrEmpty(CaseId);
    }
}
