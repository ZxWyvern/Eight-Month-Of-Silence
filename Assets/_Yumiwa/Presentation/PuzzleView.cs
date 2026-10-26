using System;
using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using VContainer;
using Yumiwa.Application;
using Yumiwa.Domain;

namespace Yumiwa.Presentation
{
    /// <summary>
    /// Prototype puzzle view per GDD §03 — environmental / combination puzzle.
    /// One puzzle for vertical slice. Requires evidence + item to attempt.
    /// Implements IPuzzleView for 0B GC prompt queries (C5).
    /// </summary>
    public sealed partial class PuzzleView : MonoBehaviour, IPuzzleView
    {
        [SerializeField] private string puzzleId = "puzzle_alley_code";
        [SerializeField] private Renderer targetRenderer;
        [SerializeField] private Color baseColor = Color.yellow;
        [SerializeField] private Color solvedColor = Color.green;
        [SerializeField] private Color failedColor = Color.red;
        [SerializeField] private TMP_FontAsset panelFont; // assign in inspector; fallback to built-in Arial if null (keeps Resources usage guarded, see ResolveFont)

        private PuzzleService puzzleService;
        private IEventBus bus;
        private IPlayerInput playerInput;
        private MissionFlowService missionFlow;
        private MaterialPropertyBlock block;
        private float failedFlashTimer;
        private bool playerInRange;
        private readonly PuzzlePanelFactory panelFactory = new PuzzlePanelFactory();

        // IPuzzleView
        public string PuzzleId => puzzleId;
        public bool IsSolved => puzzleService != null && puzzleService.GetState(puzzleId) == PuzzleState.Solved;
        public bool CanAttempt => puzzleService != null && puzzleService.CanAttempt(puzzleId);
        private GameObject puzzlePanel;
        private TMP_InputField inputField;
        private TMP_Text feedbackText;
        private bool cursorWasLocked;

        [Inject]
        public void Construct(PuzzleService puzzleService, IEventBus bus, IPlayerInput playerInput = null, MissionFlowService missionFlow = null)
        {
            this.puzzleService = puzzleService;
            this.bus = bus;
            this.playerInput = playerInput;
            this.missionFlow = missionFlow;
        }

        private void Awake()
        {
            if (targetRenderer == null) targetRenderer = GetComponentInChildren<Renderer>(true);
            block = new MaterialPropertyBlock();
            ApplyColor(baseColor);
            if (missionFlow == null)
            {
                Debug.LogWarning("[Yumiwa] PuzzleView missionFlow not injected — add Puzzle_AlleyCode to GameLifetimeScope.presenterRoots for mission wiring.", this);
            }
            // Ensure we have a trigger collider for proximity; visual collider stays solid
            var triggerCol = GetComponent<BoxCollider>();
            if (triggerCol != null && triggerCol.isTrigger)
            {
                // Already a trigger
            }
            else
            {
                var col = gameObject.AddComponent<BoxCollider>();
                col.isTrigger = true;
            }
        }

        private void OnEnable()
        {
            bus?.Subscribe<PuzzleSolvedEvent>(OnSolved);
            bus?.Subscribe<PuzzleFailedEvent>(OnFailed);
            if (playerInput != null)
            {
                playerInput.UsePressed += OnUsePressed;
                playerInput.CensorPressed += OnCancelPressed; // Esc-like close (Censor repurposed as Cancel for puzzle)
            }
        }

        private void OnDisable()
        {
            bus?.Unsubscribe<PuzzleSolvedEvent>(OnSolved);
            bus?.Unsubscribe<PuzzleFailedEvent>(OnFailed);
            if (playerInput != null)
            {
                playerInput.UsePressed -= OnUsePressed;
                playerInput.CensorPressed -= OnCancelPressed;
            }
            DestroyPanel();
        }

        private void Update()
        {
            if (failedFlashTimer > 0f)
            {
                failedFlashTimer -= Time.deltaTime;
                if (failedFlashTimer <= 0f) ApplyColor(baseColor);
            }
        }



        private void CreatePanel()
        {
            // Delegate UI construction to factory (SIZE-001 split)
            if (panelFactory.GetOrCreate(panelFont, OnSubmit, OnInputSubmit, out var panel, out var input, out var feedback))
            {
                puzzlePanel = panel;
                inputField = input;
                feedbackText = feedback;
                cursorWasLocked = Cursor.lockState == CursorLockMode.Locked;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                var es = EventSystem.current;
                if (es != null && inputField != null) es.SetSelectedGameObject(inputField.gameObject);
                inputField?.Select();
                inputField?.ActivateInputField();
            }
        }

        private void DestroyPanel()
        {
            if (puzzlePanel != null)
            {
                panelFactory.Release(puzzlePanel);
                if (cursorWasLocked || Cursor.lockState == CursorLockMode.None)
                {
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                }
                var es = EventSystem.current;
                if (es != null) es.SetSelectedGameObject(null);
            }
        }

        private System.Collections.IEnumerator DestroyPanelAfter(float seconds)
        {
            yield return new WaitForSecondsRealtime(seconds);
            DestroyPanel();
        }

        private TMP_FontAsset ResolveFont() => panelFont;

        private void ApplyColor(Color c)
        {
            if (targetRenderer == null) return;
            if (targetRenderer.sharedMaterial == null) return;
            targetRenderer.GetPropertyBlock(block);
            if (targetRenderer.sharedMaterial.HasProperty("_BaseColor")) block.SetColor("_BaseColor", c);
            else if (targetRenderer.sharedMaterial.HasProperty("_Color")) block.SetColor("_Color", c);
            else block.SetColor("_Color", c);
            targetRenderer.SetPropertyBlock(block);
        }
    }
}
