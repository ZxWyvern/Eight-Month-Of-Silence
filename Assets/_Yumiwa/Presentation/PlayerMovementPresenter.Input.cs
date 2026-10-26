using Yumiwa.Domain;

namespace Yumiwa.Presentation
{
    public sealed partial class PlayerMovementPresenter
    {
        private void OnConfigReloaded(MovementConfigReloadedEvent evt) =>
            ApplyMovementConfig(evt.Movement);

        private void OnJumpPressed()
        {
            jumpBufferTimer = movement.JumpBufferSeconds;
        }

        private void OnCrouchPressed()
        {
            if (machine.Current == MovementState.Crouch)
                machine.RequestExit();
            else if (machine.Grounded)
                machine.RequestEnter(MovementState.Crouch);
        }

        private void OnJumpReleased()
        {
            if (verticalVelocity > 0f)
                verticalVelocity *= movement.JumpCutMultiplier;
        }
    }
}
