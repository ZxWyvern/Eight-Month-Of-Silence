namespace Yumiwa.Domain
{
    /// <summary>Raised whenever the stamina pool value changes.</summary>
    public readonly struct StaminaChangedEvent
    {
        public readonly float Current;
        public readonly float Max;

        public StaminaChangedEvent(float current, float max)
        {
            Current = current;
            Max = max;
        }
    }
}
