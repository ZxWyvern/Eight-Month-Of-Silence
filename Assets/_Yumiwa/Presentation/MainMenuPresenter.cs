using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;
using Yumiwa.Domain;

namespace Yumiwa.Presentation
{
    /// <summary>
    /// Production main-menu route. Clears the previous run through the save boundary,
    /// then loads the first playable scene through the scene-loading boundary.
    /// </summary>
    public sealed class MainMenuPresenter : MonoBehaviour
    {
        [SerializeField] private Button startButton;
        [SerializeField] private Button quitButton;
        [SerializeField] private TMP_Text titleLabel;
        [SerializeField] private TMP_Text startLabel;
        [SerializeField] private TMP_Text quitLabel;
        [SerializeField] private string firstSceneName = "PunyaPandu";

        private ISaveRepository saveRepository;
        private ISceneLoader sceneLoader;
        private bool starting;

        [Inject]
        public void Construct(ISaveRepository saveRepository, ISceneLoader sceneLoader)
        {
            this.saveRepository = saveRepository;
            this.sceneLoader = sceneLoader;
        }

        private void Awake()
        {
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (titleLabel != null) titleLabel.text = "Eight Months of Silence";
            if (startLabel != null) startLabel.text = "Mulai";
            if (quitLabel != null) quitLabel.text = "Keluar";

            if (startButton != null) startButton.onClick.AddListener(OnStartGame);
            else Debug.LogError("[MainMenu] startButton is not assigned.");

            if (quitButton != null) quitButton.onClick.AddListener(OnQuit);
        }

        private void OnDestroy()
        {
            if (startButton != null) startButton.onClick.RemoveListener(OnStartGame);
            if (quitButton != null) quitButton.onClick.RemoveListener(OnQuit);
        }

        private async void OnStartGame()
        {
            if (starting) return;
            if (string.IsNullOrWhiteSpace(firstSceneName))
            {
                Debug.LogError("[MainMenu] firstSceneName is empty.");
                return;
            }
            if (!UnityEngine.Application.CanStreamedLevelBeLoaded(firstSceneName))
            {
                Debug.LogError("[MainMenu] First scene is not in Build Settings: " + firstSceneName);
                return;
            }
            if (saveRepository == null || sceneLoader == null)
            {
                Debug.LogError("[MainMenu] Save/scene services were not injected. Check GameLifetimeScope main-menu mode.");
                return;
            }

            starting = true;
            SetInteractable(false);
            try
            {
                saveRepository.ClearAll();
                await sceneLoader.LoadSceneAsync(firstSceneName);
            }
            catch (Exception exception)
            {
                Debug.LogError("[MainMenu] Failed to start: " + exception.Message);
                starting = false;
                SetInteractable(true);
            }
        }

        private void SetInteractable(bool value)
        {
            if (startButton != null) startButton.interactable = value;
            if (quitButton != null) quitButton.interactable = value;
        }

        private void OnQuit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            UnityEngine.Application.Quit();
#endif
        }
    }
}
