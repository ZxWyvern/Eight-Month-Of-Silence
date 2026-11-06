using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using VContainer;
using Yumiwa.Application;
using Yumiwa.Domain;

namespace Yumiwa.Presentation
{
    /// <summary>
    /// ADR-0030/0031: bukti lengkap → fade hitam full → scene berikut.
    /// Tidak ada panel/teks/tombol — seluruh UI teks "BAB 1 SELESAI" dihapus.
    /// </summary>
    public sealed class ChapterCompletionPresenter : MonoBehaviour
    {
        [SerializeField] private float autoReturnSeconds = 8f;
        [Tooltip("Scene tujuan setelah bukti lengkap (fade cut). Harus ada di Build Settings.")]
        [SerializeField] private string nextSceneName = "Hutan";
        [Tooltip("Fader untuk transisi. Kosong = dicari di scene, lalu dibuat runtime bila tidak ada.")]
        [SerializeField] private ScreenFader fader;

        private IEventBus bus;
        private ISceneLoader loader;
        private InputActionAsset inputAsset;
        private InputActionMap playerMap;
        private bool completed;
        private bool returning;
        private bool subscribed;

        [Inject]
        public void Construct(IEventBus bus, ISceneLoader loader, InputActionAsset inputAsset)
        {
            this.bus = bus;
            this.loader = loader;
            this.inputAsset = inputAsset;
        }

        private void OnEnable()
        {
            Subscribe();
        }

        private void Start()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            if (!subscribed || bus == null)
                return;
            bus.Unsubscribe<Act1ChapterCompletedEvent>(OnChapterCompleted);
            subscribed = false;
        }

        private void Subscribe()
        {
            if (subscribed || bus == null)
                return;
            bus.Subscribe<Act1ChapterCompletedEvent>(OnChapterCompleted);
            subscribed = true;
        }

        private void OnChapterCompleted(Act1ChapterCompletedEvent evt)
        {
            if (completed)
                return;
            completed = true;
            if (inputAsset != null)
            {
                playerMap = inputAsset.FindActionMap("Player", false);
                playerMap?.Disable();
            }
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            StartCoroutine(AutoReturn());
        }

        private IEnumerator AutoReturn()
        {
            yield return new WaitForSecondsRealtime(0.5f);
            yield return FadeAndGoNext();
        }

        private IEnumerator FadeAndGoNext()
        {
            if (returning)
                yield break;
            returning = true;
            playerMap?.Enable();
            Time.timeScale = 1f;
            if (fader == null)
            {
                fader = GetComponentInChildren<ScreenFader>(true);
                if (fader == null)
                    fader = FindFirstObjectByType<ScreenFader>();
            }
            if (fader == null)
                fader = gameObject.AddComponent<ScreenFader>();
            System.Threading.Tasks.Task fadeTask = null;
            try { fadeTask = fader.FadeOutAsync(); }
            catch (System.Exception e) { Debug.LogWarning("[Yumiwa] Fade out gagal, cut langsung ke '" + nextSceneName + "': " + e.Message, this); }
            if (fadeTask != null)
                yield return new WaitUntil(() => fadeTask.IsCompleted);
            if (string.IsNullOrEmpty(nextSceneName))
            {
                Debug.LogError("[Yumiwa] ChapterCompletion nextSceneName kosong — tetap di scene ini.", this);
                yield break;
            }
            if (loader == null)
            {
                SceneManager.LoadScene(nextSceneName);
                yield break;
            }
            yield return LoadNext();
        }

        private IEnumerator LoadNext()
        {
            var task = loader.LoadSceneAsync(nextSceneName);
            while (task != null && !task.IsCompleted)
                yield return null;
            if (task != null && task.IsFaulted)
                Debug.LogError($"[Yumiwa] Failed to load {nextSceneName}: {task.Exception}");
        }
    }
}
