using CyberWatch.Threat;
using UnityEngine;

namespace CyberWatch.Environment
{
    /// <summary>
    /// Sosok di luar jendela (Threat Silhouette) yang bereaksi pada Threat Level.
    /// - Di bawah 51%: tidak terlihat.
    /// - Bahaya (51-75%): berdiri diam jauh di area parkir.
    /// - Kritis (76-99%): berjalan pelan makin dekat ke kaca setiap Threat naik.
    /// - Threat turun: sosok mundur lagi, lalu menghilang kalau sudah di bawah 51%.
    /// Tidak pernah melompat atau mengagetkan: geraknya pelan dan tanpa suara.
    /// </summary>
    public class ThreatSilhouette : MonoBehaviour
    {
        [Header("Referensi")]
        [Tooltip("Otomatis dicari di scene kalau kosong.")]
        [SerializeField] private ThreatLevelSystem threat;
        [Tooltip("Model sosok (prefab Slender yang ditaruh di scene).")]
        [SerializeField] private Transform figure;
        [Tooltip("Titik jauh (area parkir).")]
        [SerializeField] private Transform farPoint;
        [Tooltip("Titik terdekat (tepat di luar kaca).")]
        [SerializeField] private Transform nearPoint;
        [Tooltip("Sosok selalu menghadap ke sini. Kosong = Main Camera.")]
        [SerializeField] private Transform lookTarget;

        [Header("Ambang (sesuai dokumen desain)")]
        [SerializeField, Range(0f, 100f)] private float dangerThreshold = 51f;
        [SerializeField, Range(0f, 100f)] private float criticalThreshold = 76f;

        [Header("Gerak")]
        [Tooltip("Kecepatan jalan sosok (meter per detik). Makin kecil makin pelan dan serem.")]
        [SerializeField, Min(0.05f)] private float moveSpeed = 0.8f;
        [Tooltip("Putar kalau model terlihat membelakangi kamera (coba 180).")]
        [SerializeField] private float yawOffset;

        private Vector3 targetPosition;
        private bool wantVisible;

        private void Awake()
        {
            if (threat == null)
                threat = FindAnyObjectByType<ThreatLevelSystem>();
            if (lookTarget == null && Camera.main != null)
                lookTarget = Camera.main.transform;
        }

        private void OnEnable()
        {
            if (threat == null || figure == null || farPoint == null || nearPoint == null)
            {
                Debug.LogWarning("[ThreatSilhouette] Referensi belum lengkap (threat/figure/farPoint/nearPoint).", this);
                enabled = false;
                return;
            }

            threat.ThreatChanged += OnThreatChanged;
            Evaluate(threat.Value, snap: true);
        }

        private void OnDisable()
        {
            if (threat != null)
                threat.ThreatChanged -= OnThreatChanged;
        }

        private void OnThreatChanged(float value) => Evaluate(value, snap: false);

        // Uji cepat: saat Play, klik kanan header komponen ini > Debug > ...
        [ContextMenu("Debug/Set Threat 10 (Aman)")]
        private void DebugSafe() => threat.SetValue(10f);

        [ContextMenu("Debug/Set Threat 55 (Bahaya)")]
        private void DebugDanger() => threat.SetValue(55f);

        [ContextMenu("Debug/Set Threat 80 (Kritis)")]
        private void DebugCritical() => threat.SetValue(80f);

        [ContextMenu("Debug/Set Threat 95 (Kritis, hampir)")]
        private void DebugAlmostLost() => threat.SetValue(95f);

        private void Evaluate(float value, bool snap)
        {
            wantVisible = value >= dangerThreshold;

            if (value < criticalThreshold)
            {
                targetPosition = farPoint.position;
            }
            else
            {
                // 76% = sudah agak maju, 99% = hampir menempel kaca.
                float t = Mathf.Lerp(0.2f, 1f, Mathf.InverseLerp(criticalThreshold, 100f, value));
                targetPosition = Vector3.Lerp(farPoint.position, nearPoint.position, t);
            }

            if (wantVisible && !figure.gameObject.activeSelf)
            {
                figure.position = snap ? targetPosition : farPoint.position;
                figure.gameObject.SetActive(true);
            }
            else if (snap && !wantVisible)
            {
                figure.position = farPoint.position;
                figure.gameObject.SetActive(false);
            }
        }

        private void Update()
        {
            if (!figure.gameObject.activeSelf)
                return;

            figure.position = Vector3.MoveTowards(figure.position, targetPosition, moveSpeed * Time.deltaTime);

            if (lookTarget != null)
            {
                Vector3 dir = lookTarget.position - figure.position;
                dir.y = 0f;
                if (dir.sqrMagnitude > 0.001f)
                    figure.rotation = Quaternion.Euler(0f, yawOffset, 0f) * Quaternion.LookRotation(dir);
            }

            // Threat sudah turun di bawah ambang: setelah sosok mundur sampai titik jauh, baru hilang.
            if (!wantVisible && (figure.position - farPoint.position).sqrMagnitude < 0.01f)
                figure.gameObject.SetActive(false);
        }
    }
}