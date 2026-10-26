using UnityEngine;
using TMPro;
using VContainer;
using Yumiwa.Application;
using Yumiwa.Domain;

namespace Yumiwa.Presentation
{
    /// <summary>
    /// Minimal mission HUD per GDD §06 + docs/07 minimal — shows current mission + progress + auto-hide, rework-friendly.
    /// Data-driven via MissionFlowService (ADR-0003), mechanics-aligned to scene (M01 Alley = YumiwaMovementTest prototype).
    /// Easy rework: expose SetMission for external drives, keep <150L, no hardcoded mission IDs.
    /// </summary>
    public sealed class MissionHudPresenter : MonoBehaviour
    {
        [SerializeField] private TMP_Text hudText;
        [SerializeField] private float autoHideSeconds = 0f; // 0 = persistent (mission HUD jangan hilang, docs/07 minimal tapi mission selalu visible)
        [SerializeField] private bool showProgress = true;

        private MissionFlowService missionFlow;
        private IEventBus bus;
        private Canvas canvas;

        [Inject]
        public void Construct(MissionFlowService missionFlow, IEventBus bus)
        {
            this.missionFlow = missionFlow;
            this.bus = bus;
        }

        private void Awake()
        {
            if (hudText == null) hudText = GetComponentInChildren<TMP_Text>(true);
            canvas = GetComponent<Canvas>();
            // Mission HUD persistent minimal — jangan hilang (user report "UI hilang"). Stamina saja yang auto-hide.
            if (canvas != null) canvas.enabled = true;
            if (hudText != null && string.IsNullOrEmpty(hudText.text)) hudText.text = "";
        }

        private void Start()
        {
            // Auto-start for prototype scene (YumiwaMovementTest) — easy rework: remove if driven externally
            if (missionFlow != null && missionFlow.Current == null && missionFlow.Count > 0)
            {
                missionFlow.Start();
            }
            RefreshHud();
        }

        private void OnEnable()
        {
            bus?.Subscribe<MissionChangedEvent>(OnMissionChanged);
            RefreshHud();
        }

        private void OnDisable()
        {
            bus?.Unsubscribe<MissionChangedEvent>(OnMissionChanged);
            CancelInvoke(nameof(HideHud));
        }

        private void OnMissionChanged(MissionChangedEvent e)
        {
            Debug.Log($"[Yumiwa] Mission: {e.MissionId} ({e.Chapter})", this);
            RefreshHud();
            // Mission HUD persistent — show, auto-hide hanya jika autoHideSeconds >0
            if (canvas != null) canvas.enabled = true;
            if (autoHideSeconds > 0f) ShowTemporarily();
        }

        // Rework-friendly public API — external systems (Checkpoint, Puzzle solve) can drive HUD without touching internals
        public void SetMission(string missionId)
        {
            if (missionFlow == null) return;
            var seq = missionFlow.Sequence;
            for (int i = 0; i < seq.Count; i++)
            {
                if (seq[i].MissionId == missionId)
                {
                    missionFlow.SetIndex(i);
                    return;
                }
            }
        }

        public string GetCurrentMissionName()
        {
            var cur = missionFlow?.Current;
            return cur.HasValue ? cur.Value.DisplayName : "No Mission";
        }

        public string GetCurrentProgressText()
        {
            if (missionFlow == null || missionFlow.Count == 0) return "";
            if (missionFlow.Current == null) return $"0/{missionFlow.Count}";
            return $"{missionFlow.CurrentIndex + 1}/{missionFlow.Count}";
        }

        private void RefreshHud()
        {
            if (hudText == null) return;
            if (missionFlow == null || missionFlow.Current == null)
            {
                hudText.text = showProgress && missionFlow != null && missionFlow.Count > 0
                    ? $"No Mission — 0/{missionFlow.Count}"
                    : "No Mission";
                return;
            }
            var cur = missionFlow.Current.Value;
            var progress = showProgress ? $" [{GetCurrentProgressText()}]" : "";
            // Minimal: "M01 — Jejak Darah Pertama [1/5]" — chapter not shown to keep minimal, but available via cur.Chapter for rework
            hudText.text = $"{cur.DisplayName}{progress}";
            // Final mission styling hint (subtle, rework-friendly: color via hudText.color)
            if (missionFlow.IsFinalMission)
                hudText.color = Color.yellow;
            else
                hudText.color = Color.white;
        }

        private void ShowTemporarily()
        {
            if (canvas != null) canvas.enabled = true;
            if (hudText != null) hudText.enabled = true;
            CancelInvoke(nameof(HideHud));
            if (autoHideSeconds > 0f) Invoke(nameof(HideHud), autoHideSeconds);
        }

        private void HideHud()
        {
            if (canvas != null) canvas.enabled = false;
        }
    }
}
