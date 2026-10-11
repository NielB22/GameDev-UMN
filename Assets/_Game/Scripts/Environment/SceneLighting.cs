using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace CyberWatch.Environment
{
    /// <summary>
    /// One place for the night lighting of every scene (lives on the Lighting_Night prefab):
    /// fog, ambient and camera background for the GAME look, plus a DEV mode for working in the editor.
    /// Dev mode turns on a bright sun, flat bright ambient and no fog. It is editor-only:
    /// builds always use the game lighting, and the scene file always stores the game settings.
    /// </summary>
    [ExecuteAlways]
    public class SceneLighting : MonoBehaviour
    {
        [Header("Mode")]
        [Tooltip("Bright work lighting (editor only). Turn off to see the real game lighting.")]
        [SerializeField] private bool devMode;

        [Header("Game lighting (night)")]
        [SerializeField] private Light moon;
        [SerializeField] private Color ambientSky = new Color(0.11f, 0.12f, 0.17f);
        [SerializeField] private Color ambientEquator = new Color(0.07f, 0.075f, 0.09f);
        [SerializeField] private Color ambientGround = new Color(0.035f, 0.035f, 0.04f);
        [SerializeField] private bool fog = true;
        [SerializeField] private Color fogColor = new Color(0.045f, 0.05f, 0.065f);
        [SerializeField, Range(0f, 0.2f)] private float fogDensity = 0.03f;
        [Tooltip("Night sky behind everything (Main Camera solid color).")]
        [SerializeField] private Color cameraBackground = new Color(0.015f, 0.02f, 0.035f);

        [Header("Dev lighting")]
        [Tooltip("Bright sun used only in dev mode (tag EditorOnly so it is stripped from builds).")]
        [SerializeField] private GameObject devSun;
        [SerializeField] private Color devAmbient = new Color(0.55f, 0.55f, 0.55f);

        private bool IsDev
        {
            get
            {
#if UNITY_EDITOR
                return devMode;
#else
                return false;
#endif
            }
        }

        private void OnEnable()
        {
            Apply(IsDev);
#if UNITY_EDITOR
            EditorSceneManager.sceneSaving += OnSceneSaving;
            EditorSceneManager.sceneSaved += OnSceneSaved;
#endif
        }

        private void OnDisable()
        {
#if UNITY_EDITOR
            EditorSceneManager.sceneSaving -= OnSceneSaving;
            EditorSceneManager.sceneSaved -= OnSceneSaved;
#endif
            if (IsDev)
                Apply(false);
        }

        private void Apply(bool dev)
        {
            RenderSettings.ambientMode = dev ? AmbientMode.Flat : AmbientMode.Trilight;
            if (dev)
            {
                RenderSettings.ambientLight = devAmbient;
            }
            else
            {
                RenderSettings.ambientSkyColor = ambientSky;
                RenderSettings.ambientEquatorColor = ambientEquator;
                RenderSettings.ambientGroundColor = ambientGround;
            }

            RenderSettings.fog = fog && !dev;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = fogColor;
            RenderSettings.fogDensity = fogDensity;

            Camera cam = Camera.main;
            if (cam != null)
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = cameraBackground;
            }

            if (devSun != null && devSun.activeSelf != dev)
                devSun.SetActive(dev);
        }

#if UNITY_EDITOR
        // Inspector edits: apply after the inspector finishes (SetActive is not allowed inside OnValidate).
        private void OnValidate()
        {
            EditorApplication.delayCall += () =>
            {
                if (this != null && isActiveAndEnabled)
                    Apply(IsDev);
            };
        }

        // The scene file always stores the GAME lighting, even when saved in dev mode.
        private void OnSceneSaving(UnityEngine.SceneManagement.Scene scene, string path)
        {
            if (scene == gameObject.scene && IsDev)
                Apply(false);
        }

        private void OnSceneSaved(UnityEngine.SceneManagement.Scene scene)
        {
            if (scene == gameObject.scene && IsDev)
                Apply(true);
        }
#endif
    }
}
