#if UNITY_5_3_OR_NEWER
using UnityEngine;
using UnityEngine.SceneManagement;
#endif
using System.IO;
using System.Threading.Tasks;
using Yumiwa.Domain;

namespace Yumiwa.Infrastructure
{
    /// <summary>
    /// ISceneLoader over Unity SceneManager (Single mode, per docs/09 stage-based scenes).
    /// Names absent from Build Settings are rejected before touching the engine, so a typo
    /// becomes a loud Task failure (caught by SceneTransitionService → fade back in + retry)
    /// instead of a silent stall. Follows JsonFileSaveRepository's #if pattern so the
    /// type also compiles in engine-free EditMode harnesses (no-op load there).
    /// </summary>
    public sealed class UnitySceneLoader : ISceneLoader
    {
        public string ActiveSceneName
        {
            get
            {
#if UNITY_5_3_OR_NEWER
                return SceneManager.GetActiveScene().name;
#else
                return string.Empty;
#endif
            }
        }

        public async Task LoadSceneAsync(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName))
                throw new System.ArgumentException("Scene name must not be empty.", nameof(sceneName));
#if UNITY_5_3_OR_NEWER
            if (!IsInBuildSettings(sceneName))
                throw new System.InvalidOperationException(
                    "Scene '" + sceneName + "' is not in Build Settings. Add it via File > Build Settings.");
            var op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            if (op == null)
                throw new System.InvalidOperationException("Scene '" + sceneName + "' failed to start loading.");
            op.allowSceneActivation = true;
            while (!op.isDone)
                await Awaitable.NextFrameAsync();
#else
            await Task.Delay(1).ConfigureAwait(false);
#endif
        }

#if UNITY_5_3_OR_NEWER
        private static bool IsInBuildSettings(string sceneName)
        {
            int count = SceneManager.sceneCountInBuildSettings;
            for (int i = 0; i < count; i++)
            {
                string path = SceneUtility.GetScenePathByBuildIndex(i);
                if (string.Equals(Path.GetFileNameWithoutExtension(path), sceneName, System.StringComparison.Ordinal))
                    return true;
            }
            return false;
        }
#endif
    }
}
