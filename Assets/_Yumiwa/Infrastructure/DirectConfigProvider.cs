using System.Collections.Generic;
using Yumiwa.Domain;

namespace Yumiwa.Infrastructure
{
    /// <summary>Direct ScriptableObject config delivery; Addressables variant plugs in later. Now also serves ItemDefinition lookup for ADR-0018 decoupling.</summary>
    public sealed class DirectConfigProvider : IConfigProvider
    {
        private readonly MovementConfig movement;
        private readonly StaminaConfig stamina;
        private readonly CensorEffectConfig censor;
        private readonly InspectionConfig inspection;
        private readonly Dictionary<string, ItemData> itemMap;

        public DirectConfigProvider(MovementConfig movement, StaminaConfig stamina, CensorEffectConfig censor = null, InspectionConfig inspection = null, IEnumerable<ItemData> items = null)
        {
            this.movement = movement;
            this.stamina = stamina;
            this.censor = censor;
            this.inspection = inspection;
            itemMap = new Dictionary<string, ItemData>();
            if (items != null)
            {
                foreach (var it in items)
                {
                    if (it != null && !string.IsNullOrEmpty(it.ItemId) && !itemMap.ContainsKey(it.ItemId))
                        itemMap[it.ItemId] = it;
                }
            }
        }

        public MovementConfigData GetMovement() => movement != null ? movement.ToData() : MovementConfigData.Defaults();
        public StaminaConfigData GetStamina() => stamina != null ? stamina.ToData() : new StaminaConfigData(100f, 10f, 1.5f, 8f);
        public CensorEffectData GetCensor() => censor != null ? censor.ToData() : CensorEffectData.Defaults();
        public InspectionTuningData GetInspection() => inspection != null ? inspection.ToData() : InspectionTuningData.Defaults();

        public bool HasItem(string itemId) => !string.IsNullOrEmpty(itemId) && itemMap.ContainsKey(itemId);

        public ItemDefinition GetItemDefinition(string itemId)
        {
            if (string.IsNullOrEmpty(itemId) || !itemMap.TryGetValue(itemId, out var data) || data == null)
                return default;
            return data.ToDefinition();
        }

        public Evidence GetEvidence(string itemId)
        {
            if (string.IsNullOrEmpty(itemId) || !itemMap.TryGetValue(itemId, out var data) || data == null)
                return default;
            return data.ToEvidence();
        }

        public InspectionHotspot[] GetHotspots(string itemId)
        {
            if (string.IsNullOrEmpty(itemId) || !itemMap.TryGetValue(itemId, out var data) || data == null)
                return System.Array.Empty<InspectionHotspot>();
            return data.ToHotspots();
        }
    }
}
