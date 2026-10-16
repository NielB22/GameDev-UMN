using System.Collections;
using UnityEngine;

namespace CyberWatch.Menu
{
    /// <summary>
    /// Main-menu only: the office ceiling light slowly breathes dim <-> bright (brighter than in
    /// gameplay so the office reads better) and now and then flickers like a dying fluorescent tube
    /// (short bursts, sometimes going dark for a moment). The desk lamp stays on so the
    /// scene never goes fully black. Objects are found by name inside Office_Environment,
    /// so the shared prefab is not modified.
    /// </summary>
    public class MenuLightFlicker : MonoBehaviour
    {
        [Header("Targets (found by name when empty)")]
        [SerializeField] private Light ceilingLight;
        [SerializeField] private Renderer ceilingPanel;
        [SerializeField] private string ceilingLightName = "CeilingLight_Working";
        [SerializeField] private string ceilingPanelName = "CeilingLight_Panel_Working";
        [Tooltip("Panel material while the tube is off (M_CeilingLight_Off).")]
        [SerializeField] private Material panelOffMaterial;

        [Header("Breathing (slow dim / bright)")]
        [Tooltip("Brightness boost for the menu only, so the office reads a bit better (1 = as in gameplay).")]
        [SerializeField, Min(0f)] private float menuBrightness = 2f;
        [Tooltip("Lowest point of the slow dim/bright cycle (fraction of full).")]
        [SerializeField, Range(0f, 1f)] private float breathMin = 0.45f;
        [Tooltip("Highest point of the slow dim/bright cycle (fraction of full).")]
        [SerializeField, Range(0f, 1f)] private float breathMax = 1f;
        [Tooltip("Speed of the slow dim/bright waves.")]
        [SerializeField, Min(0.01f)] private float breathSpeed = 0.35f;

        [Header("Timing")]
        [Tooltip("Seconds of calm between flicker bursts (random).")]
        [SerializeField] private Vector2 calmSeconds = new Vector2(10f, 20f);
        [SerializeField] private Vector2Int flickersPerBurst = new Vector2Int(2, 7);
        [Tooltip("Chance that a burst ends with the light dark for a moment.")]
        [SerializeField, Range(0f, 1f)] private float blackoutChance = 0.2f;
        [SerializeField] private Vector2 blackoutSeconds = new Vector2(0.4f, 1.6f);
        [Tooltip("Brightness while dimmed during a flicker (0 = off).")]
        [SerializeField, Range(0f, 1f)] private float dimLevel = 0.08f;

        private float baseIntensity;
        private Material panelOnMaterial;
        private float flicker = 1f; // set by the flicker bursts, on top of the breathing

        private void Awake()
        {
            if (ceilingLight == null)
                ceilingLight = FindByName<Light>(ceilingLightName);
            if (ceilingPanel == null)
                ceilingPanel = FindByName<Renderer>(ceilingPanelName);

            if (ceilingLight != null)
                baseIntensity = ceilingLight.intensity;
            if (ceilingPanel != null)
                panelOnMaterial = ceilingPanel.sharedMaterial;
        }

        private void OnEnable()
        {
            if (ceilingLight == null)
            {
                Debug.LogWarning($"[MenuLightFlicker] Light '{ceilingLightName}' not found.", this);
                enabled = false;
                return;
            }
            StartCoroutine(Loop());
        }

        private void OnDisable()
        {
            Set(1f);
            if (ceilingLight != null)
            {
                ceilingLight.intensity = baseIntensity;
                ceilingLight.enabled = true;
            }
        }

        // Slow, irregular dim <-> bright, with the flicker bursts on top.
        private void Update()
        {
            float wave = Mathf.PerlinNoise(Time.unscaledTime * breathSpeed, 3.7f);
            float breath = Mathf.Lerp(breathMin, breathMax, Mathf.SmoothStep(0f, 1f, wave));
            float level = breath * flicker;
            ceilingLight.intensity = baseIntensity * menuBrightness * level;
            ceilingLight.enabled = level > 0.01f;
        }

        private IEnumerator Loop()
        {
            while (true)
            {
                yield return new WaitForSecondsRealtime(Random.Range(calmSeconds.x, calmSeconds.y));

                int count = Random.Range(flickersPerBurst.x, flickersPerBurst.y + 1);
                for (int i = 0; i < count; i++)
                {
                    Set(Random.value < 0.6f ? dimLevel : Random.Range(0.4f, 0.8f));
                    yield return new WaitForSecondsRealtime(Random.Range(0.03f, 0.12f));
                    Set(1f);
                    yield return new WaitForSecondsRealtime(Random.Range(0.04f, 0.2f));
                }

                if (Random.value < blackoutChance)
                {
                    Set(0f);
                    yield return new WaitForSecondsRealtime(Random.Range(blackoutSeconds.x, blackoutSeconds.y));
                    Set(dimLevel);
                    yield return new WaitForSecondsRealtime(0.08f);
                    Set(1f);
                }
            }
        }

        private void Set(float multiplier)
        {
            flicker = multiplier;
            if (ceilingPanel != null && panelOffMaterial != null && panelOnMaterial != null)
                ceilingPanel.sharedMaterial = multiplier > 0.5f ? panelOnMaterial : panelOffMaterial;
        }

        private static T FindByName<T>(string objectName) where T : Component
        {
            foreach (T c in FindObjectsByType<T>(FindObjectsInactive.Include))
                if (c.gameObject.name == objectName)
                    return c;
            return null;
        }
    }
}
