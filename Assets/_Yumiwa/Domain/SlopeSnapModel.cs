namespace Yumiwa.Domain
{
    /// <summary>Pure slope-stick decision: snap to ground only on walkable near slopes, never ledges.</summary>
    public static class SlopeSnapModel
    {
        /// <summary>Returns the vertical snap delta (negative or zero) when snapping is valid.</summary>
        public static bool TryComputeSnap(
            float feetY,
            float groundY,
            float slopeAngleDegrees,
            float slopeLimitDegrees,
            float verticalVelocity,
            float snapDistance,
            out float snapDeltaY)
        {
            snapDeltaY = 0f;
            if (verticalVelocity > 0.01f)
                return false;
            float delta = groundY - feetY;
            if (delta > 0.02f || delta < -snapDistance)
                return false;
            if (slopeAngleDegrees > slopeLimitDegrees)
                return false;
            snapDeltaY = delta;
            return true;
        }
    }
}
