using Yumiwa.Domain;

namespace Yumiwa.Application
{
    /// <summary>
    /// Bab 1 objective chain (B1-04, disederhanakan ADR-0030): masuk TKP →
    /// observasi/dokumentasi bukti → langsung selesai + fade ke scene berikut.
    /// Mekanik rekonstruksi dihapus dari alur: bukti lengkap (body + blood)
    /// langsung menyelesaikan chapter tanpa menunggu ReconstructionSolvedEvent.
    /// Event-driven over the typed bus; owns no Unity types.
    /// Idempotent: repeated triggers, re-added evidence and reloads never
    /// complete twice or advance past the chapter. Mission advance stops at the
    /// chapter boundary — STORY-01 owns the Bab 1 → Bab 2 scene transition.
    /// </summary>
    public sealed class Act1ObjectiveChain
    {
        private readonly IEventBus bus;
        private readonly IObjectiveService objectives;
        private readonly MissionFlowService mission;
        private readonly IEvidenceLog evidenceLog;
        private readonly Act1ChapterConfig config;
        private bool chapterCompleted;

        public Act1ObjectiveChain(
            IEventBus bus,
            IObjectiveService objectives,
            MissionFlowService mission,
            IEvidenceLog evidenceLog,
            Act1ChapterConfig config)
        {
            this.bus = bus;
            this.objectives = objectives;
            this.mission = mission;
            this.evidenceLog = evidenceLog;
            this.config = config;
            bus?.Subscribe<MissionChangedEvent>(OnMissionChanged);
            bus?.Subscribe<EvidenceAddedEvent>(OnEvidenceAdded);
            bus?.Subscribe<ReconstructionSolvedEvent>(OnReconstructionSolved);
        }

        public bool ChapterCompleted => chapterCompleted;

        /// <summary>Called by the crime-scene entry trigger (Presentation glue).</summary>
        public void OnEnterCrimeScene()
        {
            if (!config.IsValid || objectives == null)
                return;
            if (objectives.IsObjectiveActive(config.EnterObjectiveId))
                objectives.UpdateObjectiveStatus(config.EnterObjectiveId, ObjectiveStatus.Completed);
        }

        private void OnMissionChanged(MissionChangedEvent e)
        {
            if (!config.IsValid || e.MissionId != config.MissionId)
                return;
            EnsureSetup();
        }

        private void EnsureSetup()
        {
            objectives.StartChapter(config.Chapter);
            EnsureObjective(config.EnterObjectiveId, config.EnterDescription);
            EnsureObjective(config.ObserveObjectiveId, config.ObserveDescription);
            EnsureObjective(config.DeduceObjectiveId, config.DeduceDescription);
            // Reload derivation: evidence import runs before mission import in
            // LoadChapter, so a resumed chapter re-completes from the log without
            // advancing the mission or publishing completion twice.
            DeriveFromLog(publishCompletion: false);
        }

        private void EnsureObjective(string id, string description)
        {
            if (objectives.IsObjectiveActive(id) || objectives.IsObjectiveCompleted(id))
                return;
            objectives.AddObjective(new Objective(id, description, ObjectiveStatus.Active, config.Chapter));
        }

        private void OnEvidenceAdded(EvidenceAddedEvent e)
        {
            if (!config.IsValid || chapterCompleted)
                return;
            if (!objectives.IsObjectiveActive(config.ObserveObjectiveId))
                return;
            if (!AllEvidencePresent())
                return;
            objectives.UpdateObjectiveStatus(config.ObserveObjectiveId, ObjectiveStatus.Completed);
            // ADR-0030: rekonstruksi dihapus — bukti lengkap langsung selesaikan chapter.
            CompleteChapterNow();
        }

        private void OnReconstructionSolved(ReconstructionSolvedEvent e)
        {
            // ADR-0030: fallback jika board masih terbuka di scene lama — abaikan bila
            // chapter sudah selesai via bukti; selain itu pertahankan perilaku lama.
            if (!config.IsValid || chapterCompleted || e.CaseId != config.CaseId)
                return;
            CompleteChapterNow();
        }

        private void CompleteChapterNow()
        {
            if (objectives.IsObjectiveActive(config.DeduceObjectiveId))
                objectives.UpdateObjectiveStatus(config.DeduceObjectiveId, ObjectiveStatus.Completed);
            chapterCompleted = true;
            objectives.CompleteChapter(config.Chapter);
            bus?.Publish(new Act1ChapterCompletedEvent(config.MissionId));
        }

        private void DeriveFromLog(bool publishCompletion)
        {
            if (AllEvidencePresent() && objectives.IsObjectiveActive(config.ObserveObjectiveId))
                objectives.UpdateObjectiveStatus(config.ObserveObjectiveId, ObjectiveStatus.Completed);
            // ADR-0030: tanpa rekonstruksi, bukti lengkap saja cukup untuk deduce.
            if (AllEvidencePresent()
                && objectives.IsObjectiveActive(config.DeduceObjectiveId))
            {
                objectives.UpdateObjectiveStatus(config.DeduceObjectiveId, ObjectiveStatus.Completed);
                chapterCompleted = true;
                objectives.CompleteChapter(config.Chapter);
                if (publishCompletion)
                    bus?.Publish(new Act1ChapterCompletedEvent(config.MissionId));
                return;
            }
            if (evidenceLog != null && !string.IsNullOrEmpty(config.DeductionEvidenceId)
                && evidenceLog.HasEvidence(config.DeductionEvidenceId))
            {
                if (objectives.IsObjectiveActive(config.DeduceObjectiveId))
                    objectives.UpdateObjectiveStatus(config.DeduceObjectiveId, ObjectiveStatus.Completed);
                chapterCompleted = true;
                objectives.CompleteChapter(config.Chapter);
                if (publishCompletion)
                    bus?.Publish(new Act1ChapterCompletedEvent(config.MissionId));
            }
        }

        private bool AllEvidencePresent()
        {
            if (evidenceLog == null)
                return false;
            for (int i = 0; i < config.EvidenceIds.Length; i++)
                if (!evidenceLog.HasEvidence(config.EvidenceIds[i]))
                    return false;
            return true;
        }
    }

    public readonly struct Act1ChapterCompletedEvent
    {
        public readonly string MissionId;
        public Act1ChapterCompletedEvent(string missionId)
        {
            MissionId = missionId;
        }
    }
}
