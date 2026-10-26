using System;
using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Yumiwa.Presentation
{
    /// <summary>
    /// UI factory for PuzzleView — creates/reuses the overlay canvas + TMP_InputField + Submit + Feedback.
    /// Extracted from PuzzleView (439L → ~290L) per SIZE-001 ADR-0011 Opsi B. Pool: single cached panel, SetActive reuse 0B after first open.
    /// No business rules here — only uGUI construction and event wiring.
    /// </summary>
    public sealed class PuzzlePanelFactory
    {
        private GameObject cachedPanel;
        private TMP_InputField cachedInput;
        private TMP_Text cachedFeedback;

        /// <summary>Get or create panel. Returns true if panel ready; also outputs input/feedback for caller to bind cursor.</summary>
        public bool GetOrCreate(TMP_FontAsset panelFont, Action<string> onSubmit, Action<string> onInputSubmit, out GameObject panel, out TMP_InputField inputField, out TMP_Text feedbackText)
        {
            // Reuse cached panel (pooled, 0B)
            if (cachedPanel != null)
            {
                cachedPanel.SetActive(true);
                if (cachedInput != null)
                {
                    cachedInput.text = "";
                    if (cachedFeedback != null) cachedFeedback.text = "";
                    var es = EventSystem.current;
                    if (es != null) es.SetSelectedGameObject(cachedInput.gameObject);
                    cachedInput.Select();
                    cachedInput.ActivateInputField();
                }
                panel = cachedPanel;
                inputField = cachedInput;
                feedbackText = cachedFeedback;
                return true;
            }

            // Create new overlay canvas + panel
            var canvasGO = new GameObject("PuzzlePanel", typeof(RectTransform));
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();

            // Background panel
            var panelGO = new GameObject("Panel", typeof(RectTransform));
            panelGO.transform.SetParent(canvasGO.transform, false);
            var panelImg = panelGO.AddComponent<Image>();
            panelImg.color = new Color(0f, 0f, 0f, 0.8f);
            var panelRect = panelGO.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(400, 200);
            panelRect.anchoredPosition = Vector2.zero;

            // TMP_InputField
            var ifGO = new GameObject("TMP_InputField", typeof(RectTransform));
            ifGO.transform.SetParent(panelGO.transform, false);
            var ifBg = ifGO.AddComponent<Image>();
            ifBg.color = Color.white;
            var newInputField = ifGO.AddComponent<TMP_InputField>();
            newInputField.targetGraphic = ifBg;
            var ifRect = ifGO.GetComponent<RectTransform>();
            ifRect.anchorMin = new Vector2(0.5f, 1f);
            ifRect.anchorMax = new Vector2(0.5f, 1f);
            ifRect.pivot = new Vector2(0.5f, 1f);
            ifRect.anchoredPosition = new Vector2(0, -30);
            ifRect.sizeDelta = new Vector2(300, 40);

            // TMP_Text component
            var textGO = new GameObject("TMP_Text", typeof(RectTransform));
            textGO.transform.SetParent(ifGO.transform, false);
            var txt = textGO.AddComponent<TextMeshProUGUI>();
            txt.font = ResolveFont(panelFont);
            txt.fontSize = 20;
            txt.color = Color.black;
            txt.alignment = TextAlignmentOptions.Center;
            txt.richText = false;
            var txtRect = textGO.GetComponent<RectTransform>();
            txtRect.anchorMin = Vector2.zero; txtRect.anchorMax = Vector2.one;
            txtRect.offsetMin = Vector2.zero; txtRect.offsetMax = Vector2.zero;
            newInputField.textComponent = txt;

            // Placeholder
            var phGO = new GameObject("Placeholder", typeof(RectTransform));
            phGO.transform.SetParent(ifGO.transform, false);
            var phTxt = phGO.AddComponent<TextMeshProUGUI>();
            phTxt.font = ResolveFont(panelFont);
            phTxt.fontSize = 18;
            phTxt.color = Color.gray;
            phTxt.text = "Masukkan kode (contoh: 2347)";
            phTxt.alignment = TextAlignmentOptions.Center;
            var phRect = phGO.GetComponent<RectTransform>();
            phRect.anchorMin = Vector2.zero; phRect.anchorMax = Vector2.one;
            phRect.offsetMin = Vector2.zero; phRect.offsetMax = Vector2.zero;
            newInputField.placeholder = phTxt;

            // Submit button
            var btnGO = new GameObject("SubmitButton", typeof(RectTransform));
            btnGO.transform.SetParent(panelGO.transform, false);
            var btnImg = btnGO.AddComponent<Image>();
            btnImg.color = new Color(0.2f, 0.6f, 1f, 1f);
            var btn = btnGO.AddComponent<Button>();
            btn.targetGraphic = btnImg;
            var btnRect = btnGO.GetComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(0.5f, 0f); btnRect.anchorMax = new Vector2(0.5f, 0f);
            btnRect.pivot = new Vector2(0.5f, 0f);
            btnRect.anchoredPosition = new Vector2(0, 30);
            btnRect.sizeDelta = new Vector2(120, 40);
            var btnTxtGO = new GameObject("TMP_Text", typeof(RectTransform));
            btnTxtGO.transform.SetParent(btnGO.transform, false);
            var btnTxt = btnTxtGO.AddComponent<TextMeshProUGUI>();
            btnTxt.font = ResolveFont(panelFont);
            btnTxt.fontSize = 18; btnTxt.color = Color.white; btnTxt.text = "Submit"; btnTxt.alignment = TextAlignmentOptions.Center;
            var btnTxtRect = btnTxtGO.GetComponent<RectTransform>();
            btnTxtRect.anchorMin = Vector2.zero; btnTxtRect.anchorMax = Vector2.one;
            btnTxtRect.offsetMin = Vector2.zero; btnTxtRect.offsetMax = Vector2.zero;

            // Feedback text
            var fbGO = new GameObject("FeedbackText", typeof(RectTransform));
            fbGO.transform.SetParent(panelGO.transform, false);
            var fbTxt = fbGO.AddComponent<TextMeshProUGUI>();
            fbTxt.font = ResolveFont(panelFont);
            fbTxt.fontSize = 16; fbTxt.color = Color.yellow; fbTxt.alignment = TextAlignmentOptions.Center;
            var fbRect = fbGO.GetComponent<RectTransform>();
            fbRect.anchorMin = new Vector2(0.5f, 0.5f); fbRect.anchorMax = new Vector2(0.5f, 0.5f);
            fbRect.pivot = new Vector2(0.5f, 0.5f);
            fbRect.anchoredPosition = new Vector2(0, -90);
            fbRect.sizeDelta = new Vector2(350, 30);

            // Wire events to caller-provided handlers
            newInputField.onEndEdit.AddListener(onInputSubmit.Invoke);
            btn.onClick.AddListener(() => onSubmit(newInputField.text));

            cachedPanel = canvasGO;
            cachedInput = newInputField;
            cachedFeedback = fbTxt;

            panel = cachedPanel;
            inputField = cachedInput;
            feedbackText = cachedFeedback;
            return true;
        }

        public void Release(GameObject panel)
        {
            if (panel != null) panel.SetActive(false);
            // Keep cached refs for reuse; do not null pool entry
        }

        public void Clear()
        {
            if (cachedPanel != null) UnityEngine.Object.Destroy(cachedPanel);
            cachedPanel = null;
            cachedInput = null;
            cachedFeedback = null;
        }

        private static TMP_FontAsset ResolveFont(TMP_FontAsset panelFont)
        {
            return panelFont; // P2: Resources removed — assign via inspector/Addressables; null uses Unity default
        }
    }
}
