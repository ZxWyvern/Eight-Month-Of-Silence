namespace Yumiwa.Domain
{
    /// <summary>Persistence boundary for player settings (docs/07). Implemented in Infrastructure.</summary>
    public interface ISettingsStore
    {
        SettingsData Load();
        void Save(SettingsData settings);
    }
}
