using UnityEngine;
using Yumiwa.Domain;

namespace Yumiwa.Presentation
{
    /// <summary>Sprint handling extracted from Motor (SIZE-001). Owns sprint state + stamina drain.</summary>
    public sealed partial class PlayerMovementPresenter
    {
        private void UpdateSprint(float dt, bool grounded)
        {
            bool wantsSprint = grounded
                && input.IsSprintHeld
                && !crouching
                && input.MoveDirection.HasDirection
                && (machine.Current == MovementState.Walk || machine.Current == MovementState.Idle || machine.Current == MovementState.Sprint);

            if (wantsSprint && machine.Current != MovementState.Sprint)
            {
                machine.RequestEnter(MovementState.Sprint);
            }
            else if (!wantsSprint && machine.Current == MovementState.Sprint)
            {
                machine.RequestEnter(MovementState.Walk);
            }

            sprinting = machine.Current == MovementState.Sprint;
            crouching = machine.Current == MovementState.Crouch;
            if (!sprinting)
                return;
            float cost = staminaCfg.SprintDrainPerSecond * dt;
            if (!stamina.CanAfford(cost))
                machine.RequestEnter(MovementState.Walk);
            else
                stamina.TrySpend(cost);
        }

        private float SprintSpeed() => movement.WalkSpeed * movement.SprintMultiplier;
        private float WoundedModifier() => wounded ? 0.75f : 1f;
        private float WaterMoveScale() =>
            water != null && water.IsInWater ? movement.WaterMoveMultiplier : 1f;
    }
}
