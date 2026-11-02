using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using VContainer;
using Yumiwa.Application;
using Yumiwa.Domain;

namespace Yumiwa.Presentation
{
    /// <summary>
    /// Pause/resume plus the MVP-01 settings surface. The presenter owns
    /// time-scale/input/modal rules; PauseMenuUi owns the built UI.
    /// </summary>
    public sealed class PauseMenuPresenter : MonoBehaviour
    {
        private const string MainMenuSceneName = "MainMenu";

        [SerializeField] private Transform playerTransform;
        [SerializeField] private PlayerMovementPresenter movement;
        [SerializeField] private FootstepPresenter footsteps;
        [SerializeField] private SubtitleView subtitles;

        private ICheckpointService checkpoint;
        private ISettingsStore store;
        private IEventBus bus;
        private InputActionMap playerMap;
        private SettingsData settings = SettingsData.Defaults();
        private PauseMenuUi view;
        private bool paused;
        private bool pauseHeld;
        private bool settingsDirty;
        private bool subscribed;
        private bool inspectionOpen;
        private bool inventoryOpen;
        private bool reconstructionOpen;
        private Action<InspectionStartedEvent> inspectionStarted;
        private Action<InspectionEndedEvent> inspectionEnded;
        private Action<InventoryOpenedEvent> inventoryOpened;
        private Action<InventoryClosedEvent> inventoryClosed;
        private Action<ReconstructionOpenedEvent> reconstructionOpened;
        private Action<ReconstructionClosedEvent> reconstructionClosed;

        public bool IsPaused => paused;

        [Inject]
        public void Construct(
            ICheckpointService checkpoint,
            ISettingsStore store,
            IEventBus bus,
            InputActionAsset inputAsset)
        {
            this.checkpoint = checkpoint;
            this.store = store;
            this.bus = bus;
            playerMap = inputAsset != null ? inputAsset.FindActionMap("Player") : null;
        }

        private void Awake()
        {
            view = new PauseMenuUi(
                transform,
                settings,
                OnResume,
                OnLoadCheckpoint,
                OnNewGame,
                OnMainMenu,
                OnSettingsChanged);
        }

        private void Start()
        {
            if (store != null) settings = Sanitize(store.Load());
            ApplyToPresenters();
            view.ApplySettings(settings);
            Subscribe();
        }

        private void OnEnable() => Subscribe();

        private void OnDisable()
        {
            if (paused) SetPaused(false);
            Unsubscribe();
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null)
            {
                pauseHeld = false;
                return;
            }

            bool held = keyboard.pKey.isPressed;
            if (held && !pauseHeld && (paused || !IsOtherModalOpen()))
                SetPaused(!paused);
            pauseHeld = held;
        }

        private void Subscribe()
        {
            if (subscribed || bus == null) return;

            inspectionStarted = _ => inspectionOpen = true;
            inspectionEnded = _ => inspectionOpen = false;
            inventoryOpened = _ => inventoryOpen = true;
            inventoryClosed = _ => inventoryOpen = false;
            reconstructionOpened = _ => reconstructionOpen = true;
            reconstructionClosed = _ => reconstructionOpen = false;

            bus.Subscribe(inspectionStarted);
            bus.Subscribe(inspectionEnded);
            bus.Subscribe(inventoryOpened);
            bus.Subscribe(inventoryClosed);
            bus.Subscribe(reconstructionOpened);
            bus.Subscribe(reconstructionClosed);
            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!subscribed || bus == null) return;

            bus.Unsubscribe(inspectionStarted);
            bus.Unsubscribe(inspectionEnded);
            bus.Unsubscribe(inventoryOpened);
            bus.Unsubscribe(inventoryClosed);
            bus.Unsubscribe(reconstructionOpened);
            bus.Unsubscribe(reconstructionClosed);
            subscribed = false;
        }

        private bool IsOtherModalOpen() => inspectionOpen || inventoryOpen || reconstructionOpen;

        private void SetPaused(bool value)
        {
            if (paused == value) return;
            paused = value;
            view.SetVisible(paused);

            if (paused)
            {
                Time.timeScale = 0f;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                playerMap?.Disable();
                bus?.Publish(new PauseOpenedEvent());
            }
            else
            {
                Time.timeScale = 1f;
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                bus?.Publish(new PauseClosedEvent());
                playerMap?.Enable();
                SaveSettings();
            }
        }

        private void OnResume() => SetPaused(false);

        private void OnLoadCheckpoint()
        {
            if (checkpoint == null || playerTransform == null) return;

            CheckpointData data;
            if (checkpoint.HasCheckpoint)
                data = checkpoint.LoadCheckpoint();
            else if (checkpoint.HasChapterSave)
                data = checkpoint.LoadChapter();
            else
                return;

            if (!string.IsNullOrEmpty(data.ActiveScene)
                && data.ActiveScene != SceneManager.GetActiveScene().name)
            {
                Debug.LogWarning($"[MVP-01] Checkpoint belongs to '{data.ActiveScene}', not the active scene.");
                return;
            }

            var controller = playerTransform.GetComponent<CharacterController>();
            if (controller != null) controller.enabled = false;
            playerTransform.SetPositionAndRotation(
                new Vector3(data.PosX, data.PosY, data.PosZ),
                new Quaternion(data.RotX, data.RotY, data.RotZ, data.RotW));
            if (controller != null) controller.enabled = true;
            Physics.SyncTransforms();
            SetPaused(false);
            Debug.Log($"[MVP-01] Checkpoint loaded at {playerTransform.position}.");
        }

        private void OnNewGame()
        {
            if (checkpoint == null) return;
            checkpoint.ClearChapter();
            checkpoint.ClearCheckpoint();
            SaveSettings();
            SetPaused(false);
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        private void OnMainMenu()
        {
            SaveSettings();
            Time.timeScale = 1f;
            SetPaused(false);
            SceneManager.LoadScene(MainMenuSceneName);
        }

        private void OnSettingsChanged(SettingsData next)
        {
            settings = next;
            settingsDirty = true;
            ApplyToPresenters();
        }

        private void ApplyToPresenters()
        {
            if (movement != null) movement.ApplySettings(settings);
            if (footsteps != null) footsteps.ApplySubtitles(settings.SubtitlesEnabled);
            if (subtitles != null) subtitles.ApplySettings(settings);
        }

        private void SaveSettings()
        {
            if (!settingsDirty || store == null) return;
            store.Save(settings);
            settingsDirty = false;
        }

        private static SettingsData Sanitize(SettingsData value)
        {
            return new SettingsData(
                value.HeadBobEnabled,
                value.CameraShakeEnabled,
                value.FovBoostEnabled,
                value.SubtitlesEnabled,
                value.MouseSensitivity > 0f ? value.MouseSensitivity : 1f,
                value.Fov >= 1f && value.Fov <= 179f ? value.Fov : 60f,
                value.TextSize,
                value.SubtitleBackgroundOpacity,
                value.ReadingSpeed);
        }
    }
}
