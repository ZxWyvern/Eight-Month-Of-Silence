namespace Yumiwa.Domain
{
    /// <summary>Service contract for requesting and tracking movement states.</summary>
    public interface IMovementStateMachine
    {
        MovementState Current { get; }
        float StateTime { get; }
        bool Grounded { get; }
        bool RequestEnter(MovementState next);
        bool RequestEnter(MovementState next, bool hasJumpObstacle);
        void RequestExit();
        void SetGrounded(bool grounded);
        void Tick(float deltaTime);
    }
}
