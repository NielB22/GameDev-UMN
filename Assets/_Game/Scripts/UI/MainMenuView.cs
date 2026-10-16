using CyberWatch.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CyberWatch.UI
{
    /// <summary>
    /// Main menu buttons: start a new shift, open settings, quit.
    /// MULAI SHIFT first swaps the buttons for the difficulty choice (NORMAL / LEMBUR TERAKHIR /
    /// KEMBALI), deliberately without any description. The choice buttons are copies of the
    /// existing ones (made at runtime), so they share the menu's style.
    /// Scene changes go through <see cref="GameManager"/>.
    /// </summary>
    public class MainMenuView : MonoBehaviour
    {
        [SerializeField] private Button playButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button quitButton;

        [Tooltip("Optional. Shown/hidden by the settings button.")]
        [SerializeField] private GameObject settingsPanel;

        [Header("Difficulty choice")]
        [SerializeField] private string normalLabel = "NORMAL";
        [SerializeField] private string hardLabel = "LEMBUR TERAKHIR";
        [SerializeField] private string backLabel = "KEMBALI";

        private Button normalButton, hardButton, backButton;
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

            BuildDifficultyButtons();
            ShowDifficultyChoice(false);
        }

        private void OnEnable()
        {
            if (playButton != null) playButton.onClick.AddListener(OnPlay);
            if (settingsButton != null) settingsButton.onClick.AddListener(OnSettings);
            if (quitButton != null) quitButton.onClick.AddListener(OnQuit);
            if (normalButton != null) normalButton.onClick.AddListener(OnNormal);
            if (hardButton != null) hardButton.onClick.AddListener(OnHard);
            if (backButton != null) backButton.onClick.AddListener(OnBack);
        }

        private void OnDisable()
        {
            if (playButton != null) playButton.onClick.RemoveListener(OnPlay);
            if (settingsButton != null) settingsButton.onClick.RemoveListener(OnSettings);
            if (quitButton != null) quitButton.onClick.RemoveListener(OnQuit);
            if (normalButton != null) normalButton.onClick.RemoveListener(OnNormal);
            if (hardButton != null) hardButton.onClick.RemoveListener(OnHard);
            if (backButton != null) backButton.onClick.RemoveListener(OnBack);
        }

        // Same slots as the main buttons: NORMAL where MULAI SHIFT is, LEMBUR TERAKHIR where
        // PENGATURAN is, KEMBALI where KELUAR is.
        private void BuildDifficultyButtons()
        {
            if (playButton == null || quitButton == null)
                return;
            normalButton = CloneButton(playButton, playButton, normalLabel);
            hardButton = CloneButton(playButton, settingsButton != null ? settingsButton : playButton, hardLabel);
            backButton = CloneButton(quitButton, quitButton, backLabel);
        }

        private static Button CloneButton(Button style, Button slot, string label)
        {
            Button copy = Instantiate(style, slot.transform.parent);
            copy.name = label + "Button";
            copy.interactable = true;
            var rt = (RectTransform)copy.transform;
            var slotRt = (RectTransform)slot.transform;
            rt.anchorMin = slotRt.anchorMin;
            rt.anchorMax = slotRt.anchorMax;
            rt.pivot = slotRt.pivot;
            rt.anchoredPosition = slotRt.anchoredPosition;
            rt.sizeDelta = slotRt.sizeDelta;
            rt.SetSiblingIndex(slotRt.GetSiblingIndex() + 1);
            TMP_Text text = copy.GetComponentInChildren<TMP_Text>(true);
            if (text != null) text.text = label;
            return copy;
        }

        private void ShowDifficultyChoice(bool show)
        {
            if (playButton != null) playButton.gameObject.SetActive(!show);
            if (settingsButton != null) settingsButton.gameObject.SetActive(!show);
            if (quitButton != null) quitButton.gameObject.SetActive(!show);
            if (normalButton != null) normalButton.gameObject.SetActive(show);
            if (hardButton != null) hardButton.gameObject.SetActive(show);
            if (backButton != null) backButton.gameObject.SetActive(show);
        }

        private void OnPlay()
        {
            if (starting)
                return;
            if (normalButton == null)
            {
                Begin(GameDifficulty.Normal); // no choice buttons: start straight away
                return;
            }
            if (settingsPanel != null) settingsPanel.SetActive(false);
            ShowDifficultyChoice(true);
        }

        private void OnNormal() => Begin(GameDifficulty.Normal);

        private void OnHard() => Begin(GameDifficulty.LemburTerakhir);

        private void OnBack() => ShowDifficultyChoice(false);

        private void Begin(GameDifficulty difficulty)
        {
            if (starting)
                return; // ignore double clicks while the scene loads
            starting = true;
            SetButtonsInteractable(false);
            GameManager.Instance.Difficulty = difficulty;
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
            if (normalButton != null) normalButton.interactable = value;
            if (hardButton != null) hardButton.interactable = value;
            if (backButton != null) backButton.interactable = value;
        }
    }
}
