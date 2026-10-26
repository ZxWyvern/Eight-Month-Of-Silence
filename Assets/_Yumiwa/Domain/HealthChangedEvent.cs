namespace Yumiwa.Domain
{
    public struct HealthChangedEvent
    {
        public readonly float Current;
        public readonly float Max;
        public readonly float Delta;

        public HealthChangedEvent(float current, float max, float delta)
        {
            Current = current;
            Max = max;
            Delta = delta;
        }
    }
}