using System.Collections;
using CyberWatch.Threat;
using UnityEngine;

namespace CyberWatch.Environment
{
    /// <summary>
    /// Lampu jalan di luar ikut gelisah saat Threat naik, tapi TIDAK PERNAH mati permanen
    /// (sosok hanya terlihat di cahaya lampu jalan).
    /// - Aman / Waspada: menyala stabil.
    /// - Bahaya: tiap lampu sesekali berkedip singkat (sendiri-sendiri, tidak serempak).
    /// - Kritis: lebih sering, kadang padam sebentar lalu menyala lagi.
    /// Ada di prefab Office_Exterior; di scene tanpa ThreatLevelSystem (Main Menu) lampu tetap stabil.
    /// </summary>
    public class StreetLampFlicker : MonoBehaviour
    {
        [Tooltip("Otomatis dicari di scene kalau kosong.")]
        [SerializeField] private ThreatLevelSystem threat;

        [Header("Lampu (urutan Lights dan Bulbs harus sama)")]
        [SerializeField] private Light[] lights;
        [Tooltip("Rumah lampu yang menyala (material emissive).")]
        [SerializeField] private Renderer[] bulbs;
        [Tooltip("Material rumah lampu saat padam (M_SodiumLamp_Off).")]
        [SerializeField] private Material bulbOffMaterial;

        [Header("Kedip")]
        [Tooltip("Jeda antar kedip per lampu saat Bahaya (detik, acak).")]
        [SerializeField] private Vector2 dangerInterval = new Vector2(8f, 16f);
        [Tooltip("Jeda antar kedip per lampu saat Kritis (detik, acak).")]
        [SerializeField] private Vector2 criticalInterval = new Vector2(3f, 8f);
        [Tooltip("Peluang kedip di Kritis berupa padam sebentar (bukan hanya redup).")]
        [SerializeField, Range(0f, 1f)] private float criticalOutageChance = 0.3f;
        [SerializeField] private Vector2 outageSeconds = new Vector2(0.4f, 1.2f);
        [Tooltip("Seberapa redup saat berkedip (0 = gelap, 1 = tidak berubah).")]
        [SerializeField, Range(0f, 1f)] private float dipLevel = 0.1f;

        private float[] baseIntensity;
        private Material[] bulbOnMaterial;
        private float[] nextFlickerAt;
        private Coroutine[] routines;

        private void Awake()
        {
            if (threat == null)
                threat = FindAnyObjectByType<ThreatLevelSystem>();

            int count = lights != null ? lights.Length : 0;
            baseIntensity = new float[count];
            bulbOnMaterial = new Material[count];
            nextFlickerAt = new float[count];
            routines = new Coroutine[count];
            for (int i = 0; i < count; i++)
            {
                if (lights[i] != null) baseIntensity[i] = lights[i].intensity;
                Renderer bulb = Bulb(i);
                if (bulb != null) bulbOnMaterial[i] = bulb.sharedMaterial;
            }
        }

        private void OnEnable()
        {
            for (int i = 0; i < nextFlickerAt.Length; i++)
                Schedule(i);
        }

        private void OnDisable()
        {
            // Kembalikan semua lampu menyala supaya scene tidak tertinggal gelap.
            for (int i = 0; i < baseIntensity.Length; i++)
            {
                routines[i] = null;
                Set(i, 1f);
            }
        }

        private ThreatStage Stage => threat != null ? threat.Stage : ThreatStage.Safe;

        private void Update()
        {
            ThreatStage stage = Stage;
            if (stage < ThreatStage.Danger)
                return;

            float now = Time.time;
            for (int i = 0; i < nextFlickerAt.Length; i++)
            {
                if (routines[i] != null || now < nextFlickerAt[i])
                    continue;

                bool outage = stage >= ThreatStage.Critical && Random.value < criticalOutageChance;
                routines[i] = StartCoroutine(Flicker(i, outage));
                Schedule(i);
            }
        }

        private void Schedule(int i)
        {
            Vector2 range = Stage >= ThreatStage.Critical ? criticalInterval : dangerInterval;
            nextFlickerAt[i] = Time.time + Random.Range(range.x, range.y);
        }

        // Kedip cepat beberapa kali; kalau "outage", padam sebentar lalu menyala lagi.
        private IEnumerator Flicker(int i, bool outage)
        {
            int flickers = Random.Range(2, 5);
            for (int f = 0; f < flickers; f++)
            {
                Set(i, dipLevel);
                yield return new WaitForSeconds(Random.Range(0.04f, 0.12f));
                Set(i, 1f);
                yield return new WaitForSeconds(Random.Range(0.04f, 0.15f));
            }

            if (outage)
            {
                Set(i, 0f);
                yield return new WaitForSeconds(Random.Range(outageSeconds.x, outageSeconds.y));
                Set(i, dipLevel);
                yield return new WaitForSeconds(0.08f);
                Set(i, 1f);
            }

            routines[i] = null;
        }

        private Renderer Bulb(int i) => bulbs != null && i < bulbs.Length ? bulbs[i] : null;

        private void Set(int i, float multiplier)
        {
            Light lamp = lights[i];
            if (lamp != null)
            {
                lamp.intensity = baseIntensity[i] * multiplier;
                lamp.enabled = multiplier > 0.01f;
            }

            Renderer bulb = Bulb(i);
            if (bulb != null && bulbOffMaterial != null && bulbOnMaterial[i] != null)
                bulb.sharedMaterial = multiplier > 0.5f ? bulbOnMaterial[i] : bulbOffMaterial;
        }
    }
}
