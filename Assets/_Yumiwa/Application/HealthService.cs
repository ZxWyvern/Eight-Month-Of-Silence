using Yumiwa.Domain;

namespace Yumiwa.Application
{
    public sealed class HealthService : IHealthService
    {
        private readonly IEventBus bus;

        public float Current { get; private set; }
        public float Max { get; }
        public bool IsWounded => Current <= WoundThreshold && Current > 0f;
        public bool IsDead => Current <= 0f;
        public float WoundThreshold { get; }

        public event System.Action<HealthChangedEvent> OnChanged;
        public event System.Action<PlayerWoundedEvent> OnWounded;
        public event System.Action<PlayerHealedEvent> OnHealed;

        public HealthService(float max = 100f, float woundThreshold = 30f, IEventBus bus = null)
        {
            Max = max;
            WoundThreshold = woundThreshold;
            Current = max;
            this.bus = bus;
        }

        public void TakeDamage(float amount)
        {
            if (amount <= 0f || IsDead)
                return;

            float previous = Current;
            Current = (Current - amount) < 0f ? 0f : (Current - amount) > Max ? Max : (Current - amount);
            float delta = Current - previous;

            bus?.Publish(new HealthChangedEvent(Current, Max, delta));
            OnChanged?.Invoke(new HealthChangedEvent(Current, Max, delta));

            if (previous > WoundThreshold && Current <= WoundThreshold)
            {
                bus?.Publish(new PlayerWoundedEvent(Current));
                OnWounded?.Invoke(new PlayerWoundedEvent(Current));
            }

            if (Current <= 0f)
            {
                bus?.Publish(new PlayerWoundedEvent(0f));
                OnWounded?.Invoke(new PlayerWoundedEvent(0f));
            }
        }

        public void Heal(float amount)
        {
            if (amount <= 0f || Current >= Max)
                return;

            float previous = Current;
            Current = (Current + amount) > Max ? Max : (Current + amount);

            bus?.Publish(new PlayerHealedEvent(previous, Current));
            OnHealed?.Invoke(new PlayerHealedEvent(previous, Current));
            bus?.Publish(new HealthChangedEvent(Current, Max, Current - previous));
            OnChanged?.Invoke(new HealthChangedEvent(Current, Max, Current - previous));
        }

        public void ResetHealth()
        {
            float previous = Current;
            Current = Max;
            bus?.Publish(new HealthChangedEvent(Current, Max, Current - previous));
            OnChanged?.Invoke(new HealthChangedEvent(Current, Max, Current - previous));
        }
    }
}