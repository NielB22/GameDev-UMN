using CyberWatch.Threat;
using UnityEngine;

namespace CyberWatch.Effects
{
    /// <summary>
    /// Turns the light VHS look (CyberWatch/VHS full-screen pass in PC_Renderer) on for this scene.
    /// Sets the global _VhsIntensity: a fixed base, rising a little with the Threat when a
    /// ThreatLevelSystem exists. Scenes without this component (e.g. the report) show no VHS,
    /// because the intensity is reset to 0 when this component is disabled or the scene unloads.
    /// Lives on the Lighting_Night prefab, so every 3D scene gets it.
    /// </summary>
    public class VhsEffect : MonoBehaviour
    {
        [Tooltip("Found automatically in the scene when empty (optional).")]
        [SerializeField] private ThreatLevelSystem threat;
        [Tooltip("VHS strength in calm moments (0 = off, 1 = full).")]
        [SerializeField, Range(0f, 1f)] private float baseIntensity = 0.6f;
        [Tooltip("VHS strength at 100% Threat.")]
        [SerializeField, Range(0f, 1f)] private float criticalIntensity = 0.85f;

        private static readonly int IntensityId = Shader.PropertyToID("_VhsIntensity");
        private float shown;

        private void Awake()
        {
            if (threat == null) threat = FindAnyObjectByType<ThreatLevelSystem>();
        }

        private void OnEnable() => shown = Target();

        private void OnDisable() => Shader.SetGlobalFloat(IntensityId, 0f);

        private void Update()
        {
            shown = Mathf.MoveTowards(shown, Target(), 0.1f * Time.deltaTime); // slow and seamless
            Shader.SetGlobalFloat(IntensityId, shown);
        }

        private float Target()
        {
            if (threat == null)
                return baseIntensity;
            float t = Mathf.SmoothStep(0f, 1f, threat.Value / ThreatLevelSystem.Max);
            return Mathf.Lerp(baseIntensity, Mathf.Max(baseIntensity, criticalIntensity), t);
        }
    }
}
