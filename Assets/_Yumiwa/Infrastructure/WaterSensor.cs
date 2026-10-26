using Yumiwa.Domain;

namespace Yumiwa.Infrastructure
{
    /// <summary>IWaterProvider tracking trigger-registered water volumes (overlap count).</summary>
    public sealed class WaterSensor : IWaterProvider
    {
        private int overlaps;

        public bool IsInWater => overlaps > 0;

        public void Enter() => overlaps++;

        public void Exit()
        {
            if (overlaps > 0)
                overlaps--;
        }
    }
}
