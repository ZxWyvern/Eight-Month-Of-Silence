namespace Yumiwa.Domain
{
    /// <summary>Contract for config delivery; Addressables implementation plugs in later. Extended in ADR-0018 to expose ItemDefinition lookup without Presentation needing Infrastructure.</summary>
    public interface IConfigProvider
    {
        MovementConfigData GetMovement();
        StaminaConfigData GetStamina();
        CensorEffectData GetCensor();
        InspectionTuningData GetInspection();
        bool HasItem(string itemId);
        ItemDefinition GetItemDefinition(string itemId);
        Evidence GetEvidence(string itemId);
        InspectionHotspot[] GetHotspots(string itemId);
    }
}
