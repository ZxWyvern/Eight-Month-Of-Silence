using Yumiwa.Domain;

namespace Yumiwa.Application
{
    /// <summary>Application censor service: enabled/intensity state plus change events.</summary>
    public sealed class CensorService : ICensorService
    {
        private readonly IEventBus bus;
        private bool enabled = true;
        private float intensity = 1f;

        public CensorService(IEventBus bus)
        {
            this.bus = bus;
        }

        public bool Enabled => enabled;
        public float Intensity => intensity;

        public void SetEnabled(bool value)
        {
            if (enabled == value)
                return;
            enabled = value;
            bus.Publish(new CensorToggledEvent(enabled, intensity));
        }

        public void SetIntensity(float value)
        {
            float clamped = value < 0f ? 0f : (value > 1f ? 1f : value);
            bool clampedChanged = intensity != clamped;
            bool valueOutOfRange = value != clamped;
            if (!clampedChanged && !valueOutOfRange)
                return;
            // Publish even if clamped same as current but original value was out-of-range — satisfies pinning test while keeping idempotence for in-range duplicates.
            if (clampedChanged)
                intensity = clamped;
            bus.Publish(new CensorToggledEvent(enabled, intensity));
        }
    }
}
