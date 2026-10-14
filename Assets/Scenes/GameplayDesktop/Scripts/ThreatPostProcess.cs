using CyberWatch.Threat;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace CyberWatch.Environment
{
    /// <summary>
    /// Efek layar yang mengikuti Threat Level:
    /// - Vignette (pinggiran layar menggelap) makin kuat seiring Threat naik.
    /// - Mulai Bahaya, layar perlahan berubah kemerahan dan sedikit pudar.
    /// - Saat Kritis, vignette berdenyut pelan seperti detak jantung (tanpa suara, tanpa kejutan).
    ///
    /// Memakai SALINAN profile milik Global Volume saat Play, jadi file profile aslinya
    /// (yang dipakai scene lain) tidak berubah.
    /// Letakkan script ini di Assets/Scenes/GameplayDesktop/Scripts/.
    /// </summary>
    public class ThreatPostProcess : MonoBehaviour
    {
        [Tooltip("Otomatis dicari di scene kalau kosong.")]
        [SerializeField] private ThreatLevelSystem threat;
        [Tooltip("Global Volume di scene. Otomatis dicari kalau kosong.")]
        [SerializeField] private Volume volume;

        [Header("Vignette")]
        [SerializeField, Range(0f, 1f)] private float vignetteSafe = 0.2f;
        [SerializeField, Range(0f, 1f)] private float vignetteCritical = 0.55f;

        [Header("Tint merah (mulai Bahaya)")]
        [Tooltip("Threat (%) saat tint mulai muncul.")]
        [SerializeField, Range(0f, 100f)] private float tintStart = 50f;
        [SerializeField] private Color criticalTint = new Color(1f, 0.55f, 0.5f);
        [SerializeField, Range(-100f, 0f)] private float criticalSaturation = -25f;

        [Header("Denyut (Kritis)")]
        [SerializeField, Range(0f, 0.2f)] private float pulseAmount = 0.06f;
        [SerializeField, Min(0.1f)] private float pulseSpeed = 2.4f;

        [Header("Halus")]
        [Tooltip("Persen per detik efek mengejar nilai Threat sebenarnya.")]
        [SerializeField, Min(1f)] private float followSpeed = 30f;

        private Vignette vignette;
        private ColorAdjustments colorAdjustments;
        private float shown;

        private void Awake()
        {
            if (threat == null)
                threat = FindAnyObjectByType<ThreatLevelSystem>();
            if (volume == null)
                volume = FindAnyObjectByType<Volume>();
        }

        private void OnEnable()
        {
            if (threat == null || volume == null)
            {
                Debug.LogWarning("[ThreatPostProcess] ThreatLevelSystem atau Volume tidak ditemukan.", this);
                enabled = false;
                return;
            }

            // .profile = salinan khusus saat Play (bukan file aslinya).
            VolumeProfile profile = volume.profile;

            if (!profile.TryGet(out vignette))
                vignette = profile.Add<Vignette>(true);
            if (!profile.TryGet(out colorAdjustments))
                colorAdjustments = profile.Add<ColorAdjustments>(true);

            vignette.active = true;
            colorAdjustments.active = true;
            vignette.smoothness.Override(0.45f);

            shown = threat.Value;
            Apply();
        }

        private void Update()
        {
            shown = Mathf.MoveTowards(shown, threat.Value, followSpeed * Time.deltaTime);
            Apply();
        }

        private void Apply()
        {
            float t = shown / ThreatLevelSystem.Max;
            float tintT = Mathf.InverseLerp(tintStart, ThreatLevelSystem.Max, shown);
            float pulse = Mathf.Sin(Time.time * pulseSpeed) * pulseAmount * tintT;

            vignette.intensity.Override(Mathf.Clamp01(Mathf.Lerp(vignetteSafe, vignetteCritical, t) + pulse));
            vignette.color.Override(Color.Lerp(Color.black, new Color(0.22f, 0f, 0f), tintT));

            colorAdjustments.colorFilter.Override(Color.Lerp(Color.white, criticalTint, tintT));
            colorAdjustments.saturation.Override(Mathf.Lerp(0f, criticalSaturation, tintT));
        }
    }
}