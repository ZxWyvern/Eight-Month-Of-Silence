using System.Collections.Generic;
using Yumiwa.Domain;

namespace Yumiwa.Application
{
    /// <summary>
    /// Deduction service per docs/03 (rekonstruksi, B1-03): the player concludes
    /// from documented evidence. Wrong conclusion = logical feedback + retry with
    /// no progress loss; correct conclusion logs the deduction fact exactly once
    /// (which also persists it through the chapter save) and publishes solve.
    /// Pure Application; content is data (ReconstructionCaseAsset).
    /// </summary>
    public sealed class ReconstructionService
    {
        private readonly Dictionary<string, ReconstructionCase> cases = new Dictionary<string, ReconstructionCase>();
        private readonly Dictionary<string, bool> solved = new Dictionary<string, bool>();
        private readonly IEvidenceLog evidenceLog;
        private readonly InspectionSessionService inspection;
        private readonly IEventBus bus;

        public ReconstructionService(
            IEnumerable<ReconstructionCase> definitions,
            IEvidenceLog evidenceLog,
            InspectionSessionService inspection,
            IEventBus bus)
        {
            this.evidenceLog = evidenceLog;
            this.inspection = inspection;
            this.bus = bus;
            if (definitions == null)
                return;
            foreach (var c in definitions)
            {
                if (!c.IsValid || cases.ContainsKey(c.CaseId))
                    continue;
                cases[c.CaseId] = c;
                solved[c.CaseId] = false;
            }
        }

        public bool IsSolved(string caseId) =>
            !string.IsNullOrEmpty(caseId) && solved.TryGetValue(caseId, out bool s) && s;

        public bool HasCase(string caseId) =>
            !string.IsNullOrEmpty(caseId) && cases.ContainsKey(caseId);

        public bool TryGetCase(string caseId, out ReconstructionCase c)
        {
            c = default;
            if (string.IsNullOrEmpty(caseId))
                return false;
            return cases.TryGetValue(caseId, out c);
        }

        /// <summary>
        /// GDD B1-02 documentation gate: every required evidence id must be logged
        /// AND examined (min hotspots discovered on the same id). Bab 1 content
        /// maps evidence ids to inspectable item ids 1:1, so the inspection count
        /// is read with the evidence id directly.
        /// </summary>
        public bool CanAttempt(string caseId, out string[] missingEvidenceIds)
        {
            missingEvidenceIds = System.Array.Empty<string>();
            if (!cases.TryGetValue(caseId, out var c) || IsSolved(caseId))
                return false;
            if (evidenceLog == null)
                return false;
            var missing = new List<string>();
            for (int i = 0; i < c.RequiredEvidenceIds.Length; i++)
            {
                string id = c.RequiredEvidenceIds[i];
                if (!evidenceLog.HasEvidence(id))
                {
                    missing.Add(id);
                    continue;
                }
                if (inspection != null && c.MinHotspotsPerEvidence > 0
                    && inspection.GetDiscoveredHotspotCount(id) < c.MinHotspotsPerEvidence)
                    missing.Add(id);
            }
            missingEvidenceIds = missing.ToArray();
            return missing.Count == 0;
        }

        public bool TryConclude(string caseId, string optionId, out string feedback)
        {
            feedback = null;
            if (!cases.TryGetValue(caseId, out var c))
            {
                feedback = "Kasus tidak ditemukan.";
                return false;
            }
            if (IsSolved(caseId))
            {
                feedback = "Deduksi sudah tercatat.";
                return false;
            }
            if (!c.HasOption(optionId))
            {
                feedback = "Pilihan tidak valid.";
                return false;
            }
            if (!CanAttempt(caseId, out var missing))
            {
                feedback = missing.Length > 0
                    ? "Bukti belum lengkap — dokumentasikan dulu: " + string.Join(", ", missing) + "."
                    : "Syarat deduksi belum terpenuhi — periksa Evidence Log.";
                return false;
            }
            if (c.CorrectOptionId == optionId)
            {
                solved[caseId] = true;
                evidenceLog?.AddEvidence(c.Deduction);
                bus?.Publish(new ReconstructionSolvedEvent(caseId, optionId));
                feedback = "Benar — deduksi tercatat di Evidence Log.";
                return true;
            }
            bus?.Publish(new ReconstructionFailedEvent(caseId, optionId));
            feedback = "Kurang tepat — tinjau kembali bukti dan coba lagi. Progres tidak hilang.";
            return false;
        }
    }

    public readonly struct ReconstructionSolvedEvent
    {
        public readonly string CaseId;
        public readonly string OptionId;
        public ReconstructionSolvedEvent(string caseId, string optionId)
        {
            CaseId = caseId;
            OptionId = optionId;
        }
    }

    public readonly struct ReconstructionFailedEvent
    {
        public readonly string CaseId;
        public readonly string OptionId;
        public ReconstructionFailedEvent(string caseId, string optionId)
        {
            CaseId = caseId;
            OptionId = optionId;
        }
    }

    public readonly struct ReconstructionOpenedEvent
    {
        public readonly string CaseId;
        public ReconstructionOpenedEvent(string caseId)
        {
            CaseId = caseId;
        }
    }

    public readonly struct ReconstructionClosedEvent
    {
        public readonly string CaseId;
        public ReconstructionClosedEvent(string caseId)
        {
            CaseId = caseId;
        }
    }
}
