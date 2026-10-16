using System.Collections;
using CyberWatch.Threat;
using UnityEngine;

namespace CyberWatch.Environment
{
    /// <summary>
    /// Light that really comes from the monitor: an emissive screen surface placed on the monitor
    /// model plus a soft spot light shining out of it. Adds a subtle CRT shimmer. The color never
    /// changes; instead, as the Threat Level rises the screen grows unstable and pulses dimmer and
    /// brighter (never fully off).
    /// Works without a ThreatLevelSystem too (e.g. main menu): it then just glows normally.
    /// </summary>
    public class MonitorGlow : MonoBehaviour
    {
        [SerializeField] private Renderer screen;
        [SerializeField] private Light screenLight;
        [Tooltip("Found automatically in the scene when empty (optional).")]
        [SerializeField] private ThreatLevelSystem threat;

        [Header("Color")]
        [SerializeField, ColorUsage(false, true)] private Color normalEmission = new Color(0.25f, 0.45f, 0.6f);
        [SerializeField] private Color normalLight = new Color(0.55f, 0.75f, 1f);

        [Header("CRT shimmer (always)")]
        [SerializeField, Range(0f, 0.3f)] private float shimmer = 0.06f;
        [SerializeField, Min(0.1f)] private float shimmerSpeed = 7f;

        [Header("Unstable brightness (rises with Threat)")]
        [Tooltip("Threat (%) where the screen starts to pulse; strongest at 100%.")]
        [SerializeField, Range(0f, 100f)] private float unstableFrom = ThreatLevelSystem.DangerAbove;
        [Tooltip("How far the brightness may drop at 100% Threat (0.6 = down to 40%).")]
        [SerializeField, Range(0f, 0.9f)] private float maxDim = 0.6f;
        [Tooltip("Speed of the slow dim/bright waves.")]
        [SerializeField, Min(0.05f)] private float pulseSpeed = 1.6f;
        [Tooltip("Short, sharper dips now and then (still never off).")]
        [SerializeField] private Vector2 dipInterval = new Vector2(1.5f, 4f);
        [SerializeField, Min(0.05f)] private float dipSeconds = 0.35f;

        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
        private MaterialPropertyBlock block;
        private float baseIntensity;
        private float shown;
        private float nextDipAt;
        private float dipStart = -10f;
        private bool dead; // lose sequence: screen is off for good

        private void Awake()
        {
            if (threat == null)
                threat = FindAnyObjectByType<ThreatLevelSystem>();
            if (screenLight != null)
            {
                baseIntensity = screenLight.intensity;
                screenLight.color = normalLight;
            }
            block = new MaterialPropertyBlock();
        }

        /// <summary>Lose sequence: the screen sputters and goes black for good.</summary>
        public IEnumerator DieOut()
        {
            if (dead)
                yield break;
            dead = true;
            int flickers = Random.Range(3, 6);
            for (int i = 0; i < flickers; i++)
            {
                SetLevel(i % 2 == 0 ? 0.25f : 0.8f);
                yield return new WaitForSeconds(Random.Range(0.05f, 0.15f));
                SetLevel(0f);
                yield return new WaitForSeconds(Random.Range(0.05f, 0.2f));
            }
            SetLevel(0f);
        }

        private void Update()
        {
            if (dead)
                return;
            float target = threat != null ? Mathf.InverseLerp(unstableFrom, ThreatLevelSystem.Max, threat.Value) : 0f;
            shown = Mathf.MoveTowards(shown, target, Time.deltaTime * 0.3f); // slow, never a sudden change

            float level = (1f + (Mathf.PerlinNoise(Time.time * shimmerSpeed, 0.37f) - 0.5f) * 2f * shimmer)
                          * UnstableLevel();
            SetLevel(level);
        }

        // 1 = normal brightness, 0 = screen and its light fully off.
        private void SetLevel(float level)
        {
            if (screen != null)
            {
                screen.GetPropertyBlock(block);
                block.SetColor(EmissionColorId, normalEmission * level);
                screen.SetPropertyBlock(block);
            }

            if (screenLight != null)
            {
                screenLight.intensity = baseIntensity * level;
                screenLight.enabled = level > 0.001f;
            }
        }

        // 1 = steady. Slow dim/bright waves plus the odd eased dip, scaled by Threat.
        private float UnstableLevel()
        {
            if (shown <= 0.001f)
                return 1f;

            float now = Time.time;
            if (now >= nextDipAt)
            {
                dipStart = now;
                // Dips come more often as Threat rises.
                nextDipAt = now + Random.Range(dipInterval.x, dipInterval.y) / Mathf.Lerp(0.5f, 1.5f, shown);
            }

            float wave = Mathf.PerlinNoise(now * pulseSpeed, 5.1f);               // 0..1, smooth
            float dipT = (now - dipStart) / dipSeconds;
            float dip = dipT < 1f ? Mathf.Sin(dipT * Mathf.PI) : 0f;             // eased down and back up
            float dim = Mathf.Clamp01(wave * 0.6f + dip * 0.7f);
            return 1f - maxDim * shown * dim;
        }
    }
}
