using CyberWatch.Session;
using UnityEngine;

namespace CyberWatch.Core
{
    /// <summary>
    /// Persistent owner of cross-scene state (the current shift) and the game flow.
    /// Created automatically on first access, so any scene can be played directly in the editor.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        private static GameManager instance;

        public static GameManager Instance
        {
            get
            {
                if (instance == null)
                {
                    var go = new GameObject(nameof(GameManager));
                    instance = go.AddComponent<GameManager>();
                }
                return instance;
            }
        }

        public ShiftSession CurrentShift { get; private set; } = new ShiftSession();

        /// <summary>Picked in the main menu; stays the same for Retry.</summary>
        public GameDifficulty Difficulty { get; set; } = GameDifficulty.Normal;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            if (instance == this)
                instance = null;
        }

        // ---------- flow ----------

        public void GoToMainMenu() => SceneLoader.Load(SceneNames.MainMenu);
        public void StartIntro() { ResetShift(); SceneLoader.Load(SceneNames.CinematicIntro); }

        /// <summary>
        /// Starts a fresh shift. The player begins walking in the office (walk mode)
        /// and sits at the IT desk to work (desk mode) — both inside Gameplay_Desktop_Scene.
        /// </summary>
        public void StartNewShift()
        {
            ResetShift();
            SceneLoader.Load(SceneNames.GameplayDesktop);
        }

        /// <summary>Discards the previous shift's decisions and outcome.</summary>
        public void ResetShift() => CurrentShift = new ShiftSession();

        /// <summary>Called on win (06:00) or loss (threat 100%).</summary>
        public void EndShift(ShiftOutcome outcome)
        {
            if (CurrentShift.Outcome != ShiftOutcome.InProgress)
                return;

            CurrentShift.Outcome = outcome;
            SceneLoader.Load(SceneNames.GameOverReport);
        }
   
     

        public void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }


}
