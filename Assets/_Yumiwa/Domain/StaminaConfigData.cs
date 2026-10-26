namespace Yumiwa.Domain
{
    /// <summary>Plain-data stamina tuning snapshot (no UnityEngine dependency).</summary>
    public readonly struct StaminaConfigData
    {
        public readonly float MaxStamina;
        public readonly float RegenPerSecond;
        public readonly float RegenDelaySeconds;
        public readonly float SprintDrainPerSecond;

        public StaminaConfigData(float maxStamina, float regenPerSecond, float regenDelaySeconds, float sprintDrainPerSecond)
        {
            MaxStamina = maxStamina;
            RegenPerSecond = regenPerSecond;
            RegenDelaySeconds = regenDelaySeconds;
            SprintDrainPerSecond = sprintDrainPerSecond;
        }
    }
}
