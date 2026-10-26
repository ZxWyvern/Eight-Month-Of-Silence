using UnityEngine;
using Yumiwa.Domain;

namespace Yumiwa.Infrastructure
{
    [CreateAssetMenu(menuName = "Yumiwa/EvidenceData", fileName = "EvidenceData")]
    public sealed class EvidenceData : ScriptableObject
    {
        public string EvidenceId = "evidence_new";
        public string DisplayName = "New Evidence";
        [TextArea(2, 4)] public string Description = "";
        public string Hotspot = "";
        [TextArea(2, 4)] public string Fact = "";
        public string Dependency = "";
        [TextArea(2, 4)] public string Consequence = "";
        public EvidenceType Type = EvidenceType.Critical;
        public EvidenceCategory Category = EvidenceCategory.Fact;

        public Evidence ToEvidence() => new Evidence(
            EvidenceId, DisplayName, Description, Hotspot, Fact, Dependency, Consequence, Type, Category);

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(EvidenceId))
                Debug.LogWarning($"[Yumiwa] EvidenceData '{name}' has an empty EvidenceId.", this);
        }
    }
}