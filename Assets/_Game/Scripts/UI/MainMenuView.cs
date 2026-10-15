using CyberWatch.Core;
using UnityEngine;
using UnityEngine.UI;

namespace CyberWatch.UI
{
    /// <summary>
    /// Main menu buttons: start a new shift, open settings, quit.
    /// Scene changes go through <see cref="GameManager"/>.
    /// </summary>
    public class MainMenuView : MonoBehaviour
    {
        [SerializeField] private Button playButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button quitButton;

        [Tooltip("Optional. Shown/hidden by the settings button.")]
        [SerializeField] private GameObject settingsPanel;

        private bool starting;

        private void Awake()
        {
            // Coming back from gameplay: make sure the cursor and time are usable.
            Time.timeScale = 1f;
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;

            if (settingsPanel != null)
                settingsPanel.SetActive(false);
            if (settingsButton != null && settingsPanel == null)
                settingsButton.interactable = false; // enabled once the settings panel exists
        }

        private void OnEnable()
        {
            if (playButton != null) playButton.onClick.AddListener(OnPlay);
            if (settingsButton != null) settingsButton.onClick.AddListener(OnSettings);
            if (quitButton != null) quitButton.onClick.AddListener(OnQuit);
        }

        private void OnDisable()
        {
            if (playButton != null) playButton.onClick.RemoveListener(OnPlay);
            if (settingsButton != null) settingsButton.onClick.RemoveListener(OnSettings);
            if (quitButton != null) quitButton.onClick.RemoveListener(OnQuit);
        }

        private void OnPlay()
        {
            if (starting)
                return; // ignore double clicks while the scene loads
            starting = true;
            SetButtonsInteractable(false);
            GameManager.Instance.StartIntro();
        }

        private void OnSettings()
        {
            if (settingsPanel != null)
                settingsPanel.SetActive(!settingsPanel.activeSelf);
        }

        private void OnQuit() => GameManager.Instance.QuitGame();

        private void SetButtonsInteractable(bool value)
        {
            if (playButton != null) playButton.interactable = value;
            if (settingsButton != null) settingsButton.interactable = value && settingsPanel != null;
            if (quitButton != null) quitButton.interactable = value;
        }
    }
}
