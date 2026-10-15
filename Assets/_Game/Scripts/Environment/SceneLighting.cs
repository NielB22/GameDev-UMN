using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace CyberWatch.Environment
{
    /// <summary>
    /// Applies the night lighting of a scene (lives on the Lighting_Night prefab).
    /// All GAME values come from a shared <see cref="LightingProfile"/> asset — edit that asset to
    /// change moon, ambient, fog, sky and reflections for every scene at once.
    /// Dev mode (editor only) switches to bright work lighting; builds always use the profile,
    /// and the scene file always stores the game settings even when saved in dev mode.
    /// </summary>
    [ExecuteAlways]
    public class SceneLighting : MonoBehaviour
    {
        [Header("Mode")]
        [Tooltip("Bright work lighting (editor only). Turn off to see the real game lighting.")]
        [SerializeField] private bool devMode;

        [Header("Game lighting")]
        [Tooltip("Global night settings (Assets/_Game/Data/Lighting/LightingProfile_Night).")]
        [SerializeField] private LightingProfile profile;
        [SerializeField] private Light moon;

        [Header("Dev lighting")]
        [Tooltip("Bright sun used only in dev mode (tag EditorOnly so it is stripped from builds).")]
        [SerializeField] private GameObject devSun;
        [SerializeField] private Color devAmbient = new Color(0.55f, 0.55f, 0.55f);

        public LightingProfile Profile => profile;

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
            LightingProfile.Changed += OnProfileChanged;
            Apply(IsDev);
#if UNITY_EDITOR
            EditorSceneManager.sceneSaving += OnSceneSaving;
            EditorSceneManager.sceneSaved += OnSceneSaved;
#endif
        }

        private void OnDisable()
        {
            LightingProfile.Changed -= OnProfileChanged;
#if UNITY_EDITOR
            EditorSceneManager.sceneSaving -= OnSceneSaving;
            EditorSceneManager.sceneSaved -= OnSceneSaved;
#endif
            if (IsDev)
                Apply(false);
        }

        private void OnProfileChanged(LightingProfile changed)
        {
            if (changed == profile && isActiveAndEnabled)
                Apply(IsDev);
        }

        private void Apply(bool dev)
        {
            if (profile == null)
            {
                Debug.LogWarning("[SceneLighting] No LightingProfile assigned.", this);
                return;
            }

            ApplyMoon();

            if (dev)
            {
                RenderSettings.ambientMode = AmbientMode.Flat;
                RenderSettings.ambientLight = devAmbient;
            }
            else
            {
                RenderSettings.ambientMode = AmbientMode.Trilight;
                RenderSettings.ambientSkyColor = profile.ambientSky * profile.ambientMultiplier;
                RenderSettings.ambientEquatorColor = profile.ambientEquator * profile.ambientMultiplier;
                RenderSettings.ambientGroundColor = profile.ambientGround * profile.ambientMultiplier;
            }

            RenderSettings.reflectionIntensity = dev ? 1f : profile.reflectionIntensity;

            RenderSettings.fog = profile.fog && !dev;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = profile.fogColor;
            RenderSettings.fogDensity = profile.fogDensity;

            Camera cam = Camera.main;
            if (cam != null)
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = profile.skyColor;
            }

            if (devSun != null && devSun.activeSelf != dev)
                devSun.SetActive(dev);
        }

        private void ApplyMoon()
        {
            if (moon == null)
                return;
            moon.color = profile.moonColor;
            moon.intensity = profile.moonIntensity;
            moon.shadowStrength = profile.moonShadowStrength;
            moon.transform.rotation = Quaternion.Euler(profile.moonElevation, profile.moonDirection, 0f);
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
