namespace Yumiwa.Domain
{
    /// <summary>Raised on every accepted movement-state transition.</summary>
    public readonly struct MovementStateChangedEvent
    {
        public readonly MovementState Previous;
        public readonly MovementState Current;

        public MovementStateChangedEvent(MovementState previous, MovementState current)
        {
            Previous = previous;
            Current = current;
        }
    }
}
