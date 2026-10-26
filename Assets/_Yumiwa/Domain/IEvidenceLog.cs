using System.Collections.Generic;
using Yumiwa.Domain;

namespace Yumiwa.Domain
{
    public interface IEvidenceLog
    {
        bool HasEvidence(string evidenceId);
        void AddEvidence(Evidence evidence);
        void RemoveEvidence(string evidenceId);
        IReadOnlyList<Evidence> GetAllEvidence();
        IReadOnlyList<Evidence> GetEvidenceByCategory(EvidenceCategory category);
        IReadOnlyList<Evidence> GetCriticalEvidence();
        IReadOnlyList<Evidence> GetPhysicalEvidence();
        IReadOnlyList<Evidence> GetOptionalEvidence();
        void Clear();
        void Import(Evidence[] evidences);
    }
}