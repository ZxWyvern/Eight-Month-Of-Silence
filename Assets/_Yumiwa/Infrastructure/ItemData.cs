using UnityEngine;
using Yumiwa.Domain;

namespace Yumiwa.Infrastructure
{
    /// <summary>
    /// Authored item record. One asset per item type; world items and the
    /// inspection presenter both reference it, so no per-item code is needed.
    /// </summary>
    [CreateAssetMenu(menuName = "Yumiwa/ItemData", fileName = "ItemData")]
    public sealed class ItemData : ScriptableObject
    {
        public string ItemId = "item_new";
        public string DisplayName = "New Item";
        [TextArea(2, 4)] public string Description = "";
        public Sprite Icon;
        [Tooltip("Dedicated close-up model spawned by the inspection presenter. The world item is never moved.")]
        public GameObject InspectionPrefab;
        public int Quantity = 1;
        [Header("GDD Routing")]
        public ItemKind Kind = ItemKind.PhysicalTool;
        public bool IsObjectiveItem = false;
        [Tooltip("If this item carries evidence (e.g., bloody watch), link EvidenceData here. Critical evidence routes to log, not slots.")]
        public EvidenceData LinkedEvidence;
        [Tooltip("Optional hotspots for inspection — each requires hotspot angle check before discovery.")]
        public InspectionHotspotData[] Hotspots;

        public ItemDefinition ToDefinition() =>
            new ItemDefinition(ItemId, DisplayName, Description, Quantity > 0 ? Quantity : 1, Kind, IsObjectiveItem);

        public Evidence ToEvidence() => LinkedEvidence != null ? LinkedEvidence.ToEvidence() : default;

        public InspectionHotspot[] ToHotspots()
        {
            if (Hotspots == null || Hotspots.Length == 0)
                return System.Array.Empty<InspectionHotspot>();
            var arr = new InspectionHotspot[Hotspots.Length];
            for (int i = 0; i < Hotspots.Length; i++)
                arr[i] = Hotspots[i] != null ? Hotspots[i].ToHotspot() : default;
            return arr;
        }

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(ItemId))
                Debug.LogWarning($"[Yumiwa] ItemData '{name}' has an empty ItemId.", this);
            if (InspectionPrefab == null)
                Debug.LogWarning($"[Yumiwa] ItemData '{DisplayName}' has no InspectionPrefab; inspection shows text only.", this);
        }
    }
}
