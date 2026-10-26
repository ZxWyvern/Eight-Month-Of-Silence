using UnityEngine;
using UnityEngine.UI;
using TMPro;
using VContainer;
using Yumiwa.Application;
using Yumiwa.Domain;

namespace Yumiwa.Presentation
{
    /// <summary>
    /// Deduction board (B1-03): evidence checklist with documentation state plus
    /// conclusion options. Opens with N (prototype binding; remap deferred per
    /// docs/07). Time keeps running — the board is modal for movement only.
    /// On a correct conclusion the chapter is saved from the player pose.
    /// Self-builds its UI (precedent: HealthHudPresenter) so no prefab is needed.
    /// </summary>
    public sealed class ReconstructionView : MonoBehaviour
    {
        [SerializeField] private string caseId = "case_b1";
        [Tooltip("Player transform used for the chapter-save pose on solve.")]
        [SerializeField] private Transform playerTransform;

        private ReconstructionService recon;
        private IEvidenceLog log;
        private InspectionSessionService inspection;
        private IEventBus bus;
        private ICheckpointService checkpoint;
        private IHealthService health;
        private IStaminaService stamina;

        private bool inspectionOpen;
        private bool inventoryOpen;
        private bool paused;
        private bool open;

        private GameObject panel;
        private TMP_Text titleText;
        private TMP_Text checklistText;
        private TMP_Text feedbackText;
        private Button[] optionButtons = new Button[0];
        private TMP_Text[] optionLabels = new TMP_Text[0];

        private System.Action<InspectionStartedEvent> inspectionStartedHandler;
        private System.Action<InspectionEndedEvent> inspectionEndedHandler;
        private System.Action<InventoryOpenedEvent> inventoryOpenedHandler;
        private System.Action<InventoryClosedEvent> inventoryClosedHandler;
        private System.Action<PauseOpenedEvent> pauseOpenedHandler;
        private System.Action<PauseClosedEvent> pauseClosedHandler;

        public bool IsOpen => open;

        [Inject]
        public void Construct(
            ReconstructionService recon,
            IEvidenceLog log,
            InspectionSessionService inspection,
            IEventBus bus,
            ICheckpointService checkpoint,
            IHealthService health,
            IStaminaService stamina)
        {
            this.recon = recon;
            this.log = log;
            this.inspection = inspection;
            this.bus = bus;
            this.checkpoint = checkpoint;
            this.health = health;
            this.stamina = stamina;
        }

        private void Awake() => BuildUi();

        private void OnEnable()
        {
            if (bus == null) return;
            if (inspectionStartedHandler == null) inspectionStartedHandler = _ => inspectionOpen = true;
            if (inspectionEndedHandler == null) inspectionEndedHandler = _ => inspectionOpen = false;
            if (inventoryOpenedHandler == null) inventoryOpenedHandler = _ => inventoryOpen = true;
            if (inventoryClosedHandler == null) inventoryClosedHandler = _ => inventoryOpen = false;
            if (pauseOpenedHandler == null) pauseOpenedHandler = _ => paused = true;
            if (pauseClosedHandler == null) pauseClosedHandler = _ => paused = false;
            bus.Subscribe(inspectionStartedHandler);
            bus.Subscribe(inspectionEndedHandler);
            bus.Subscribe(inventoryOpenedHandler);
            bus.Subscribe(inventoryClosedHandler);
            bus.Subscribe(pauseOpenedHandler);
            bus.Subscribe(pauseClosedHandler);
        }

        private void OnDisable()
        {
            if (bus == null) return;
            bus.Unsubscribe(inspectionStartedHandler);
            bus.Unsubscribe(inspectionEndedHandler);
            bus.Unsubscribe(inventoryOpenedHandler);
            bus.Unsubscribe(inventoryClosedHandler);
            bus.Unsubscribe(pauseOpenedHandler);
            bus.Unsubscribe(pauseClosedHandler);
            if (open) SetOpen(false);
        }

        private void Update()
        {
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb == null) return;
            if (!kb.nKey.wasPressedThisFrame) return;
            if (inspectionOpen || inventoryOpen || paused) return; // other modals own the keyboard
            SetOpen(!open);
        }

        public void SetOpen(bool value)
        {
            if (value && (recon == null || !recon.HasCase(caseId)))
            {
                Debug.LogWarning($"[Yumiwa] Reconstruction case '{caseId}' unavailable.", this);
                return;
            }
            open = value;
            if (panel != null) panel.SetActive(open);
            if (open)
            {
                Refresh();
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                bus?.Publish(new ReconstructionOpenedEvent(caseId));
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                bus?.Publish(new ReconstructionClosedEvent(caseId));
            }
        }

        private void Refresh()
        {
            if (recon == null || !recon.TryGetCase(caseId, out var c)) return;
            if (titleText != null) titleText.text = "Rekonstruksi: " + c.Title;
            if (checklistText != null)
            {
                var sb = new System.Text.StringBuilder();
                for (int i = 0; i < c.RequiredEvidenceIds.Length; i++)
                {
                    string id = c.RequiredEvidenceIds[i];
                    bool have = log != null && log.HasEvidence(id);
                    int found = inspection != null ? inspection.GetDiscoveredHotspotCount(id) : 0;
                    string mark = have && found >= c.MinHotspotsPerEvidence ? "[x]" : "[ ]";
                    sb.AppendLine($"{mark} {id} ({found}/{c.MinHotspotsPerEvidence})");
                }
                checklistText.text = sb.ToString();
            }
            int n = c.OptionIds != null ? c.OptionIds.Length : 0;
            for (int i = 0; i < optionButtons.Length; i++)
            {
                bool show = i < n;
                if (optionButtons[i] != null) optionButtons[i].gameObject.SetActive(show);
                if (show && optionLabels[i] != null)
                    optionLabels[i].text = $"{i + 1}. {c.OptionTexts[i]}";
            }
            if (feedbackText != null)
                feedbackText.text = recon.IsSolved(caseId)
                    ? "Deduksi sudah tercatat di Evidence Log."
                    : "Pilih kesimpulan yang didukung bukti. Salah = coba lagi.";
        }

        private void OnOptionClicked(int index)
        {
            if (recon == null || !recon.TryGetCase(caseId, out var c)) return;
            if (index < 0 || index >= c.OptionIds.Length) return;
            bool ok = recon.TryConclude(caseId, c.OptionIds[index], out string feedback);
            if (feedbackText != null) feedbackText.text = feedback;
            Refresh();
            if (ok) SaveChapter();
        }

        private void SaveChapter()
        {
            if (checkpoint == null || playerTransform == null) return;
            var t = playerTransform;
            checkpoint.SaveChapter(new CheckpointData
            {
                PosX = t.position.x,
                PosY = t.position.y,
                PosZ = t.position.z,
                RotX = t.rotation.x,
                RotY = t.rotation.y,
                RotZ = t.rotation.z,
                RotW = t.rotation.w,
                Health = health != null ? health.Current : 100f,
                Stamina = stamina != null ? stamina.Current : 100f,
                ActiveScene = gameObject.scene.name,
                Timestamp = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            });
            Debug.Log("[Yumiwa] Chapter saved on correct deduction.", this);
        }

        private void BuildUi()
        {
            var canvasGO = new GameObject("ReconstructionCanvas");
            canvasGO.transform.SetParent(transform, false);
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();

            panel = new GameObject("Panel");
            panel.transform.SetParent(canvasGO.transform, false);
            var bg = panel.AddComponent<Image>();
            bg.color = new Color(0.05f, 0.05f, 0.08f, 0.94f);
            var rt = panel.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(640f, 480f);
            rt.anchoredPosition = Vector2.zero;

            titleText = MakeText(panel.transform, "Title", new Vector2(0, 190), new Vector2(600, 40), 22);
            checklistText = MakeText(panel.transform, "Checklist", new Vector2(0, 90), new Vector2(600, 130), 16);
            feedbackText = MakeText(panel.transform, "Feedback", new Vector2(0, -170), new Vector2(600, 60), 15);

            optionButtons = new Button[4];
            optionLabels = new TMP_Text[4];
            for (int i = 0; i < 4; i++)
            {
                int idx = i;
                var btnGO = new GameObject("Option" + i);
                btnGO.transform.SetParent(panel.transform, false);
                var btnRt = btnGO.AddComponent<RectTransform>();
                btnRt.anchorMin = new Vector2(0.5f, 0.5f);
                btnRt.anchorMax = new Vector2(0.5f, 0.5f);
                btnRt.sizeDelta = new Vector2(600, 44);
                btnRt.anchoredPosition = new Vector2(0, 10 - i * 50);
                var img = btnGO.AddComponent<Image>();
                img.color = new Color(0.16f, 0.16f, 0.22f, 1f);
                var btn = btnGO.AddComponent<Button>();
                btn.onClick.AddListener(() => OnOptionClicked(idx));
                var label = MakeText(btnGO.transform, "Label", Vector2.zero, new Vector2(580, 40), 15);
                label.alignment = TextAlignmentOptions.MidlineLeft;
                optionButtons[i] = btn;
                optionLabels[i] = label;
            }

            var closeGO = new GameObject("Close");
            closeGO.transform.SetParent(panel.transform, false);
            var closeRt = closeGO.AddComponent<RectTransform>();
            closeRt.anchorMin = new Vector2(0.5f, 0.5f);
            closeRt.anchorMax = new Vector2(0.5f, 0.5f);
            closeRt.sizeDelta = new Vector2(200, 40);
            closeRt.anchoredPosition = new Vector2(0, -215);
            var closeImg = closeGO.AddComponent<Image>();
            closeImg.color = new Color(0.25f, 0.12f, 0.12f, 1f);
            var closeBtn = closeGO.AddComponent<Button>();
            closeBtn.onClick.AddListener(() => SetOpen(false));
            var closeLabel = MakeText(closeGO.transform, "Label", Vector2.zero, new Vector2(190, 36), 15);
            closeLabel.text = "Tutup (N)";
            panel.SetActive(false);
        }

        private static TMP_Text MakeText(Transform parent, string name, Vector2 anchored, Vector2 size, int fontSize)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = anchored;
            var text = go.AddComponent<TextMeshProUGUI>();
            text.fontSize = fontSize;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            return text;
        }
    }
}
