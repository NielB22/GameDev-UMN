using CyberWatch.Threat;
using UnityEngine;
using UnityEngine.UI;

namespace CyberWatch.Effects
{
    /// <summary>
    /// Short TV-static flash over the whole screen (0.1-0.4 s), used when "something changes":
    /// the figure blinks to a new spot, a light dies, or the Threat stage goes up.
    /// Flashes are only shown shortly after the Threat value went UP; when it drops or stays
    /// the same (e.g. the figure creeping, a random street lamp outage) the screen stays clean.
    /// Call ScreenGlitch.Trigger(strength) from anywhere; without a ScreenGlitch in the scene
    /// (e.g. main menu) the call does nothing.
    /// Builds its own overlay canvas at runtime. ThreatPostProcess reads Strength to add a
    /// chromatic / lens spike while the flash lasts.
    /// </summary>
    public class ScreenGlitch : MonoBehaviour
    {
        [Tooltip("Found automatically in the scene when empty.")]
        [SerializeField] private ThreatLevelSystem threat;

        [Header("Look")]
        [Tooltip("Static resolution (low = chunky TV noise).")]
        [SerializeField] private Vector2Int noiseSize = new Vector2Int(160, 90);
        [Tooltip("Static opacity at full strength.")]
        [SerializeField, Range(0f, 1f)] private float maxAlpha = 0.35f;
        [Tooltip("Chance per row of a bright horizontal tear line.")]
        [SerializeField, Range(0f, 0.3f)] private float tearChance = 0.06f;
        [SerializeField] private Vector2 durationSeconds = new Vector2(0.12f, 0.35f);
        [Tooltip("Static flickers (on/off) this many times per second while it lasts.")]
        [SerializeField, Min(1f)] private float flickerRate = 30f;

        [Header("Only when Threat rises")]
        [Tooltip("A flash is only shown within this many seconds after the Threat went UP " +
                 "(not when it drops or stays the same).")]
        [SerializeField, Min(0.1f)] private float riseWindowSeconds = 2.5f;

        [Header("Automatic triggers")]
        [Tooltip("Flash when the Threat stage goes up.")]
        [SerializeField] private bool onStageUp = true;
        [SerializeField, Range(0f, 1f)] private float stageUpStrength = 0.8f;

        private static ScreenGlitch instance;

        private RawImage overlay;
        private Texture2D noise;
        private Color32[] pixels;
        private float endTime;
        private float duration;
        private float strength;
        private float nextFrameAt;
        private float lastThreat;
        private float lastRiseAt = -100f;

        /// <summary>0..1 while a flash is running (fades out), else 0.</summary>
        public static float Strength => instance != null ? instance.CurrentStrength : 0f;

        private float CurrentStrength =>
            Time.time < endTime && duration > 0f ? strength * Mathf.Clamp01((endTime - Time.time) / duration) : 0f;

        /// <summary>Flash the screen. Strength 0..1; a stronger flash also lasts longer.</summary>
        public static void Trigger(float strength = 1f)
        {
            if (instance != null)
                instance.Flash(strength);
        }

        private void Awake()
        {
            instance = this;
            if (threat == null) threat = FindAnyObjectByType<ThreatLevelSystem>();
            BuildOverlay();
        }

        private void OnDestroy()
        {
            if (instance == this) instance = null;
            if (noise != null) Destroy(noise);
        }

        private void OnEnable()
        {
            if (threat == null)
                return;
            lastThreat = threat.Value;
            threat.ThreatChanged += OnThreatChanged;
            threat.StageChanged += OnStageChanged;
        }

        private void OnDisable()
        {
            if (threat == null)
                return;
            threat.ThreatChanged -= OnThreatChanged;
            threat.StageChanged -= OnStageChanged;
        }

        private void OnThreatChanged(float value)
        {
            if (value > lastThreat)
                lastRiseAt = Time.time;
            lastThreat = value;
        }

        // Also true when the rise is happening right now but our ThreatChanged handler has not run yet.
        private bool RecentlyRose =>
            threat == null || Time.time - lastRiseAt <= riseWindowSeconds || threat.Value > lastThreat;

        private void OnStageChanged(ThreatStage previous, ThreatStage current)
        {
            if (onStageUp && current > previous && current >= ThreatStage.Alert && current < ThreatStage.Compromised)
                Flash(stageUpStrength);
        }

        private void Flash(float s)
        {
            s = Mathf.Clamp01(s);
            if (s <= 0f || !RecentlyRose)
                return;
            // A new flash never cuts a stronger one short.
            if (CurrentStrength > s)
                return;
            strength = s;
            duration = Mathf.Lerp(durationSeconds.x, durationSeconds.y, s);
            endTime = Time.time + duration;
        }

        private void BuildOverlay()
        {
            var canvasGo = new GameObject("ScreenGlitchOverlay", typeof(Canvas));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 500; // above HUD and inbox

            var imageGo = new GameObject("Static", typeof(RectTransform), typeof(RawImage));
            imageGo.transform.SetParent(canvasGo.transform, false);
            var rect = (RectTransform)imageGo.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            noise = new Texture2D(noiseSize.x, noiseSize.y, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Repeat
            };
            pixels = new Color32[noiseSize.x * noiseSize.y];

            overlay = imageGo.GetComponent<RawImage>();
            overlay.texture = noise;
            overlay.raycastTarget = false;
            overlay.color = new Color(1f, 1f, 1f, 0f);
            overlay.enabled = false;
        }

        private void Update()
        {
            float s = CurrentStrength;
            bool on = s > 0.001f;
            if (overlay.enabled != on)
                overlay.enabled = on;
            if (!on || Time.time < nextFrameAt)
                return;

            nextFrameAt = Time.time + 1f / flickerRate;
            FillNoise();
            // Flicker: some frames almost clear, so it reads as an unstable signal.
            float alpha = maxAlpha * s * (Random.value < 0.25f ? 0.3f : 1f);
            overlay.color = new Color(1f, 1f, 1f, alpha);
            overlay.uvRect = new Rect(Random.value, Random.value, 1f, 1f);
        }

        // Grey static with darker/brighter rows and the odd bright tear line.
        private void FillNoise()
        {
            int w = noiseSize.x, h = noiseSize.y;
            for (int y = 0; y < h; y++)
            {
                bool tear = Random.value < tearChance;
                float row = Random.Range(0.6f, 1.1f);
                for (int x = 0; x < w; x++)
                {
                    byte v = tear
                        ? (byte)Random.Range(200, 256)
                        : (byte)Mathf.Clamp(Random.Range(0, 256) * row, 0f, 255f);
                    pixels[y * w + x] = new Color32(v, v, v, 255);
                }
            }
            noise.SetPixels32(pixels);
            noise.Apply(false);
        }
    }
}
