using Yumiwa.Domain;

namespace Yumiwa.Application
{
    /// <summary>Application state machine: rule checks, stamina spend, landing exits.</summary>
    public sealed class MovementStateMachine : IMovementStateMachine
    {
        private readonly PlayerMovementState entity;
        private readonly IStaminaService stamina;
        private readonly IAmmoReadModel ammo;
        private readonly IEventBus bus;
        private readonly MovementConfigData movementCfg;

        public MovementStateMachine(
            PlayerMovementState entity,
            IStaminaService stamina,
            IAmmoReadModel ammo,
            IEventBus bus,
            MovementConfigData movementCfg = default)
        {
            this.entity = entity;
            this.stamina = stamina;
            this.ammo = ammo;
            this.bus = bus;
            this.movementCfg = movementCfg;
        }

        public MovementState Current => entity.Current;
        public float StateTime => entity.StateTime;
        public bool Grounded => entity.Grounded;

        public bool RequestEnter(MovementState next) => RequestEnter(next, hasJumpObstacle: true);

        /// <summary>Single-source stamina: cfg WalkSpeed&gt;0 uses MovementConfigData costs, else legacy fallback. Jump obstacle gate via hasJumpObstacle (P1-M4).</summary>
        public bool RequestEnter(MovementState next, bool hasJumpObstacle)
        {
            if (Current == next)
                return true;
            bool hasCfg = movementCfg.WalkSpeed > 0f;
            MovementStateData data;
            bool got = hasCfg ? MovementStateTable.TryGet(next, movementCfg, out data) : MovementStateTable.TryGet(next, out data);
            if (!got)
                return false;
            if (stamina.Current < data.StaminaCost)
                return false;
            if (!entity.CanEnter(next, stamina.Current, ammo.HasAmmo, movementCfg, hasJumpObstacle))
                return false;
            float cost = data.StaminaCost;
            if (cost > 0f && !stamina.TrySpend(cost))
                return false;
            TransitionTo(next);
            return true;
        }

        public void RequestExit()
        {
            if (Current == MovementState.Idle)
                return;
            TransitionTo(MovementState.Idle);
        }

        public void SetGrounded(bool grounded)
        {
            entity.SetGrounded(grounded);
            if (grounded && Current == MovementState.Jump)
                TransitionTo(MovementState.Idle);
        }

        public void Tick(float deltaTime)
        {
            entity.Tick(deltaTime);
            stamina.Tick(deltaTime);
        }

        private void TransitionTo(MovementState next)
        {
            var previous = entity.Current;
            entity.Enter(next);
            bus.Publish(new MovementStateChangedEvent(previous, next));
        }
    }
}
