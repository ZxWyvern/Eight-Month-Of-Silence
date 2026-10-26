using Yumiwa.Domain;

namespace Yumiwa.Domain
{
    public interface IHealthService
    {
        float Current { get; }
        float Max { get; }
        bool IsWounded { get; }
        bool IsDead { get; }
        float WoundThreshold { get; }

        event System.Action<HealthChangedEvent> OnChanged;
        event System.Action<PlayerWoundedEvent> OnWounded;
        event System.Action<PlayerHealedEvent> OnHealed;

        void TakeDamage(float amount);
        void Heal(float amount);
        void ResetHealth();
    }
}