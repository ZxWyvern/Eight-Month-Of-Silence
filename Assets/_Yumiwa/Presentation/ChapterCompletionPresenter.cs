using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using VContainer;
using Yumiwa.Application;
using Yumiwa.Domain;

namespace Yumiwa.Presentation
{
    public sealed class ChapterCompletionPresenter : MonoBehaviour
    {
        [SerializeField] private float autoReturnSeconds = 8f;

        private IEventBus bus;
        private ISceneLoader loader;
        private InputActionAsset inputAsset;
        private InputActionMap playerMap;
        private GameObject panel;
        private TMP_Text bodyText;
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

        private void Awake()
        {
            BuildUi();
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
            if (panel != null)
                panel.SetActive(true);
            if (bodyText != null)
                bodyText.text = "Kasus dibuka. Jejak mengarah pada satu nama.\n\nLanjut ke Menu Utama.";
            if (inputAsset != null)
            {
                playerMap = inputAsset.FindActionMap("Player", false);
                playerMap?.Disable();
            }
            Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            StartCoroutine(AutoReturn());
        }

        private IEnumerator AutoReturn()
        {
            yield return new WaitForSecondsRealtime(Mathf.Max(1f, autoReturnSeconds));
            ReturnToMenu();
        }

        private void ReturnToMenu()
        {
            if (returning)
                return;
            returning = true;
            StopAllCoroutines();
            Time.timeScale = 1f;
            playerMap?.Enable();
            if (loader == null)
            {
                SceneManager.LoadScene("MainMenu");
                return;
            }
            StartCoroutine(LoadMenu());
        }

        private IEnumerator LoadMenu()
        {
            var task = loader.LoadSceneAsync("MainMenu");
            while (task != null && !task.IsCompleted)
                yield return null;
            if (task != null && task.IsFaulted)
                Debug.LogError($"[Yumiwa] Failed to load MainMenu: {task.Exception}");
        }

        private void BuildUi()
        {
            var canvasGO = new GameObject("ChapterCompletionCanvas", typeof(RectTransform));
            canvasGO.transform.SetParent(transform, false);
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 210;
            canvasGO.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasGO.AddComponent<GraphicRaycaster>();

            panel = new GameObject("Panel", typeof(RectTransform));
            panel.transform.SetParent(canvasGO.transform, false);
            var panelImage = panel.AddComponent<Image>();
            panelImage.color = new Color(0.015f, 0.018f, 0.022f, 0.96f);
            var panelRt = panel.GetComponent<RectTransform>();
            panelRt.anchorMin = Vector2.zero;
            panelRt.anchorMax = Vector2.one;
            panelRt.offsetMin = Vector2.zero;
            panelRt.offsetMax = Vector2.zero;

            var title = MakeText(panel.transform, "Title", "BAB 1 SELESAI", 58, FontStyles.Bold);
            var titleRt = title.rectTransform;
            titleRt.anchorMin = new Vector2(0.5f, 0.68f);
            titleRt.anchorMax = new Vector2(0.5f, 0.68f);
            titleRt.sizeDelta = new Vector2(900, 90);
            titleRt.anchoredPosition = Vector2.zero;
            title.alignment = TextAlignmentOptions.Center;

            bodyText = MakeText(panel.transform, "Body", "", 26, FontStyles.Normal);
            var bodyRt = bodyText.rectTransform;
            bodyRt.anchorMin = new Vector2(0.5f, 0.5f);
            bodyRt.anchorMax = new Vector2(0.5f, 0.5f);
            bodyRt.sizeDelta = new Vector2(820, 180);
            bodyRt.anchoredPosition = Vector2.zero;
            bodyText.alignment = TextAlignmentOptions.Center;
            bodyText.color = new Color(0.84f, 0.86f, 0.88f, 1f);

            var buttonGO = new GameObject("ReturnButton", typeof(RectTransform));
            buttonGO.transform.SetParent(panel.transform, false);
            var buttonImage = buttonGO.AddComponent<Image>();
            buttonImage.color = new Color(0.32f, 0.06f, 0.06f, 1f);
            var button = buttonGO.AddComponent<Button>();
            button.targetGraphic = buttonImage;
            var buttonRt = buttonGO.GetComponent<RectTransform>();
            buttonRt.anchorMin = new Vector2(0.5f, 0.28f);
            buttonRt.anchorMax = new Vector2(0.5f, 0.28f);
            buttonRt.sizeDelta = new Vector2(360, 64);
            buttonRt.anchoredPosition = Vector2.zero;
            var buttonText = MakeText(buttonGO.transform, "Label", "Kembali ke Menu Utama", 24, FontStyles.Bold);
            var labelRt = buttonText.rectTransform;
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = Vector2.zero;
            labelRt.offsetMax = Vector2.zero;
            buttonText.alignment = TextAlignmentOptions.Center;
            button.onClick.AddListener(ReturnToMenu);
            panel.SetActive(false);
        }

        private static TMP_Text MakeText(Transform parent, string name, string value, float size, FontStyles style)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var text = go.AddComponent<TextMeshProUGUI>();
            text.text = value;
            text.fontSize = size;
            text.fontStyle = style;
            text.color = Color.white;
            text.enableWordWrapping = true;
            return text;
        }
    }
}
