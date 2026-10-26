using System;
using System.Collections.Generic;
using Yumiwa.Domain;

namespace Yumiwa.Application
{
    public sealed class EvidenceLogService : IEvidenceLog
    {
        private readonly Dictionary<string, Evidence> evidenceById = new Dictionary<string, Evidence>();
        private readonly IEventBus bus;

        public EvidenceLogService(IEventBus bus = null)
        {
            this.bus = bus;
        }

        public bool HasEvidence(string evidenceId)
            => evidenceById.ContainsKey(evidenceId);

        public void AddEvidence(Evidence evidence)
        {
            if (!evidence.IsValid)
                return;
            evidenceById[evidence.EvidenceId] = evidence;
            bus?.Publish(new EvidenceAddedEvent(evidence.EvidenceId));
        }

        public void RemoveEvidence(string evidenceId)
        {
            if (string.IsNullOrEmpty(evidenceId))
                return;
            if (evidenceById.TryGetValue(evidenceId, out var existing))
            {
                // Critical evidence cannot be removed (anti-soft-lock)
                if (existing.Type != EvidenceType.Critical)
                    evidenceById.Remove(evidenceId);
            }
        }

        public IReadOnlyList<Evidence> GetAllEvidence()
        {
            var list = new List<Evidence>(evidenceById.Values);
            list.Sort((a, b) => string.Compare(a.DisplayName, b.DisplayName, StringComparison.Ordinal));
            return list;
        }

        public IReadOnlyList<Evidence> GetEvidenceByCategory(EvidenceCategory category)
        {
            var list = new List<Evidence>();
            foreach (var e in evidenceById.Values)
                if (e.Category == category)
                    list.Add(e);
            return list;
        }

        public IReadOnlyList<Evidence> GetCriticalEvidence()
        {
            var list = new List<Evidence>();
            foreach (var e in evidenceById.Values)
                if (e.Type == EvidenceType.Critical)
                    list.Add(e);
            return list;
        }

        public IReadOnlyList<Evidence> GetPhysicalEvidence()
        {
            var list = new List<Evidence>();
            foreach (var e in evidenceById.Values)
                if (e.Type == EvidenceType.Physical)
                    list.Add(e);
            return list;
        }

        public IReadOnlyList<Evidence> GetOptionalEvidence()
        {
            var list = new List<Evidence>();
            foreach (var e in evidenceById.Values)
                if (e.Type == EvidenceType.Optional)
                    list.Add(e);
            return list;
        }

        public void Clear()
        {
            evidenceById.Clear();
        }

        public void Import(Evidence[] evidences)
        {
            evidenceById.Clear();
            if (evidences == null) return;
            foreach (var e in evidences)
                if (e.IsValid) evidenceById[e.EvidenceId] = e;
        }
    }
}
