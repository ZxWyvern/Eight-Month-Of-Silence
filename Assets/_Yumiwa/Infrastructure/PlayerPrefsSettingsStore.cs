using UnityEngine;
using Yumiwa.Domain;

namespace Yumiwa.Infrastructure
{
    /// <summary>
    /// PlayerPrefs-backed settings store (MVP-01). Presentation talks only to
    /// the Domain ISettingsStore boundary; PlayerPrefs I/O stays here.
    /// </summary>
    public sealed class PlayerPrefsSettingsStore : ISettingsStore
    {
        private const string Prefix = "Yumiwa.Settings.";
        private readonly SettingsData fallback;

        public PlayerPrefsSettingsStore(SettingsData? fallback = null)
        {
            this.fallback = fallback ?? SettingsData.Defaults();
        }

        public SettingsData Load()
        {
            if (!PlayerPrefs.HasKey(Prefix + "saved"))
                return fallback;
            return new SettingsData(
                PlayerPrefs.GetInt(Prefix + "headBob", 1) == 1,
                PlayerPrefs.GetInt(Prefix + "shake", 1) == 1,
                PlayerPrefs.GetInt(Prefix + "fovBoost", 1) == 1,
                PlayerPrefs.GetInt(Prefix + "subtitles", 1) == 1,
                PlayerPrefs.GetFloat(Prefix + "sensitivity", 1f),
                PlayerPrefs.GetFloat(Prefix + "fov", 75f));
        }

        public void Save(SettingsData settings)
        {
            PlayerPrefs.SetInt(Prefix + "saved", 1);
            PlayerPrefs.SetInt(Prefix + "headBob", settings.HeadBobEnabled ? 1 : 0);
            PlayerPrefs.SetInt(Prefix + "shake", settings.CameraShakeEnabled ? 1 : 0);
            PlayerPrefs.SetInt(Prefix + "fovBoost", settings.FovBoostEnabled ? 1 : 0);
            PlayerPrefs.SetInt(Prefix + "subtitles", settings.SubtitlesEnabled ? 1 : 0);
            PlayerPrefs.SetFloat(Prefix + "sensitivity", settings.MouseSensitivity);
            PlayerPrefs.SetFloat(Prefix + "fov", settings.Fov);
            PlayerPrefs.Save();
        }
    }
}
