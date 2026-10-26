namespace Yumiwa.Domain
{
    /// <summary>Contract for sampling water-volume overlap (swim/dash gating).</summary>
    public interface IWaterProvider
    {
        bool IsInWater { get; }
        void Enter();
        void Exit();
    }
}
