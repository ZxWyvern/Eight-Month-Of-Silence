using UnityEngine;
using Yumiwa.Domain;

namespace Yumiwa.Infrastructure
{
    /// <summary>ScriptableObject stamina tunables; defaults mirror rules.json stamina.config.</summary>
    [CreateAssetMenu(menuName = "Yumiwa/StaminaConfig", fileName = "StaminaConfig")]
    public sealed class StaminaConfig : ScriptableObject
    {
        public float MaxStamina = 100f;
        public float RegenPerSecond = 10f;
        public float RegenDelaySeconds = 1.5f;
        public float SprintDrainPerSecond = 8f;

        public StaminaConfigData ToData() => new StaminaConfigData(
            MaxStamina, RegenPerSecond, RegenDelaySeconds, SprintDrainPerSecond);
    }
}
