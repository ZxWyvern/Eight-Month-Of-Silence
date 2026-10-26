namespace Yumiwa.Domain
{
    /// <summary>
    /// Pure hotspot definition per GDD §03 — angle-gated examinable detail.
    /// Every evidence/inspectable has 0..N hotspots; hotspot discovery drives
    /// EvidenceLog updates. Discovery is valid at angle/distance, survives Cancel.
    /// </summary>
    public readonly struct InspectionHotspot
    {
        public readonly string HotspotId;
        public readonly string Description;
        public readonly string Fact;
        public readonly float YawMin;
        public readonly float YawMax;
        public readonly float PitchMin;
        public readonly float PitchMax;
        public readonly float DistanceMin;
        public readonly float DistanceMax;

        public InspectionHotspot(
            string hotspotId,
            string description,
            string fact,
            float yawMin = -180f, float yawMax = 180f,
            float pitchMin = -90f, float pitchMax = 90f,
            float distanceMin = 0f, float distanceMax = 10f)
        {
            HotspotId = hotspotId;
            Description = description;
            Fact = fact;
            YawMin = yawMin;
            YawMax = yawMax;
            PitchMin = pitchMin;
            PitchMax = pitchMax;
            DistanceMin = distanceMin;
            DistanceMax = distanceMax;
        }

        public bool IsValid => !string.IsNullOrEmpty(HotspotId);

        public bool IsAngleValid(float yaw, float pitch, float distance)
        {
            float nYaw = NormalizeAngle(yaw);
            bool yawOk;
            if (YawMin <= YawMax)
                yawOk = nYaw >= YawMin && nYaw <= YawMax;
            else
                yawOk = nYaw >= YawMin || nYaw <= YawMax; // wrap case e.g. 170..-170
            return yawOk
                && pitch >= PitchMin && pitch <= PitchMax
                && distance >= DistanceMin && distance <= DistanceMax;
        }

        private static float NormalizeAngle(float a)
        {
            while (a > 180f) a -= 360f;
            while (a < -180f) a += 360f;
            return a;
        }
    }
}
