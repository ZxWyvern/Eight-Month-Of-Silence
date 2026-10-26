using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Yumiwa.Presentation
{
    /// <summary>
    /// Full-screen fade overlay for door scene transitions. Builds its own Canvas +
    /// CanvasGroup + Image in Awake when unassigned, so setup is one component drop.
    /// Durations are inspector tuning next to the visual (same precedent as
    /// TrapZone.warningDelay, ADR-0013) — no magic numbers in code paths.
    /// Zero per-frame allocation outside an active fade: single coroutine, no LINQ.
    /// NOT DontDestroyOnLoad by design: fade-out always completes before the load
    /// starts, and each scene owns its fader (destination scenes tick startOpaque +
    /// fadeInOnStart). This keeps DI per-scene (ADR-0009) with no static instance.
    /// </summary>
    public sealed class ScreenFader : MonoBehaviour
    {
        [SerializeField] private float fadeOutSeconds = 0.6f;
        [SerializeField] private float fadeInSeconds = 0.6f;
        [SerializeField] private Color fadeColor = Color.black;
        [Tooltip("Tick on faders in destination scenes: first rendered frame is fully opaque.")]
        [SerializeField] private bool startOpaque;
        [Tooltip("Tick on faders in destination scenes: auto fade in on Start.")]
        [SerializeField] private bool fadeInOnStart;
        [SerializeField] private Canvas fadeCanvas;
        [SerializeField] private CanvasGroup fadeGroup;
        [SerializeField] private Image fadeImage;

        private Coroutine _routine;
        private TaskCompletionSource<bool> _tcs;

        public bool IsFading { get; private set; }

        public float FadeOutSeconds => fadeOutSeconds;

        public float FadeInSeconds => fadeInSeconds;

        private void Awake()
        {
            EnsureBuilt();
            fadeImage.color = fadeColor;
            SetAlphaInstant(startOpaque ? 1f : 0f);
        }

        private void Start()
        {
            // Fire-and-forget is safe here: FadeInAsync never faults (internal catch + log).
            if (fadeInOnStart)
                _ = FadeInAsync();
        }

        private void OnDestroy()
        {
            // Never leave an awaiter hanging if the scene tears down mid-fade.
            if (_tcs != null)
                _tcs.TrySetResult(false);
            _tcs = null;
        }

        public Task FadeOutAsync() => RunFade(1f, Mathf.Max(0.01f, fadeOutSeconds));

        public Task FadeInAsync() => RunFade(0f, Mathf.Max(0.01f, fadeInSeconds));

        private async Task RunFade(float target, float seconds)
        {
            try
            {
                if (fadeGroup == null)
                    return;
                if (!IsFading && Mathf.Approximately(fadeGroup.alpha, target))
                    return;
                if (_routine != null)
                    StopCoroutine(_routine);
                _tcs = new TaskCompletionSource<bool>();
                _routine = StartCoroutine(FadeRoutine(target, seconds, _tcs));
                await _tcs.Task;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[Yumiwa] ScreenFader fade aborted: " + e.Message, this);
            }
        }

        private IEnumerator FadeRoutine(float target, float seconds, TaskCompletionSource<bool> tcs)
        {
            IsFading = true;
            fadeGroup.blocksRaycasts = true;
            float start = fadeGroup.alpha;
            float t = 0f;
            while (t < 1f)
            {
                t += Time.unscaledDeltaTime / seconds;
                if (t > 1f)
                    t = 1f;
                fadeGroup.alpha = Mathf.Lerp(start, target, t);
                yield return null;
            }
            fadeGroup.alpha = target;
            fadeGroup.blocksRaycasts = target > 0.5f;
            IsFading = false;
            _routine = null;
            tcs.TrySetResult(true);
        }

        private void SetAlphaInstant(float alpha)
        {
            fadeGroup.alpha = alpha;
            fadeGroup.blocksRaycasts = alpha > 0.5f;
        }

        private void EnsureBuilt()
        {
            if (fadeCanvas == null)
            {
                var canvasGo = new GameObject("ScreenFaderCanvas");
                canvasGo.transform.SetParent(transform, false);
                fadeCanvas = canvasGo.AddComponent<Canvas>();
                fadeCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                fadeCanvas.sortingOrder = 999;
                fadeGroup = canvasGo.AddComponent<CanvasGroup>();
                var imageGo = new GameObject("FadeImage");
                imageGo.transform.SetParent(canvasGo.transform, false);
                fadeImage = imageGo.AddComponent<Image>();
                StretchFullScreen(fadeImage.rectTransform);
                fadeImage.sprite = MakeWhiteSprite();
            }
            if (fadeGroup == null)
            {
                fadeGroup = fadeCanvas.GetComponent<CanvasGroup>();
                if (fadeGroup == null)
                    fadeGroup = fadeCanvas.gameObject.AddComponent<CanvasGroup>();
            }
            if (fadeImage == null)
            {
                fadeImage = fadeCanvas.GetComponentInChildren<Image>(true);
                if (fadeImage == null)
                {
                    var imageGo = new GameObject("FadeImage");
                    imageGo.transform.SetParent(fadeCanvas.transform, false);
                    fadeImage = imageGo.AddComponent<Image>();
                    StretchFullScreen(fadeImage.rectTransform);
                }
            }
            if (fadeImage.sprite == null)
                fadeImage.sprite = MakeWhiteSprite();
            fadeImage.raycastTarget = false;
            fadeGroup.interactable = false;
        }

        private static void StretchFullScreen(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static Sprite MakeWhiteSprite()
        {
            var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            tex.SetPixel(0, 0, Color.white);
            tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f));
        }
    }
}
