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
        [SerializeField] private Button takeButton;
        [SerializeField] private Button backButton;

        public event Action OnTakeClicked;
        public event Action OnBackClicked;

        private Canvas canvas;

        private void Awake()
        {
            canvas = GetComponent<Canvas>();
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

        public void ClearMessage() => ShowMessage(null);

        public void Show()
        {
            SetVisible(true);
        }

        public void Hide()
        {
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

        private void HandleTake() => OnTakeClicked?.Invoke();
        private void HandleBack() => OnBackClicked?.Invoke();
    }
}
