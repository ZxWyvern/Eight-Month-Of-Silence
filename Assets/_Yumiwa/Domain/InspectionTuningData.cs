namespace Yumiwa.Domain
{
    /// <summary>Plain-data inspection tuning snapshot (no UnityEngine dependency).</summary>
    public readonly struct InspectionTuningData
    {
        public readonly float Smoothing;
        public readonly float MinPitch;
        public readonly float MaxPitch;
        public readonly float MinDistance;
        public readonly float MaxDistance;
        public readonly float DefaultDistance;
        public readonly float MouseDegreesPerPixel;
        public readonly float StickDegreesPerSecond;
        public readonly float MouseZoomStep;
        public readonly float PadZoomRate;

        public InspectionTuningData(
            float smoothing = 12f,
            float minPitch = -60f,
            float maxPitch = 60f,
            float minDistance = 0.35f,
            float maxDistance = 2f,
            float defaultDistance = 0.9f,
            float mouseDegreesPerPixel = 0.35f,
            float stickDegreesPerSecond = 200f,
            float mouseZoomStep = 0.12f,
            float padZoomRate = 1.2f)
        {
            Smoothing = smoothing;
            MinPitch = minPitch;
            MaxPitch = maxPitch;
            MinDistance = minDistance;
            MaxDistance = maxDistance;
            DefaultDistance = defaultDistance;
            MouseDegreesPerPixel = mouseDegreesPerPixel;
            StickDegreesPerSecond = stickDegreesPerSecond;
            MouseZoomStep = mouseZoomStep;
            PadZoomRate = padZoomRate;
        }

        public static InspectionTuningData Defaults() => new InspectionTuningData(12f, -60f, 60f, 0.35f, 2f, 0.9f, 0.35f, 200f, 0.12f, 1.2f);
    }
}
