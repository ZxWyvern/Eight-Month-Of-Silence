namespace Yumiwa.Domain
{
    /// <summary>
    /// Pure inspection orientation model: yaw/pitch targets with exponential
    /// smoothing plus a clamped dolly distance. Callers pass pre-scaled degree
    /// and meter deltas (device mapping lives in presentation); Tick advances
    /// the smoothing. All math stays unit-testable here.
    /// </summary>
    public sealed class InspectionRotationModel
    {
        private readonly InspectionTuningData tuning;

        private float targetYaw;
        private float targetPitch;
        private float targetDistance;
        private float yaw;
        private float pitch;
        private float distance;

        public InspectionRotationModel(InspectionTuningData tuning)
        {
            this.tuning = tuning;
            targetDistance = Clamp(tuning.DefaultDistance, tuning.MinDistance, tuning.MaxDistance);
            distance = targetDistance;
        }

        public float Yaw => yaw;
        public float Pitch => pitch;
        public float Distance => distance;

        public void ApplyRotateDelta(float yawDeltaDegrees, float pitchDeltaDegrees)
        {
            targetYaw += yawDeltaDegrees;
            targetYaw = NormalizeAngle(targetYaw); // P1-M6 prevent unbounded drift (was 360*N after N turns, undiscoverable)
            targetPitch = Clamp(targetPitch + pitchDeltaDegrees, tuning.MinPitch, tuning.MaxPitch);
        }

        private static float NormalizeAngle(float a)
        {
            while (a > 180f) a -= 360f;
            while (a < -180f) a += 360f;
            return a;
        }

        private static float DeltaAngle(float from, float to)
        {
            return NormalizeAngle(to - from);
        }

        public void ApplyZoomDelta(float distanceDelta)
        {
            targetDistance = Clamp(targetDistance + distanceDelta, tuning.MinDistance, tuning.MaxDistance);
        }

        public void Tick(float deltaTime)
        {
            if (deltaTime <= 0f)
                return;
            float t = 1f - (float)System.Math.Exp(-tuning.Smoothing * deltaTime);
            float yawDelta = DeltaAngle(yaw, targetYaw);
            yaw += yawDelta * t;
            yaw = NormalizeAngle(yaw);
            pitch += (targetPitch - pitch) * t;
            distance += (targetDistance - distance) * t;
        }

        public void Reset()
        {
            targetYaw = 0f;
            targetPitch = 0f;
            yaw = 0f;
            pitch = 0f;
            targetDistance = Clamp(tuning.DefaultDistance, tuning.MinDistance, tuning.MaxDistance);
            distance = targetDistance;
        }

        private static float Clamp(float value, float min, float max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }
    }
}
