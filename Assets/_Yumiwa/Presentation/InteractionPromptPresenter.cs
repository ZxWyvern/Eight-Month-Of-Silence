using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Yumiwa.Application;
using Yumiwa.Domain;

namespace Yumiwa.Presentation
{
    /// <summary>
    /// Minimal interaction prompt per docs/07 HUD — kontekstual, muncul hanya saat ada Interactable di radius.
    /// Rework-friendly: scan IInspectable + IPuzzleView + CheckpointTrigger, no hardcoded IDs, auto-creates UI if not assigned.
    /// C5 fix: 0B per-frame — no reflection, TryGetComponent, cached strings, preallocated prompt.
    /// ADR-0018: now uses IInspectable.Definition not Data.
    /// </summary>
    public sealed class InteractionPromptPresenter : MonoBehaviour
    {
        [SerializeField] private TMP_Text promptText;
        [SerializeField] private string interactKey = "F";
        [SerializeField] private string examineKey = "E";
        [SerializeField] private float scanRadius = 2.2f;
        [SerializeField] private LayerMask scanMask = ~0;
        [SerializeField] private float fadeSpeed = 8f;
        [SerializeField] private TMP_FontAsset promptFont; // optional inspector font (avoids Resources per-frame)

        private Canvas promptCanvas;
        private readonly Collider[] hits = new Collider[16];
        private string currentPrompt = "";
        private float targetAlpha = 0f;
        private CanvasGroup canvasGroup;
        // C5: preallocate to avoid string alloc when prompt unchanged
        private string cachedLockedPrompt = "Puzzle Terkunci \u2014 cek Evidence Log";
        private string cachedSolvePrompt;
        private string cachedCheckpointPrefix = "Checkpoint \u2014 ";

        private void Awake()
        {
            cachedSolvePrompt = $"[{interactKey}] Solve Puzzle";
            if (promptText == null)
            {
                promptText = GetComponentInChildren<TMP_Text>(true);
            }
            // Auto-create minimal prompt UI if scene has none (rework-friendly, no prefab required)
            if (promptText == null)
            {
                var canvasGO = new GameObject("InteractionPrompt", typeof(RectTransform));
                canvasGO.transform.SetParent(transform, false);
                var canvas = canvasGO.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 110;
                canvasGO.AddComponent<CanvasScaler>();
                canvasGO.AddComponent<GraphicRaycaster>();
                promptCanvas = canvas;
                canvasGroup = canvasGO.AddComponent<CanvasGroup>();
                var textGO = new GameObject("PromptText", typeof(RectTransform));
                textGO.transform.SetParent(canvasGO.transform, false);
                promptText = textGO.AddComponent<TextMeshProUGUI>();
                if (promptFont != null) promptText.font = promptFont; // P2: Resources removed — null keeps Unity default (Arial) without Addressables
                promptText.fontSize = 18;
                promptText.color = Color.white;
                promptText.alignment = TextAlignmentOptions.Center;
                promptText.text = "";
                var rt = textGO.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0f);
                rt.anchorMax = new Vector2(0.5f, 0f);
                rt.pivot = new Vector2(0.5f, 0f);
                rt.anchoredPosition = new Vector2(0, 80);
                rt.sizeDelta = new Vector2(600, 30);
                // BG behind text for readability
                var bgGO = new GameObject("BG", typeof(RectTransform));
                bgGO.transform.SetParent(textGO.transform, false);
                bgGO.transform.SetAsFirstSibling();
                var bgImg = bgGO.AddComponent<Image>();
                bgImg.color = new Color(0, 0, 0, 0.6f);
                var bgRt = bgGO.GetComponent<RectTransform>();
                bgRt.anchorMin = Vector2.zero;
                bgRt.anchorMax = Vector2.one;
                bgRt.offsetMin = new Vector2(-10, -4);
                bgRt.offsetMax = new Vector2(10, 4);
            }
            else
            {
                promptCanvas = GetComponent<Canvas>();
                if (promptCanvas == null) promptCanvas = GetComponentInParent<Canvas>();
                canvasGroup = GetComponent<CanvasGroup>();
                if (canvasGroup == null && promptCanvas != null) canvasGroup = promptCanvas.gameObject.AddComponent<CanvasGroup>();
            }
            if (promptText != null) promptText.text = "";
            if (canvasGroup != null) canvasGroup.alpha = 0f;
        }

        private float lastScanTime;
        private IInspectable cachedBestInspectable;
        private string cachedInspectablePrompt;
        private IPuzzleView cachedBestPuzzle;
        private CheckpointTrigger cachedBestCheckpoint;

        private void Update()
        {
            // Throttle scan to 0.1s to reduce 48 TryGetComponent per-frame → ~10 scans/s (ARCH §8 0B)
            if (Time.unscaledTime - lastScanTime < 0.1f)
            {
                // Only fade, no scan
                if (canvasGroup != null) canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, targetAlpha, Time.unscaledDeltaTime * fadeSpeed);
                else if (promptText != null) promptText.enabled = targetAlpha > 0.01f;
                return;
            }
            lastScanTime = Time.unscaledTime;
            string prompt = GetNearestPromptCached();
            if (prompt != currentPrompt)
            {
                currentPrompt = prompt;
                if (promptText != null) promptText.text = prompt;
                targetAlpha = string.IsNullOrEmpty(prompt) ? 0f : 1f;
            }
            if (canvasGroup != null)
                canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, targetAlpha, Time.unscaledDeltaTime * fadeSpeed);
            else if (promptText != null)
                promptText.enabled = targetAlpha > 0.01f;
        }

        private string GetNearestPrompt() => GetNearestPromptCached();

        private string GetNearestPromptCached()
        {
            int count = Physics.OverlapSphereNonAlloc(transform.position, scanRadius, hits, scanMask, QueryTriggerInteraction.Collide);
            IInspectable bestInspectable = null;
            float bestSqr = float.MaxValue;
            IPuzzleView bestPuzzle = null;
            float bestPuzzleSqr = float.MaxValue;
            CheckpointTrigger bestCheckpoint = null;
            float bestCheckpointSqr = float.MaxValue;
            Vector3 origin = transform.position;
            for (int i = 0; i < count; i++)
            {
                var col = hits[i];
                if (col == null) continue;
                // Single TryGetComponent per type, still 48 worst-case but throttled 0.1s → ~4.8 avg per frame
                if (col.TryGetComponent<IInspectable>(out var insp) && insp.IsAvailable)
                {
                    var comp = insp as Component;
                    if (comp != null)
                    {
                        float sqr = (comp.transform.position - origin).sqrMagnitude;
                        if (sqr < bestSqr) { bestSqr = sqr; bestInspectable = insp; }
                    }
                }
                if (col.TryGetComponent<IPuzzleView>(out var puzzle))
                {
                    float sqr = (col.transform.position - origin).sqrMagnitude;
                    if (sqr < bestPuzzleSqr) { bestPuzzleSqr = sqr; bestPuzzle = puzzle; }
                }
                if (col.TryGetComponent<CheckpointTrigger>(out var cp) && col.enabled)
                {
                    float sqr = (col.transform.position - origin).sqrMagnitude;
                    if (sqr < bestCheckpointSqr) { bestCheckpointSqr = sqr; bestCheckpoint = cp; }
                }
            }
            if (bestInspectable != null)
            {
                if (bestInspectable == cachedBestInspectable && !string.IsNullOrEmpty(cachedInspectablePrompt))
                    return cachedInspectablePrompt;
                var def = bestInspectable.Definition;
                string name = !string.IsNullOrEmpty(def.DisplayName) ? def.DisplayName : (bestInspectable as Component)?.name ?? "Item";
                string prompt = $"[{examineKey}] Examine \u2014 {name}";
                cachedBestInspectable = bestInspectable;
                cachedInspectablePrompt = prompt;
                cachedBestPuzzle = null;
                cachedBestCheckpoint = null;
                return prompt;
            }
            // Clear inspectable cache when not best
            cachedBestInspectable = null;
            if (bestPuzzle != null)
            {
                if (bestPuzzle == cachedBestPuzzle) return bestPuzzle.IsSolved ? "" : (!bestPuzzle.CanAttempt ? cachedLockedPrompt : cachedSolvePrompt);
                cachedBestPuzzle = bestPuzzle;
                cachedBestCheckpoint = null;
                if (bestPuzzle.IsSolved) return "";
                if (!bestPuzzle.CanAttempt) return cachedLockedPrompt;
                return cachedSolvePrompt;
            }
            cachedBestPuzzle = null;
            if (bestCheckpoint != null)
            {
                if (bestCheckpoint == cachedBestCheckpoint) return cachedCheckpointPrefix + bestCheckpoint.name; // still alloc but throttled
                cachedBestCheckpoint = bestCheckpoint;
                return cachedCheckpointPrefix + bestCheckpoint.name;
            }
            cachedBestCheckpoint = null;
            return "";
        }
    }
}
