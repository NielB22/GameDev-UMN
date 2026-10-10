using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CyberWatch.Core
{
    /// <summary>
    /// Single entry point for scene changes, so transitions (fade, loading screen)
    /// can be added later in one place.
    /// </summary>
    public static class SceneLoader
    {
        public static bool IsLoading { get; private set; }

        /// <summary>Raised right before a scene starts loading (name of the target scene).</summary>
        public static event Action<string> LoadStarted;

        /// <summary>Raised after the target scene has finished loading.</summary>
        public static event Action<string> LoadCompleted;

        public static void Load(string sceneName)
        {
            if (IsLoading)
            {
                Debug.LogWarning($"[SceneLoader] Already loading a scene, ignored request for '{sceneName}'.");
                return;
            }

            IsLoading = true;
            LoadStarted?.Invoke(sceneName);

            AsyncOperation op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            if (op == null)
            {
                IsLoading = false;
                Debug.LogError($"[SceneLoader] Scene '{sceneName}' is not in the build scene list.");
                return;
            }

            op.completed += _ =>
            {
                IsLoading = false;
                LoadCompleted?.Invoke(sceneName);
            };
        }

        public static void ReloadCurrent() => Load(SceneManager.GetActiveScene().name);
    }
}
