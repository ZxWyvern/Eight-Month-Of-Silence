using System.IO;
using System.Threading;
#if UNITY_5_3_OR_NEWER
using UnityEngine;
#endif
using Yumiwa.Domain;

namespace Yumiwa.Infrastructure
{
    /// <summary>
    /// Versioned local player-settings store per plan.json (subtitles, text size,
    /// background opacity, reading speed). Separate file from checkpoint saves —
    /// dialogue queue stays transient, so SAVE-001 is untouched.
    /// Synchronous atomic write (temp + Replace/Move under a gate): the payload is
    /// tiny and saves are user-initiated only, never per-frame. Mutable DTO shields
    /// the readonly <see cref="SettingsData"/> from JsonUtility limitations.
    /// Implements both contracts so MVP-01 has one on-disk store, never a
    /// JSON/PlayerPrefs split.
    /// </summary>
    public sealed class JsonFileSettingsRepository : ISettingsRepository, ISettingsStore
    {
        public const string Version = "1";
        private const string FileKey = "yumiwa_settings.json";

        private readonly string basePath;
        private static readonly SemaphoreSlim _writeGate = new SemaphoreSlim(1, 1);

        public JsonFileSettingsRepository(string basePath = null)
        {
#if UNITY_5_3_OR_NEWER
            this.basePath = basePath ?? UnityEngine.Application.persistentDataPath;
#else
            this.basePath = basePath ?? Path.GetTempPath();
#endif
        }

        [System.Serializable]
        private struct SettingsDto
        {
            public string DtoVersion;
            public bool HeadBobEnabled;
            public bool CameraShakeEnabled;
            public bool FovBoostEnabled;
            public bool SubtitlesEnabled;
            public float MouseSensitivity;
            public float Fov;
            public int TextSize;
            public float SubtitleBackgroundOpacity;
            public int ReadingSpeed;
        }

        public SettingsData Load() => LoadOrDefaults();

        public SettingsData LoadOrDefaults()
        {
            string path = Path.Combine(basePath, FileKey);
            if (!File.Exists(path))
                return SettingsData.Defaults();
            try
            {
                var json = File.ReadAllText(path);
#if UNITY_5_3_OR_NEWER
                var dto = JsonUtility.FromJson<SettingsDto>(json);
#else
                var dto = System.Text.Json.JsonSerializer.Deserialize<SettingsDto>(json);
#endif
                return new SettingsData(
                    dto.HeadBobEnabled, dto.CameraShakeEnabled, dto.FovBoostEnabled,
                    dto.SubtitlesEnabled, dto.MouseSensitivity, dto.Fov,
                    (DialogueTextSize)dto.TextSize, dto.SubtitleBackgroundOpacity,
                    (DialogueReadingSpeed)dto.ReadingSpeed);
            }
            catch (System.Exception e)
            {
#if UNITY_5_3_OR_NEWER
                Debug.LogWarning($"[Yumiwa] Settings load failed, using defaults: {e.Message}");
#else
                System.Console.WriteLine($"Settings load failed, using defaults: {e.Message}");
#endif
                return SettingsData.Defaults();
            }
        }

        public void Save(SettingsData settings)
        {
            var dto = new SettingsDto
            {
                DtoVersion = Version,
                HeadBobEnabled = settings.HeadBobEnabled,
                CameraShakeEnabled = settings.CameraShakeEnabled,
                FovBoostEnabled = settings.FovBoostEnabled,
                SubtitlesEnabled = settings.SubtitlesEnabled,
                MouseSensitivity = settings.MouseSensitivity,
                Fov = settings.Fov,
                TextSize = (int)settings.TextSize,
                SubtitleBackgroundOpacity = settings.SubtitleBackgroundOpacity,
                ReadingSpeed = (int)settings.ReadingSpeed
            };
#if UNITY_5_3_OR_NEWER
            string json = JsonUtility.ToJson(dto, true);
#else
            string json = System.Text.Json.JsonSerializer.Serialize(dto);
#endif
            string path = Path.Combine(basePath, FileKey);
            _writeGate.Wait();
            try
            {
                string dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
                string tmp = Path.Combine(dir ?? ".", System.Guid.NewGuid().ToString("N") + ".tmp");
                try
                {
                    File.WriteAllText(tmp, json);
                    if (File.Exists(path)) File.Replace(tmp, path, null);
                    else File.Move(tmp, path);
                }
                catch
                {
                    try { if (File.Exists(tmp)) File.Delete(tmp); } catch { }
                    throw;
                }
            }
            catch (System.Exception e)
            {
#if UNITY_5_3_OR_NEWER
                Debug.LogWarning($"[Yumiwa] Settings save failed {path}: {e.Message}");
#else
                System.Console.WriteLine($"Settings save failed {path}: {e.Message}");
#endif
            }
            finally { _writeGate.Release(); }
        }
    }
}
