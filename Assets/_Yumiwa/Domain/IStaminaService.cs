namespace Yumiwa.Domain
{
    /// <summary>Service contract for stamina spend gating and regeneration.</summary>
    public interface IStaminaService
    {
        float Current { get; }
        float Max { get; }
        bool CanAfford(float cost);
        bool TrySpend(float cost);
        void Tick(float deltaTime);
    }
}
