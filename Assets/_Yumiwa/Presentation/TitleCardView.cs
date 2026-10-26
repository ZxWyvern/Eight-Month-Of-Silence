using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace Yumiwa.Presentation
{
    /// <summary>
    /// Title-card view per plan.json presentation: time-based typewriter reveal
    /// (18 chars/sec), hold-button skip. Pure Presentation: no gameplay state, no bus,
    /// no input-map reads — skip is a UI hold wired via EventTrigger ( ARCH §6 ).
    /// Text is rebuilt only while the visible char count changes (no per-frame churn).
    /// </summary>
    public sealed class TitleCardView : MonoBehaviour
    {
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text subtitleText;
        [SerializeField] private Button skipButton;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private TMP_FontAsset titleFont;
        [SerializeField] private float charactersPerSecond = 18f;
        [SerializeField] private bool skippable = true;
        [SerializeField] private float holdToSkipSeconds = 0.6f;

        private string fullTitle = "";
        private string fullSubtitle = "";
        private float elapsed;
        private int shownChars = -1;
        private bool pointerHeld;
        private float holdTime;

        public bool IsShowing => canvasGroup != null && canvasGroup.alpha > 0.01f;

        private void Awake()
        {
            if (titleText == null)
                titleText = GetComponentInChildren<TMP_Text>(true);
            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup != null)
                canvasGroup.alpha = 0f;
            if (titleFont != null && titleText != null)
                titleText.font = titleFont;
            WireSkipHold();
        }

        private void Update()
        {
            if (!IsShowing)
                return;
            elapsed += Time.unscaledDeltaTime;
            int total = FullText().Length;
            int want = charactersPerSecond > 0f ? Mathf.Min(total, Mathf.FloorToInt(elapsed * charactersPerSecond)) : total;
            if (want != shownChars)
            {
                shownChars = want;
                RenderPartial(want);
            }
            if (skippable && pointerHeld)
            {
                holdTime += Time.unscaledDeltaTime;
                if (holdTime >= holdToSkipSeconds)
                {
                    pointerHeld = false;
                    Hide();
                }
            }
        }

        public void ShowTitle(string title, string subtitle = null)
        {
            fullTitle = title ?? "";
            fullSubtitle = subtitle ?? "";
            elapsed = 0f;
            shownChars = -1;
            holdTime = 0f;
            if (canvasGroup != null)
                canvasGroup.alpha = 1f;
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            if (canvasGroup != null)
                canvasGroup.alpha = 0f;
            shownChars = -1;
        }

        private string FullText() => string.IsNullOrEmpty(fullSubtitle) ? fullTitle : fullTitle + "\n" + fullSubtitle;

        private void RenderPartial(int charCount)
        {
            string full = FullText();
            string visible = charCount >= full.Length ? full : full.Substring(0, charCount);
            if (titleText != null && subtitleText == null)
            {
                titleText.text = visible;
                return;
            }
            if (titleText != null)
                titleText.text = charCount >= fullTitle.Length ? fullTitle : visible.Length <= fullTitle.Length ? visible : fullTitle;
            if (subtitleText != null)
            {
                int subShown = charCount - fullTitle.Length - 1;
                subtitleText.text = subShown <= 0 ? "" : subShown >= fullSubtitle.Length ? fullSubtitle : fullSubtitle.Substring(0, subShown);
            }
        }

        private void WireSkipHold()
        {
            if (skipButton == null || !skippable)
                return;
            var trigger = skipButton.gameObject.GetComponent<EventTrigger>();
            if (trigger == null)
                trigger = skipButton.gameObject.AddComponent<EventTrigger>();
            var down = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
            down.callback.AddListener(_ => { pointerHeld = true; holdTime = 0f; });
            var up = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
            up.callback.AddListener(_ => { pointerHeld = false; holdTime = 0f; });
            trigger.triggers.Add(down);
            trigger.triggers.Add(up);
        }
    }
}
