using System;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace Yumiwa.Presentation
{
    /// <summary>
    /// Inspection HUD: item name/description, transient status message, and
    /// Take/Back actions. Forwards clicks as events; the controller decides.
    /// Subscriptions are added once in Start and removed on destroy.
    /// </summary>
    public sealed class ItemInspectionUI : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text descriptionText;
        [SerializeField] private TMP_Text messageText;
        [SerializeField] private TMP_Text hintText;
        [SerializeField] private Button takeButton;
        [SerializeField] private Button backButton;

        public event Action OnTakeClicked;
        public event Action OnBackClicked;

        private Canvas canvas;

        private void Awake()
        {
            canvas = GetComponent<Canvas>();
            hintText ??= BuildHintText();
            // Sembunyi sebelum frame pertama di-render: tidak ada flash fullscreen,
            // dan GameObject tetap aktif sehingga subscription tombol tetap hidup.
            SetVisible(false);
        }

        private void Start()
        {
            if (takeButton != null)
                takeButton.onClick.AddListener(HandleTake);
            if (backButton != null)
                backButton.onClick.AddListener(HandleBack);
            SetVisible(false);
        }

        private void OnDestroy()
        {
            if (takeButton != null)
                takeButton.onClick.RemoveListener(HandleTake);
            if (backButton != null)
                backButton.onClick.RemoveListener(HandleBack);
        }

        public void Bind(Yumiwa.Domain.ItemDefinition definition)
        {
            if (nameText != null)
                nameText.text = definition.DisplayName;
            if (descriptionText != null)
                descriptionText.text = definition.Description;
            ClearHint();
            ClearMessage();
        }

        public void ShowMessage(string message)
        {
            if (messageText != null)
            {
                messageText.text = message ?? "";
                messageText.gameObject.SetActive(!string.IsNullOrEmpty(message));
            }
        }

        public void ShowHint(string message)
        {
            if (hintText == null)
                return;
            hintText.text = message ?? "";
            hintText.gameObject.SetActive(!string.IsNullOrEmpty(message));
        }

        public void ClearMessage() => ShowMessage(null);
        public void ClearHint() => ShowHint(null);

        public void Show()
        {
            SetVisible(true);
        }

        public void Hide()
        {
            ClearHint();
            SetVisible(false);
        }

        /// <summary>
        /// Toggle lewat Canvas, bukan GameObject: rendering mati total
        /// (DimPanel + tombol + teks hilang dari layar) tapi component tetap
        /// aktif sehingga event Take/Back tidak pernah putus.
        /// </summary>
        private void SetVisible(bool visible)
        {
            if (panel != null && panel != gameObject)
                panel.SetActive(visible);
            if (canvas != null)
                canvas.enabled = visible;
        }

        private TMP_Text BuildHintText()
        {
            var host = panel != null ? panel.transform : transform;
            var go = new GameObject("HintText", typeof(RectTransform));
            go.transform.SetParent(host, false);
            var text = go.AddComponent<TextMeshProUGUI>();
            text.text = "";
            text.fontSize = 22;
            text.color = new Color(1f, 0.82f, 0.42f, 1f);
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.Normal;
            var rt = text.rectTransform;
            rt.anchorMin = new Vector2(0.5f, 0.08f);
            rt.anchorMax = new Vector2(0.5f, 0.08f);
            rt.sizeDelta = new Vector2(720, 44);
            rt.anchoredPosition = Vector2.zero;
            go.SetActive(false);
            return text;
        }

        private void HandleTake() => OnTakeClicked?.Invoke();
        private void HandleBack() => OnBackClicked?.Invoke();
    }
}
