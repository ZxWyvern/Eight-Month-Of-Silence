namespace Yumiwa.Domain
{
    /// <summary>
    /// Versioned local player-settings store (plan.json player_settings persistence).
    /// Independent from CheckpointData — dialogue queue stays transient (SAVE-001 untouched).
    /// Infrastructure implements with atomic temp-file writes.
    /// </summary>
    public interface ISettingsRepository
    {
        SettingsData LoadOrDefaults();
        void Save(SettingsData settings);
    }
}
