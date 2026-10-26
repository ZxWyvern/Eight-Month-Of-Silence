using UnityEngine;
using TMPro;
using UnityEngine.UI;
using VContainer;
using Yumiwa.Domain;

namespace Yumiwa.Presentation
{
    /// <summary>
    /// Minimal Health HUD per docs/07 — shows health bar + wounded state.
    /// Guarded Resources fallback per ADR-0011 PATTERN-006.
    /// </summary>
    public sealed class HealthHudPresenter : MonoBehaviour
    {
        [SerializeField] private Slider slider;
        [SerializeField] private TMP_Text label;
        [SerializeField] private Image fillImage;
        [SerializeField] private TMP_FontAsset panelFont; // inspector-wirable, avoids Resources per-frame
        [SerializeField] private Color normalColor = Color.green;
        [SerializeField] private Color woundedColor = Color.red;

        private IEventBus bus;
        private IHealthService health;

        [Inject]
        public void Construct(IEventBus bus, IHealthService health)
        {
            this.bus = bus;
            this.health = health;
        }

        private TMP_FontAsset ResolveFont() => panelFont; // P2: Resources fallback removed — assign panelFont in inspector or via Addressables; null uses Unity default font

        private void Awake()
        {
            if (slider == null) slider = GetComponentInChildren<Slider>(true);
            if (label == null) label = GetComponentInChildren<TMP_Text>(true);
            if (fillImage == null && slider != null) fillImage = slider.fillRect != null ? slider.fillRect.GetComponent<Image>() : null;

            // Auto-create minimal HUD if scene has none (rework-friendly, like InteractionPromptPresenter)
            if (slider == null)
            {
                var canvasGO = new GameObject("HealthHud", typeof(RectTransform));
                canvasGO.transform.SetParent(transform, false);
                var canvas = canvasGO.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 105;
                canvasGO.AddComponent<CanvasScaler>();
                canvasGO.AddComponent<GraphicRaycaster>();
                var bgGO = new GameObject("Slider", typeof(RectTransform));
                bgGO.transform.SetParent(canvasGO.transform, false);
                var bgImg = bgGO.AddComponent<Image>();
                bgImg.color = new Color(0.2f,0.2f,0.2f,0.8f);
                slider = bgGO.AddComponent<Slider>();
                slider.minValue = 0f; slider.maxValue = 1f; slider.value = 1f;
                slider.direction = Slider.Direction.LeftToRight;
                var bgRt = bgGO.GetComponent<RectTransform>();
                bgRt.anchorMin = new Vector2(0.02f, 0.92f); bgRt.anchorMax = new Vector2(0.22f, 0.97f);
                bgRt.offsetMin = Vector2.zero; bgRt.offsetMax = Vector2.zero;
                var fillGO = new GameObject("Fill", typeof(RectTransform));
                fillGO.transform.SetParent(bgGO.transform, false);
                fillImage = fillGO.AddComponent<Image>();
                fillImage.color = normalColor;
                var fillRt = fillGO.GetComponent<RectTransform>();
                fillRt.anchorMin = Vector2.zero; fillRt.anchorMax = Vector2.one;
                fillRt.offsetMin = Vector2.zero; fillRt.offsetMax = Vector2.zero;
                slider.fillRect = fillRt;
                var handleGO = new GameObject("Handle", typeof(RectTransform));
                handleGO.transform.SetParent(bgGO.transform, false);
                slider.handleRect = handleGO.GetComponent<RectTransform>();
                slider.targetGraphic = fillImage;
                var lblGO = new GameObject("Label", typeof(RectTransform));
                lblGO.transform.SetParent(bgGO.transform, false);
                label = lblGO.AddComponent<TextMeshProUGUI>();
                var f = ResolveFont();
                if (f != null) label.font = f;
                label.fontSize = 14; label.color = Color.white; label.alignment = TextAlignmentOptions.Center;
                var lblRt = lblGO.GetComponent<RectTransform>();
                lblRt.anchorMin = Vector2.zero; lblRt.anchorMax = Vector2.one;
                lblRt.offsetMin = Vector2.zero; lblRt.offsetMax = Vector2.zero;
                slider = bgGO.GetComponent<Slider>();
            }
        }

        private void OnEnable()
        {
            bus?.Subscribe<HealthChangedEvent>(OnHealthChanged);
            bus?.Subscribe<PlayerWoundedEvent>(OnWounded);
            bus?.Subscribe<PlayerHealedEvent>(OnHealed);
            RefreshFromHealth();
        }

        private void OnDisable()
        {
            bus?.Unsubscribe<HealthChangedEvent>(OnHealthChanged);
            bus?.Unsubscribe<PlayerWoundedEvent>(OnWounded);
            bus?.Unsubscribe<PlayerHealedEvent>(OnHealed);
        }

        private void OnHealthChanged(HealthChangedEvent e)
        {
            if (slider != null && e.Max > 0f) slider.value = e.Current / e.Max;
            if (label != null) label.text = $"{e.Current:0}/{e.Max:0}";
            if (fillImage != null) fillImage.color = e.Current <= health.WoundThreshold ? woundedColor : normalColor;
        }

        private void OnWounded(PlayerWoundedEvent e)
        {
            if (fillImage != null) fillImage.color = woundedColor;
            if (label != null) label.text += " WOUNDED";
        }

        private void OnHealed(PlayerHealedEvent e)
        {
            if (fillImage != null && health != null && !health.IsWounded) fillImage.color = normalColor;
            RefreshFromHealth();
        }

        private void RefreshFromHealth()
        {
            if (health == null) return;
            if (slider != null && health.Max > 0f) slider.value = health.Current / health.Max;
            if (label != null) label.text = $"{health.Current:0}/{health.Max:0}";
            if (fillImage != null) fillImage.color = health.IsWounded ? woundedColor : normalColor;
        }
    }
}
