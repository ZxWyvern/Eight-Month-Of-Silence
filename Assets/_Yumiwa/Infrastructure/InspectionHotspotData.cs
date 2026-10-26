using UnityEngine;
using Yumiwa.Domain;

namespace Yumiwa.Infrastructure
{
    [CreateAssetMenu(menuName = "Yumiwa/InspectionHotspot", fileName = "Hotspot")]
    public sealed class InspectionHotspotData : ScriptableObject
    {
        public string HotspotId = "hotspot_new";
        public string Description = "Hotspot";
        [TextArea(2,3)] public string Fact = "";
        [Header("Angle Gate (valid orientation range)")]
        public float YawMin = -180f;
        public float YawMax = 180f;
        public float PitchMin = -90f;
        public float PitchMax = 90f;
        [Header("Distance Gate (zoom)")]
        public float DistanceMin = 0f;
        public float DistanceMax = 10f;

        public InspectionHotspot ToHotspot() => new InspectionHotspot(
            HotspotId, Description, Fact, YawMin, YawMax, PitchMin, PitchMax, DistanceMin, DistanceMax);

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(HotspotId))
                Debug.LogWarning($"[Yumiwa] Hotspot '{name}' has empty id.", this);
        }
    }
}
