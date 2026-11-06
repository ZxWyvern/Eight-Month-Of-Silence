using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using VContainer;
using Yumiwa.Application;
using Yumiwa.Domain;

namespace Yumiwa.Presentation
{
    /// <summary>
    /// Interlude otomatis pasca-Bab 1 (ADR-0032): tampilkan typewriter via
    /// TitleCardView, tahan sesuai hold, lalu fade → scene berikut.
    /// Pure Presentation: tanpa gameplay state; loader/fader punya fallback
    /// runtime bila scene ini tidak punya LifetimeScope (preseden: DoorScenePortal).
    /// </summary>
    public sealed class InterludeAutoAdvance : MonoBehaviour
    {
        [SerializeField, TextArea(1, 3)] private string title = "[TULIS JUDUL INTERLUDE]";
        [SerializeField, TextArea(2, 6)] private string subtitle = "[Tulis teks interlude di Inspector]";
        [SerializeField] private float holdAfterCompleteSeconds = 2f;
        [SerializeField] private string nextSceneName = "Hutan";
        [SerializeField] private TitleCardView titleCard;
        [SerializeField] private ScreenFader fader;

        private ISceneLoader loader;
        private float holdTime;
        private bool leaving;

        [Inject]
        public void Construct(ISceneLoader loader)
        {
            this.loader = loader;
        }

        private void Awake()
        {
            if (titleCard == null)
                titleCard = GetComponentInChildren<TitleCardView>(true);
            if (titleCard == null)
                titleCard = FindFirstObjectByType<TitleCardView>();
            if (fader == null)
            {
                fader = GetComponentInChildren<ScreenFader>(true);
                if (fader == null)
                    fader = FindFirstObjectByType<ScreenFader>();
            }
            if (fader == null)
                fader = gameObject.AddComponent<ScreenFader>();
        }

        private void Start()
        {
            holdTime = 0f;
            if (titleCard != null)
                titleCard.ShowTitle(title, subtitle);
            else
                Debug.LogWarning("[Yumiwa] InterludeAutoAdvance: TitleCardView tidak ditemukan.", this);
        }

        private void Update()
        {
            if (leaving || titleCard == null || !titleCard.IsComplete)
                return;
            holdTime += Time.unscaledDeltaTime;
            if (holdTime >= Mathf.Max(0f, holdAfterCompleteSeconds))
                StartCoroutine(FadeAndGoNext());
        }

        private IEnumerator FadeAndGoNext()
        {
            leaving = true;
            System.Threading.Tasks.Task fadeTask = null;
            try { fadeTask = fader.FadeOutAsync(); }
            catch (System.Exception e) { Debug.LogWarning("[Yumiwa] Interlude fade gagal, cut ke '" + nextSceneName + "': " + e.Message, this); }
            if (fadeTask != null)
                yield return new WaitUntil(() => fadeTask.IsCompleted);
            if (string.IsNullOrEmpty(nextSceneName))
            {
                Debug.LogError("[Yumiwa] InterludeAutoAdvance nextSceneName kosong.", this);
                yield break;
            }
            if (loader == null)
            {
                SceneManager.LoadScene(nextSceneName);
                yield break;
            }
            var task = loader.LoadSceneAsync(nextSceneName);
            while (task != null && !task.IsCompleted)
                yield return null;
            if (task != null && task.IsFaulted)
                Debug.LogError($"[Yumiwa] Failed to load {nextSceneName}: {task.Exception}");
        }
    }
}
