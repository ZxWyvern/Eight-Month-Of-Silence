namespace Yumiwa.Domain
{
    /// <summary>Dev-time hot-reload broadcast carrying a fresh movement tuning snapshot.</summary>
    public readonly struct MovementConfigReloadedEvent
    {
        public readonly MovementConfigData Movement;

        public MovementConfigReloadedEvent(MovementConfigData movement)
        {
            Movement = movement;
        }
    }
}
