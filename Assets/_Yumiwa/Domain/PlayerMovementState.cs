namespace Yumiwa.Domain
{
    /// <summary>Pure movement-state entity: transition validation plus state timing.</summary>
    public sealed class PlayerMovementState
    {
        public MovementState Current { get; private set; } = MovementState.Idle;
        public float StateTime { get; private set; }
        public bool Grounded { get; private set; } = true;

        public bool CanEnter(MovementState next, float stamina, bool hasAmmo)
        {
            return CanEnter(next, stamina, hasAmmo, default, hasJumpObstacle: true);
        }

        /// <summary>Data-driven overload — single source of stamina costs via cfg when WalkSpeed&gt;0; LimitedJump gate via hasJumpObstacle (ADR P1-M4).</summary>
        public bool CanEnter(MovementState next, float stamina, bool hasAmmo, MovementConfigData cfg, bool hasJumpObstacle = true)
        {
            if (next == MovementState.Idle)
                return Current != MovementState.Idle;
            MovementStateData data;
            bool hasCfg = cfg.WalkSpeed > 0f;
            bool got = hasCfg ? MovementStateTable.TryGet(next, cfg, out data) : MovementStateTable.TryGet(next, out data);
            if (!got || stamina < data.StaminaCost)
                return false;
            switch (next)
            {
                case MovementState.Walk:
                    return Current == MovementState.Idle || Current == MovementState.Crouch || Current == MovementState.Sprint;
                case MovementState.Sprint:
                    return (Current == MovementState.Walk || Current == MovementState.Idle) && Grounded;
                case MovementState.Crouch:
                    return Grounded && Current != MovementState.Crouch;
                case MovementState.Jump:
                    if (hasCfg && cfg.LimitedJumpOnlyAtObstacle && !hasJumpObstacle)
                        return false;
                    return Grounded && Current != MovementState.Jump;
                default:
                    return false;
            }
        }

        public void Enter(MovementState next)
        {
            Current = next;
            StateTime = 0f;
        }

        public void SetGrounded(bool grounded)
        {
            Grounded = grounded;
            if (grounded && Current == MovementState.Jump)
                Current = MovementState.Idle;
        }

        public void Tick(float deltaTime) => StateTime += deltaTime;
    }
}
