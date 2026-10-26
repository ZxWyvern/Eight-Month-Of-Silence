using System.IO;
using System.Threading;
using System.Threading.Tasks;
#if UNITY_5_3_OR_NEWER
using UnityEngine;
#endif
using Yumiwa.Domain;

namespace Yumiwa.Infrastructure
{
    /// <summary>
    /// File-backed save repository per GDD §52 — separate Chapter vs House stores.
    /// Atomic write via temp-file + Move; versioned via CheckpointData.SaveVersion.
    /// Non-blocking per ARCH §8: Awaitable.BackgroundThreadAsync on Unity, Task.Run fallback in EditMode tests.
    /// P1-F04 fix: unique tmp per write + SemaphoreSlim gate + atomic overwrite (no delete-then-move hazard).
    /// Infrastructure only, never referenced directly from Domain/Application beyond interface.
    /// </summary>
    public sealed class JsonFileSaveRepository : ISaveRepository
    {
        private const string ChapterKey = "yumiwa_chapter_save.json";
        private const string HouseKey = "yumiwa_house_save.json";
        private const string Version = "4"; // P1-F05 bump for inventory/evidence/puzzle/mission (was 3)

        private readonly string basePath;
        private static readonly SemaphoreSlim _writeGate = new SemaphoreSlim(1, 1);

        public JsonFileSaveRepository(string basePath = null)
        {
#if UNITY_5_3_OR_NEWER
            this.basePath = basePath ?? UnityEngine.Application.persistentDataPath;
#else
            this.basePath = basePath ?? Path.GetTempPath();
#endif
        }

        private string ChapterPath => Path.Combine(basePath, ChapterKey);
        private string HousePath => Path.Combine(basePath, HouseKey);

        [System.Serializable]
        private struct HouseWrapper
        {
            public CheckpointData Data;
            public CheckpointMilestone Milestone;
        }

        public bool HasChapterSave => File.Exists(ChapterPath);
        public bool HasHouseCheckpoint => File.Exists(HousePath);

        public void SaveChapter(CheckpointData data)
        {
            data.SaveVersion = Version;
            data.Timestamp = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();
#if UNITY_5_3_OR_NEWER
            WriteAtomicAsync(ChapterPath, JsonUtility.ToJson(data, true));
#else
            WriteAtomicAsync(ChapterPath, System.Text.Json.JsonSerializer.Serialize(data));
#endif
        }

        public CheckpointData LoadChapter()
        {
            if (!HasChapterSave)
                return default;
            var json = File.ReadAllText(ChapterPath);
#if UNITY_5_3_OR_NEWER
            var data = JsonUtility.FromJson<CheckpointData>(json);
#else
            var data = System.Text.Json.JsonSerializer.Deserialize<CheckpointData>(json);
#endif
            if (string.IsNullOrEmpty(data.SaveVersion))
                data.SaveVersion = Version;
            return data;
        }

        public void ClearChapter()
        {
            // Same gate as writes: never delete opposite an in-flight chapter write.
            _writeGate.Wait();
            try
            {
                if (File.Exists(ChapterPath))
                    File.Delete(ChapterPath);
            }
            finally { _writeGate.Release(); }
        }

        public void SaveHouseCheckpoint(CheckpointData data, CheckpointMilestone milestone)
        {
            data.Milestone = milestone;
            data.SaveVersion = Version;
            data.Timestamp = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var wrapper = new HouseWrapper { Data = data, Milestone = milestone };
#if UNITY_5_3_OR_NEWER
            WriteAtomicAsync(HousePath, JsonUtility.ToJson(wrapper, true));
#else
            WriteAtomicAsync(HousePath, System.Text.Json.JsonSerializer.Serialize(wrapper));
#endif
        }

        public CheckpointData LoadHouseCheckpoint(out CheckpointMilestone milestone)
        {
            milestone = CheckpointMilestone.None;
            if (!HasHouseCheckpoint)
                return default;
            var json = File.ReadAllText(HousePath);
#if UNITY_5_3_OR_NEWER
            var wrapper = JsonUtility.FromJson<HouseWrapper>(json);
#else
            var wrapper = System.Text.Json.JsonSerializer.Deserialize<HouseWrapper>(json);
#endif
            milestone = wrapper.Milestone;
            return wrapper.Data;
        }

        public void ClearHouseCheckpoint()
        {
            // P1-F04: gate clear with same semaphore to avoid delete vs in-flight write race
            _writeGate.Wait();
            try
            {
                if (File.Exists(HousePath))
                    File.Delete(HousePath);
            }
            finally { _writeGate.Release(); }
        }

        public void ClearAll()
        {
            _writeGate.Wait();
            try
            {
                if (File.Exists(HousePath))
                    File.Delete(HousePath);
                if (File.Exists(ChapterPath))
                    File.Delete(ChapterPath);
            }
            finally { _writeGate.Release(); }
        }

        /// <summary>
        /// Verification hook (tests/diagnostics): completes once every write queued
        /// before the call has finished. All writes hold <see cref="_writeGate"/>
        /// during I/O, so acquiring it afterwards proves earlier writers are done.
        /// Production call graph unchanged — ADR-0016 async-void lifter kept.
        /// </summary>
        public static Task DrainAsync()
        {
            return Task.Run(async () =>
            {
                await _writeGate.WaitAsync().ConfigureAwait(false);
                _writeGate.Release();
            });
        }

        private static void MoveReplacing(string tmp, string path)
        {
            // Atomic overwrite where the platform supports it: Replace is a single
            // OS-level replace (no crash window between delete and move). Fresh
            // files that have no destination yet fall back to Move.
            if (File.Exists(path)) File.Replace(tmp, path, null);
            else File.Move(tmp, path);
        }

        private static async void WriteAtomicAsync(string path, string content)
        {
            // P1-F04: unique tmp per call + gate + atomic overwrite, keep async void lifter per ADR-0016 but safe
            string dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
            string tmp = Path.Combine(dir ?? ".", System.Guid.NewGuid().ToString("N") + ".tmp");
#if UNITY_5_3_OR_NEWER
            await _writeGate.WaitAsync().ConfigureAwait(false);
            try
            {
                await Awaitable.BackgroundThreadAsync();
                await File.WriteAllTextAsync(tmp, content).ConfigureAwait(false);
                // Ensure file is flushed before move
                MoveReplacing(tmp, path);
            }
            catch (System.Exception e)
            {
                UnityEngine.Debug.LogWarning($"[Yumiwa] Save async failed {path}: {e.Message}");
                try { if (File.Exists(tmp)) File.Delete(tmp); } catch { }
            }
            finally { _writeGate.Release(); }
#else
            await _writeGate.WaitAsync().ConfigureAwait(false);
            try
            {
                await Task.Run(async () =>
                {
                    await File.WriteAllTextAsync(tmp, content).ConfigureAwait(false);
                    MoveReplacing(tmp, path);
                }).ConfigureAwait(false);
            }
            catch (System.Exception e)
            {
                System.Console.WriteLine($"Save async failed {path}: {e.Message}");
                try { if (File.Exists(tmp)) File.Delete(tmp); } catch { }
            }
            finally { _writeGate.Release(); }
#endif
        }
    }
}
