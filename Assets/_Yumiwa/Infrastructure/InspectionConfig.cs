using UnityEngine;
using Yumiwa.Domain;

namespace Yumiwa.Infrastructure
{
    /// <summary>ScriptableObject inspection tunables; defaults mirror pickup.json behavior.</summary>
    [CreateAssetMenu(menuName = "Yumiwa/InspectionConfig", fileName = "InspectionConfig")]
    public sealed class InspectionConfig : ScriptableObject
    {
        [Header("Rotation")]
        public float Smoothing = 12f;
        public float MinPitch = -60f;
        public float MaxPitch = 60f;

        [Header("Zoom (dolly distance in meters)")]
        public float MinDistance = 0.35f;
        public float MaxDistance = 2f;
        public float DefaultDistance = 0.9f;

        [Header("Device mapping")]
        public float MouseDegreesPerPixel = 0.35f;
        public float StickDegreesPerSecond = 200f;
        public float MouseZoomStep = 0.12f;
        public float PadZoomRate = 1.2f;

        [Header("Presentation")]
        [Tooltip("Inspection instance is moved to this layer when it exists; otherwise the prefab keeps its own layer.")]
        public string InspectionLayerName = "Inspection";

        public InspectionTuningData ToData() => new InspectionTuningData(
            Smoothing, MinPitch, MaxPitch,
            MinDistance, MaxDistance, DefaultDistance,
            MouseDegreesPerPixel, StickDegreesPerSecond, MouseZoomStep, PadZoomRate);
    }
}
