using CyberWatch.Threat;
using UnityEngine;

namespace CyberWatch.Environment
{
    /// <summary>
    /// Light that really comes from the monitor: an emissive screen surface placed on the monitor
    /// model plus a soft spot light shining out of it. Adds a subtle CRT shimmer, and the screen
    /// slowly turns red as the Threat Level rises (design doc: "layar PC berubah kemerahan").
    /// Works without a ThreatLevelSystem too (e.g. main menu): it then just glows normally.
    /// </summary>
    public class MonitorGlow : MonoBehaviour
    {
        [SerializeField] private Renderer screen;
        [SerializeField] private Light screenLight;
        [Tooltip("Found automatically in the scene when empty (optional).")]
        [SerializeField] private ThreatLevelSystem threat;

        [Header("Colors")]
        [SerializeField, ColorUsage(false, true)] private Color normalEmission = new Color(0.25f, 0.45f, 0.6f);
        [SerializeField, ColorUsage(false, true)] private Color dangerEmission = new Color(0.75f, 0.05f, 0.03f);
        [SerializeField] private Color normalLight = new Color(0.55f, 0.75f, 1f);
        [SerializeField] private Color dangerLight = new Color(1f, 0.15f, 0.1f);
        [Tooltip("Threat (%) where the screen starts turning red; fully red at 100%.")]
        [SerializeField, Range(0f, 100f)] private float redFrom = ThreatLevelSystem.DangerAbove;

        [Header("CRT shimmer")]
        [SerializeField, Range(0f, 0.3f)] private float shimmer = 0.06f;
        [SerializeField, Min(0.1f)] private float shimmerSpeed = 7f;

        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
        private MaterialPropertyBlock block;
        private float baseIntensity;
        private float shown;

        private void Awake()
        {
            if (threat == null)
                threat = FindAnyObjectByType<ThreatLevelSystem>();
            if (screenLight != null)
                baseIntensity = screenLight.intensity;
            block = new MaterialPropertyBlock();
        }

        private void Update()
        {
            float target = threat != null ? Mathf.InverseLerp(redFrom, ThreatLevelSystem.Max, threat.Value) : 0f;
            shown = Mathf.MoveTowards(shown, target, Time.deltaTime * 0.5f); // slow, never a sudden change

            float flicker = 1f + (Mathf.PerlinNoise(Time.time * shimmerSpeed, 0.37f) - 0.5f) * 2f * shimmer;

            if (screen != null)
            {
                screen.GetPropertyBlock(block);
                block.SetColor(EmissionColorId, Color.Lerp(normalEmission, dangerEmission, shown) * flicker);
                screen.SetPropertyBlock(block);
            }

            if (screenLight != null)
            {
                screenLight.color = Color.Lerp(normalLight, dangerLight, shown);
                screenLight.intensity = baseIntensity * flicker;
            }
        }
    }
}
