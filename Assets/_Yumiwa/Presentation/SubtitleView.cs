using UnityEngine;
using UnityEngine.UI;
using TMPro;
using VContainer;
using Yumiwa.Application;
using Yumiwa.Domain;

namespace Yumiwa.Presentation
{
    /// <summary>
    /// CoD-style non-blocking subtitle view per plan.json: bottom-center, max 2 visible
    /// lines, 60% width, 0.15s fades, "{Name}: {Text}" prefix colored from Speaker.
    /// Reuses one pooled TMP_Text (no per-line instantiate); text is rebuilt only when
    /// the active line changes, never per-frame (ARCH §8 0B). Timing/queue ownership
    /// stays in <see cref="DialogueQueueService"/> (ARCH-002).
    /// </summary>
    public sealed class SubtitleView : MonoBehaviour
    {
        [SerializeField] private TMP_Text subtitleText;
        [SerializeField] private Image background;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private TMP_FontAsset subtitleFont;
        [SerializeField] private float fadeDurationSeconds = 0.15f;
        [SerializeField] private float maxWidthPercentOfScreen = 60f;
        // Inspector-tunable font sizes (presentation tuning, designer-owned) <!-- TBD#6 -->
        [SerializeField] private int smallFontSize = 24;
        [SerializeField] private int mediumFontSize = 32;
        [SerializeField] private int largeFontSize = 42;

        private DialogueQueueService dialogue;
        private IEventBus bus;
        private SettingsData settings;
        private string displayedLineId;
        private float targetAlpha;

        [Inject]
        public void Construct(DialogueQueueService dialogue, IEventBus bus, SettingsData settings)
        {
            this.dialogue = dialogue;
            this.bus = bus;
            this.settings = settings;
        }

        private void Awake()
        {
            if (subtitleText == null)
                subtitleText = GetComponentInChildren<TMP_Text>(true);
            if (subtitleText == null)
                BuildMinimalUi();
            else
            {
                if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
                if (canvasGroup == null && subtitleText != null)
                {
                    var canvas = subtitleText.GetComponentInParent<Canvas>();
                    if (canvas != null) canvasGroup = canvas.gameObject.AddComponent<CanvasGroup>();
                }
                if (background == null && subtitleText != null)
                    background = subtitleText.GetComponentInChildren<Image>(true);
            }
            if (subtitleFont != null && subtitleText != null)
                subtitleText.font = subtitleFont;
            if (subtitleText != null)
            {
                subtitleText.text = "";
                subtitleText.maxVisibleLines = 2;
            }
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                targetAlpha = 0f;
            }
            ApplySettings(settings);
        }

        private void OnEnable()
        {
            if (bus != null)
            {
                bus.Subscribe<DialogueStarted>(OnDialogueStarted);
                bus.Subscribe<DialogueEnded>(OnDialogueEnded);
                bus.Subscribe<DialogueInterrupted>(OnDialogueInterrupted);
            }
        }

        private void OnDisable()
        {
            if (bus != null)
            {
                bus.Unsubscribe<DialogueStarted>(OnDialogueStarted);
                bus.Unsubscribe<DialogueEnded>(OnDialogueEnded);
                bus.Unsubscribe<DialogueInterrupted>(OnDialogueInterrupted);
            }
        }

        private void Update()
        {
            if (dialogue != null)
                dialogue.Tick(Time.time);
            if (canvasGroup != null && fadeDurationSeconds > 0f)
                canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, targetAlpha, Time.unscaledDeltaTime / fadeDurationSeconds);
        }

        /// <summary>Runtime settings apply (text size, background opacity, reading speed).</summary>
        public void ApplySettings(SettingsData next)
        {
            settings = next;
            if (subtitleText != null)
            {
                switch (settings.TextSize)
                {
                    case DialogueTextSize.Small: subtitleText.fontSize = smallFontSize; break;
                    case DialogueTextSize.Large: subtitleText.fontSize = largeFontSize; break;
                    default: subtitleText.fontSize = mediumFontSize; break;
                }
            }
            if (dialogue != null)
                dialogue.SetReadingSpeedMultiplier(settings.ReadingSpeed.ToMultiplier());
            RefreshVisibility();
        }

        private void OnDialogueStarted(DialogueStarted e)
        {
            displayedLineId = e.LineId;
            RefreshVisibility();
        }

        private void OnDialogueEnded(DialogueEnded e)
        {
            if (displayedLineId == e.LineId)
            {
                displayedLineId = null;
                targetAlpha = 0f;
            }
        }

        private void OnDialogueInterrupted(DialogueInterrupted e)
        {
            if (displayedLineId == e.LineId)
            {
                displayedLineId = null;
                targetAlpha = 0f;
            }
        }

        private void RefreshVisibility()
        {
            if (string.IsNullOrEmpty(displayedLineId) || !settings.SubtitlesEnabled)
            {
                targetAlpha = 0f;
                return;
            }
            if (dialogue?.ActiveLine is DialogueLine line && line.LineId == displayedLineId)
            {
                if (subtitleText != null)
                    subtitleText.text = FormatLine(line);
                if (background != null)
                {
                    var c = background.color;
                    c.a = settings.SubtitleBackgroundOpacity;
                    background.color = c;
                }
                targetAlpha = 1f;
            }
            else
            {
                targetAlpha = 0f;
            }
        }

        private string FormatLine(DialogueLine line)
        {
            string prefix = line.SpeakerId;
            string colorHex = "#FFFFFF";
            if (dialogue != null && dialogue.TryGetSpeaker(line.SpeakerId, out var speaker) && speaker.IsValid)
            {
                prefix = string.IsNullOrEmpty(speaker.DisplayName) ? speaker.SpeakerId : speaker.DisplayName;
                if (!string.IsNullOrEmpty(speaker.NameColorHex))
                    colorHex = speaker.NameColorHex;
            }
            return $"<color={colorHex}>{prefix}</color>: {line.Text}";
        }

        private void BuildMinimalUi()
        {
            var canvasGO = new GameObject("SubtitleView", typeof(RectTransform));
            canvasGO.transform.SetParent(transform, false);
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 120;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();
            canvasGroup = canvasGO.AddComponent<CanvasGroup>();
            var bgGO = new GameObject("SubtitleBG", typeof(RectTransform));
            bgGO.transform.SetParent(canvasGO.transform, false);
            background = bgGO.AddComponent<Image>();
            background.color = new Color(0, 0, 0, settings.SubtitleBackgroundOpacity);
            var bgRt = bgGO.GetComponent<RectTransform>();
            float width = Screen.width * maxWidthPercentOfScreen / 100f;
            bgRt.anchorMin = new Vector2(0.5f, 0f);
            bgRt.anchorMax = new Vector2(0.5f, 0f);
            bgRt.pivot = new Vector2(0.5f, 0f);
            bgRt.anchoredPosition = new Vector2(0, 60);
            bgRt.sizeDelta = new Vector2(width, 90);
            var textGO = new GameObject("SubtitleText", typeof(RectTransform));
            textGO.transform.SetParent(bgGO.transform, false);
            subtitleText = textGO.AddComponent<TextMeshProUGUI>();
            if (subtitleFont != null) subtitleText.font = subtitleFont;
            subtitleText.fontSize = mediumFontSize;
            subtitleText.color = Color.white;
            subtitleText.alignment = TextAlignmentOptions.Center;
            subtitleText.textWrappingMode = TextWrappingModes.Normal;
            subtitleText.maxVisibleLines = 2;
            subtitleText.text = "";
            var rt = textGO.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(12, 8);
            rt.offsetMax = new Vector2(-12, -8);
        }
    }
}
