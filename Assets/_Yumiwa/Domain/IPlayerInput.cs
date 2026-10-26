namespace Yumiwa.Domain
{
    /// <summary>Sampled player-input boundary; vectors sampled, buttons evented.</summary>
    public interface IPlayerInput
    {
        PlanarDirection MoveDirection { get; }
        float LookDeltaX { get; }
        float LookDeltaY { get; }
        bool IsSprintHeld { get; }

        event System.Action JumpPressed;
        event System.Action JumpReleased;
        event System.Action FirePressed;
        event System.Action CrouchPressed;
        event System.Action InventoryPressed;
        event System.Action SwapHandsPressed;
        event System.Action DropHeldPressed;
        event System.Action UsePressed;
        event System.Action CensorPressed;

        void Enable();
        void Disable();
    }
}
