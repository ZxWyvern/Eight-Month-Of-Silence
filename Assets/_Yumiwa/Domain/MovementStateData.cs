namespace Yumiwa.Domain
{
    /// <summary>Per-state tuning bundle: stamina cost plus exposed vital points.</summary>
    public readonly struct MovementStateData
    {
        public readonly float StaminaCost;
        public readonly VitalPoint[] ExposedVitalPoints;

        public MovementStateData(float staminaCost, VitalPoint[] exposedVitalPoints)
        {
            StaminaCost = staminaCost;
            ExposedVitalPoints = exposedVitalPoints;
        }
    }
}
