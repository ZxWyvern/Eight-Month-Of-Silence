using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Yumiwa.Domain;

namespace Yumiwa.Presentation
{
    /// <summary>
    /// Self-built pause/settings view (MVP-01). Owns only presentation and the
    /// settings controls; gameplay rules stay in PauseMenuPresenter. Kept out of
    /// the MonoBehaviour so the presenter stays below SIZE-001.
    /// </summary>
    public sealed class PauseMenuUi
    {
        private readonly GameObject panel;
        private readonly Slider sensitivitySlider;
        private readonly Slider fovSlider;
        private readonly Slider subtitleOpacitySlider;
        private readonly TMP_Text sensitivityValue;
        private readonly TMP_Text fovValue;
        private readonly TMP_Text subtitleOpacityValue;
        private readonly Toggle headBobToggle;
        private readonly Toggle shakeToggle;
        private readonly Toggle fovBoostToggle;
        private readonly Toggle subtitlesToggle;
        private readonly TMP_Text textSizeLabel;
        private readonly TMP_Text readingSpeedLabel;
        private readonly Action<SettingsData> settingsChanged;

        private SettingsData current;

        public PauseMenuUi(
            Transform parent,
            SettingsData initial,
            Action resume,
            Action load,
            Action newGame,
            Action mainMenu,
            Action<SettingsData> settingsChanged)
        {
            current = initial;
            this.settingsChanged = settingsChanged;

            var canvasGO = new GameObject("PauseCanvas");
            canvasGO.transform.SetParent(parent, false);
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 60;
            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGO.AddComponent<GraphicRaycaster>();

            panel = new GameObject("Panel");
            panel.transform.SetParent(canvasGO.transform, false);
            var bg = panel.AddComponent<Image>();
            bg.color = new Color(0.04f, 0.04f, 0.06f, 0.95f);
            var rt = panel.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;

            var title = MakeText(panel.transform, "Title", new Vector2(0, 290), new Vector2(600, 54), 34);
            title.text = "Jeda — Act 1";

            MakeButton(panel.transform, "Resume", new Vector2(0, 205), "Lanjutkan (P)", resume);
            MakeButton(panel.transform, "Load", new Vector2(0, 145), "Muat Checkpoint", load);
            MakeButton(panel.transform, "New", new Vector2(0, 85), "Mulai Baru", newGame);
            MakeButton(panel.transform, "MainMenu", new Vector2(0, 25), "Menu Utama", mainMenu);

            var st = MakeText(panel.transform, "SettingsTitle", new Vector2(0, -35), new Vector2(600, 40), 24);
            st.text = "Pengaturan";

            sensitivitySlider = MakeSlider(panel.transform, "Sens", new Vector2(0, -90),
                0.2f, 3f, initial.MouseSensitivity, "Sensitivitas", out sensitivityValue);
            fovSlider = MakeSlider(panel.transform, "Fov", new Vector2(0, -145),
                60f, 90f, initial.Fov, "FOV", out fovValue);
            subtitleOpacitySlider = MakeSlider(panel.transform, "SubsBg", new Vector2(0, -340),
                0f, 1f, initial.SubtitleBackgroundOpacity, "Latar Subtitle", out subtitleOpacityValue);

            headBobToggle = MakeToggle(panel.transform, "Bob", new Vector2(-160, -200),
                "Head Bob", initial.HeadBobEnabled);
            shakeToggle = MakeToggle(panel.transform, "Shake", new Vector2(160, -200),
                "Shake", initial.CameraShakeEnabled);
            fovBoostToggle = MakeToggle(panel.transform, "Boost", new Vector2(-160, -245),
                "FOV Boost", initial.FovBoostEnabled);
            subtitlesToggle = MakeToggle(panel.transform, "Subs", new Vector2(160, -245),
                "Subtitle", initial.SubtitlesEnabled);

            var textSizeButton = MakeOptionButton(panel.transform, "TextSize", new Vector2(-160, -285),
                out textSizeLabel);
            textSizeButton.onClick.AddListener(CycleTextSize);
            var readingSpeedButton = MakeOptionButton(panel.transform, "ReadSpeed", new Vector2(160, -285),
                out readingSpeedLabel);
            readingSpeedButton.onClick.AddListener(CycleReadingSpeed);

            ApplySettings(initial);
            panel.SetActive(false);
        }

        public void SetVisible(bool visible) => panel.SetActive(visible);

        public void ApplySettings(SettingsData next)
        {
            current = next;
            sensitivitySlider.SetValueWithoutNotify(next.MouseSensitivity);
            fovSlider.SetValueWithoutNotify(next.Fov);
            subtitleOpacitySlider.SetValueWithoutNotify(next.SubtitleBackgroundOpacity);
            sensitivityValue.text = next.MouseSensitivity.ToString("0.0");
            fovValue.text = next.Fov.ToString("0");
            subtitleOpacityValue.text = next.SubtitleBackgroundOpacity.ToString("0.00");
            headBobToggle.SetIsOnWithoutNotify(next.HeadBobEnabled);
            shakeToggle.SetIsOnWithoutNotify(next.CameraShakeEnabled);
            fovBoostToggle.SetIsOnWithoutNotify(next.FovBoostEnabled);
            subtitlesToggle.SetIsOnWithoutNotify(next.SubtitlesEnabled);
            textSizeLabel.text = "Teks: " + TextSizeName(next.TextSize);
            readingSpeedLabel.text = "Baca: " + ReadingSpeedName(next.ReadingSpeed);
        }

        private void NotifyChanged()
        {
            current = new SettingsData(
                headBobToggle.isOn,
                shakeToggle.isOn,
                fovBoostToggle.isOn,
                subtitlesToggle.isOn,
                sensitivitySlider.value,
                fovSlider.value,
                current.TextSize,
                subtitleOpacitySlider.value,
                current.ReadingSpeed);
            settingsChanged?.Invoke(current);
            ApplySettings(current);
        }

        private void CycleTextSize()
        {
            current = new SettingsData(
                current.HeadBobEnabled,
                current.CameraShakeEnabled,
                current.FovBoostEnabled,
                current.SubtitlesEnabled,
                current.MouseSensitivity,
                current.Fov,
                (DialogueTextSize)(((int)current.TextSize + 1) % 3),
                current.SubtitleBackgroundOpacity,
                current.ReadingSpeed);
            settingsChanged?.Invoke(current);
            ApplySettings(current);
        }

        private void CycleReadingSpeed()
        {
            current = new SettingsData(
                current.HeadBobEnabled,
                current.CameraShakeEnabled,
                current.FovBoostEnabled,
                current.SubtitlesEnabled,
                current.MouseSensitivity,
                current.Fov,
                current.TextSize,
                current.SubtitleBackgroundOpacity,
                (DialogueReadingSpeed)(((int)current.ReadingSpeed + 1) % 3));
            settingsChanged?.Invoke(current);
            ApplySettings(current);
        }

        private static string TextSizeName(DialogueTextSize size)
        {
            switch (size)
            {
                case DialogueTextSize.Small: return "Kecil";
                case DialogueTextSize.Large: return "Besar";
                default: return "Sedang";
            }
        }

        private static string ReadingSpeedName(DialogueReadingSpeed speed)
        {
            switch (speed)
            {
                case DialogueReadingSpeed.Slow: return "Lambat";
                case DialogueReadingSpeed.Fast: return "Cepat";
                default: return "Normal";
            }
        }

        private void MakeButton(Transform parent, string name, Vector2 anchored, string label, Action onClick)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var crt = go.AddComponent<RectTransform>();
            crt.anchorMin = new Vector2(0.5f, 0.5f);
            crt.anchorMax = new Vector2(0.5f, 0.5f);
            crt.sizeDelta = new Vector2(360, 48);
            crt.anchoredPosition = anchored;
            var img = go.AddComponent<Image>();
            img.color = new Color(0.16f, 0.16f, 0.22f, 1f);
            var button = go.AddComponent<Button>();
            button.targetGraphic = img;
            button.onClick.AddListener(() => onClick());
            var text = MakeText(go.transform, "Label", Vector2.zero, new Vector2(340, 44), 20);
            text.text = label;
        }

        private Button MakeOptionButton(Transform parent, string name, Vector2 anchored, out TMP_Text label)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var crt = go.AddComponent<RectTransform>();
            crt.anchorMin = new Vector2(0.5f, 0.5f);
            crt.anchorMax = new Vector2(0.5f, 0.5f);
            crt.sizeDelta = new Vector2(250, 34);
            crt.anchoredPosition = anchored;
            var img = go.AddComponent<Image>();
            img.color = new Color(0.1f, 0.1f, 0.14f, 1f);
            var button = go.AddComponent<Button>();
            button.targetGraphic = img;
            label = MakeText(go.transform, "Label", Vector2.zero, new Vector2(230, 30), 18);
            return button;
        }

        private Slider MakeSlider(Transform parent, string name, Vector2 anchored,
            float min, float max, float value, string caption, out TMP_Text valueLabel)
        {
            var cap = MakeText(parent, name + "Cap", anchored + new Vector2(-240, 0), new Vector2(180, 32), 18);
            cap.alignment = TextAlignmentOptions.MidlineRight;
            cap.text = caption;
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var crt = go.AddComponent<RectTransform>();
            crt.anchorMin = new Vector2(0.5f, 0.5f);
            crt.anchorMax = new Vector2(0.5f, 0.5f);
            crt.sizeDelta = new Vector2(320, 22);
            crt.anchoredPosition = anchored + new Vector2(70, 0);
            var img = go.AddComponent<Image>();
            img.color = new Color(0.1f, 0.1f, 0.14f, 1f);
            var slider = go.AddComponent<Slider>();
            slider.targetGraphic = img;
            slider.minValue = min;
            slider.maxValue = max;
            slider.value = value;
            slider.onValueChanged.AddListener(_ => NotifyChanged());

            var fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(go.transform, false);
            var fillAreaRt = fillArea.AddComponent<RectTransform>();
            fillAreaRt.anchorMin = Vector2.zero;
            fillAreaRt.anchorMax = Vector2.one;
            fillAreaRt.offsetMin = new Vector2(5, 5);
            fillAreaRt.offsetMax = new Vector2(-5, -5);
            var fill = new GameObject("Fill");
            fill.transform.SetParent(fillArea.transform, false);
            var fillRt = fill.AddComponent<RectTransform>();
            fillRt.sizeDelta = Vector2.zero;
            fillRt.anchoredPosition = Vector2.zero;
            var fillImage = fill.AddComponent<Image>();
            fillImage.color = new Color(0.35f, 0.62f, 0.85f, 1f);
            fillImage.raycastTarget = false;
            slider.fillRect = fill.GetComponent<RectTransform>();

            var handleArea = new GameObject("Handle Area");
            handleArea.transform.SetParent(go.transform, false);
            var handleAreaRt = handleArea.AddComponent<RectTransform>();
            handleAreaRt.anchorMin = Vector2.zero;
            handleAreaRt.anchorMax = Vector2.one;
            handleAreaRt.offsetMin = new Vector2(5, 0);
            handleAreaRt.offsetMax = new Vector2(-5, 0);
            var handle = new GameObject("Handle");
            handle.transform.SetParent(handleArea.transform, false);
            var handleRt = handle.AddComponent<RectTransform>();
            handleRt.sizeDelta = new Vector2(16, 26);
            var handleImage = handle.AddComponent<Image>();
            handleImage.color = new Color(0.9f, 0.9f, 0.95f, 1f);
            handleImage.raycastTarget = false;
            slider.handleRect = handleRt;
            slider.targetGraphic = handleImage;

            valueLabel = MakeText(parent, name + "Val", anchored + new Vector2(270, 0), new Vector2(80, 32), 18);
            valueLabel.alignment = TextAlignmentOptions.MidlineLeft;
            return slider;
        }

        private Toggle MakeToggle(Transform parent, string name, Vector2 anchored, string caption, bool value)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var crt = go.AddComponent<RectTransform>();
            crt.anchorMin = new Vector2(0.5f, 0.5f);
            crt.anchorMax = new Vector2(0.5f, 0.5f);
            crt.sizeDelta = new Vector2(250, 34);
            crt.anchoredPosition = anchored;
            var img = go.AddComponent<Image>();
            img.color = new Color(0.1f, 0.1f, 0.14f, 1f);
            var toggle = go.AddComponent<Toggle>();
            toggle.targetGraphic = img;
            toggle.isOn = value;
            toggle.onValueChanged.AddListener(_ => NotifyChanged());

            var checkGO = new GameObject("Checkmark");
            checkGO.transform.SetParent(go.transform, false);
            var checkRt = checkGO.AddComponent<RectTransform>();
            checkRt.anchorMin = new Vector2(0f, 0.5f);
            checkRt.anchorMax = new Vector2(0f, 0.5f);
            checkRt.sizeDelta = new Vector2(18, 18);
            checkRt.anchoredPosition = new Vector2(14, 0);
            var check = checkGO.AddComponent<Image>();
            check.color = new Color(0.55f, 0.85f, 0.62f, 1f);
            toggle.graphic = check;

            var cap = MakeText(go.transform, "Cap", new Vector2(22, 0), new Vector2(210, 30), 18);
            cap.alignment = TextAlignmentOptions.MidlineLeft;
            cap.text = caption;
            return toggle;
        }

        private static TMP_Text MakeText(Transform parent, string name, Vector2 anchored, Vector2 size, int fontSize)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var crt = go.AddComponent<RectTransform>();
            crt.anchorMin = new Vector2(0.5f, 0.5f);
            crt.anchorMax = new Vector2(0.5f, 0.5f);
            crt.sizeDelta = size;
            crt.anchoredPosition = anchored;
            var text = go.AddComponent<TextMeshProUGUI>();
            text.fontSize = fontSize;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            return text;
        }
    }
}
