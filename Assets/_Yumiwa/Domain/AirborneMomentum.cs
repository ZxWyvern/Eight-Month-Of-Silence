namespace Yumiwa.Domain
{
    /// <summary>Pure airborne-momentum math: preserve run speed into jumps, steer without snapping.</summary>
    public static class AirborneMomentum
    {
        /// <summary>Takeoff speed for a jump height under constant gravity (both negative/positive handled).</summary>
        public static float JumpSpeed(float gravity, float height)
        {
            float g = gravity < 0f ? -gravity : gravity;
            return (float)System.Math.Sqrt(2f * g * height);
        }

        /// <summary>
        /// Air steering: add wish accel, apply tiny drag, never snap preserved momentum away.
        /// Operates on planar components so Domain stays UnityEngine-free.
        /// </summary>
        public static void ApplyAirControl(
            ref float currentX, ref float currentY,
            float wishX, float wishY,
            float accel, float drag, float deltaTime)
        {
            currentX += wishX * accel * deltaTime;
            currentY += wishY * accel * deltaTime;
            if (drag > 0f && deltaTime > 0f)
            {
                float keep = 1f - drag * deltaTime;
                if (keep < 0f)
                    keep = 0f;
                currentX *= keep;
                currentY *= keep;
            }
        }

        /// <summary>Whether preserved speed meets expectation (used by tests and tuning).</summary>
        public static bool PreservesSpeed(float before, float after, float tolerance = 0.05f) =>
            after + tolerance >= before;
    }
}
