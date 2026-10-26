using Yumiwa.Domain;

namespace Yumiwa.Application
{
    /// <summary>
    /// Pure footstep cadence per docs/03 + docs/08 (MVP-01): step interval follows
    /// movement speed and grounded state. Surface identification is supplied by
    /// Presentation (raycast); audio playback and subtitle text stay in the view.
    /// Zero allocation per tick.
    /// </summary>
    public sealed class FootstepService
    {
        public float WalkInterval = 0.45f;
        public float SprintInterval = 0.32f;
        public float CrouchInterval = 0.62f;
        public float MinMoveSpeed = 0.8f;

        private float timer;

        /// <summary>Returns true exactly on a step beat. Never allocates.</summary>
        public bool Tick(float dt, float speed, bool grounded, bool sprinting, bool crouching)
        {
            if (dt <= 0f || !grounded || speed < MinMoveSpeed)
            {
                timer = 0f;
                return false;
            }
            float interval = crouching ? CrouchInterval : (sprinting ? SprintInterval : WalkInterval);
            timer -= dt;
            if (timer <= 0f)
            {
                timer = interval;
                return true;
            }
            return false;
        }

        public void Reset() => timer = 0f;
    }

    public readonly struct FootstepEvent
    {
        public readonly string SurfaceId;
        public readonly bool Sprinting;
        public FootstepEvent(string surfaceId, bool sprinting)
        {
            SurfaceId = surfaceId;
            Sprinting = sprinting;
        }
    }
}
