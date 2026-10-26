namespace Yumiwa.Domain
{
    /// <summary>Enumerates the player movement states: grounded locomotion only (Pilar 1: Grounded Horror).</summary>
    public enum MovementState
    {
        Idle,
        Walk,
        Sprint,
        Crouch,
        Jump
    }
}
