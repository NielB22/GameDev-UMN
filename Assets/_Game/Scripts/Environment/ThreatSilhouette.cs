using System.Collections;
using CyberWatch.Threat;
using UnityEngine;

namespace CyberWatch.Environment
{
    /// <summary>
    /// Sosok di luar jendela (Threat Silhouette) yang bereaksi pada Threat Level.
    /// - Waspada (26-50%): sesekali terlihat sekilas di kejauhan lalu hilang (opsional).
    /// - Bahaya (51-75%): berdiri jauh di area parkir.
    /// - Kritis (76-99%): makin dekat.
    /// - Threat turun: sosok mundur, lalu hilang di bawah 51%.
    ///
    /// Supaya tegang terus (bukan hanya saat Threat berubah), sosok juga "merayap":
    /// tiap beberapa detik ia maju sedikit walau pemain tidak salah. Mundur separuh
    /// kalau pemain berhasil memblokir penipuan.
    ///
    /// Tanpa jumpscare: tidak ada suara keras dan tidak ada gerakan cepat ke arah kamera.
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
        [Tooltip("Titik terdekat (di luar kaca).")]
        [SerializeField] private Transform nearPoint;
        [Tooltip("Sosok selalu menghadap ke sini. Kosong = Main Camera.")]
        [SerializeField] private Transform lookTarget;

        // Ambang stage TIDAK disimpan di sini: memakai ThreatLevelSystem (StageFor + konstanta),
        // supaya sosok, lampu, CCTV, dan bar Threat selalu berganti stage di nilai yang sama.

        [Header("Gerak")]
        [Tooltip("Menghilang sebentar lalu muncul di posisi baru (tidak terlihat berjalan).")]
        [SerializeField] private bool blinkMove = true;
        [SerializeField] private Vector2 blinkSeconds = new Vector2(0.3f, 0.8f);
        [Tooltip("Hanya dipakai kalau Blink Move mati (meter per detik).")]
        [SerializeField, Min(0.05f)] private float moveSpeed = 0.8f;
        [Tooltip("Putar kalau model terlihat membelakangi kamera (coba 180).")]
        [SerializeField] private float yawOffset;

        [Header("Merayap (tegang terus walau Threat tidak berubah)")]
        [SerializeField] private bool creep = true;
        [Tooltip("Jeda antar langkah merayap (detik, acak).")]
        [SerializeField] private Vector2 creepIntervalSeconds = new Vector2(10f, 22f);
        [Tooltip("Seberapa jauh maju per langkah (0-1 dari jarak jauh ke dekat).")]
        [SerializeField, Range(0f, 0.5f)] private float creepStep = 0.06f;
        [Tooltip("Batas total tambahan dari merayap.")]
        [SerializeField, Range(0f, 0.6f)] private float creepMax = 0.3f;

        [Header("Sekilas di Waspada (opsional)")]
        [SerializeField] private bool glimpseInAlert = true;
        [SerializeField] private Vector2 glimpseIntervalSeconds = new Vector2(15f, 30f);
        [SerializeField] private Vector2 glimpseDuration = new Vector2(1.2f, 2.4f);

        [Header("Tampilan seram")]
        [Tooltip("Kecepatan animasi Idle. Lebih pelan = terasa tidak wajar. 1 = normal.")]
        [SerializeField, Min(0.05f)] private float animatorSpeed = 0.4f;
        [Tooltip("Kepala/badan miring ke samping, makin besar saat makin dekat.")]
        [SerializeField, Range(0f, 15f)] private float leanDegrees = 6f;
        [Tooltip("Kedutan singkat (putar mendadak lalu kembali).")]
        [SerializeField, Range(0f, 25f)] private float twitchDegrees = 10f;
        [SerializeField] private Vector2 twitchIntervalSeconds = new Vector2(3f, 8f);

        [Header("Animasi jalan (hanya kalau Blink Move mati)")]
        [Tooltip("Nama parameter Bool di Animator Controller yang memutar animasi Walk.")]
        [SerializeField] private string walkingBoolName = "Walking";
        [Tooltip("Kecepatan animasi saat berjalan. Samakan dengan Move Speed supaya kaki tidak meluncur.")]
        [SerializeField, Min(0.05f)] private float walkAnimatorSpeed = 0.7f;

        private Animator animator;
        private bool hasWalkingParam;
        private bool isWalking;
        private Vector3 targetPosition;
        private bool wantVisible;
        private float currentValue;
        private float creepProgress;
        private float nextCreepAt;
        private float nextGlimpseAt;
        private float nextTwitchAt;
        private float twitchUntil;
        private float twitchYaw;
        private Coroutine blinkRoutine;
        private Coroutine glimpseRoutine;

        private void Awake()
        {
            if (threat == null)
                threat = FindAnyObjectByType<ThreatLevelSystem>();
            if (lookTarget == null && Camera.main != null)
                lookTarget = Camera.main.transform;

            if (figure != null)
            {
                animator = figure.GetComponentInChildren<Animator>(true);
                if (animator != null)
                {
                    animator.speed = animatorSpeed;
                    foreach (AnimatorControllerParameter p in animator.parameters)
                    {
                        if (p.name == walkingBoolName && p.type == AnimatorControllerParameterType.Bool)
                            hasWalkingParam = true;
                    }
                }
            }
        }

        private void SetWalking(bool walking)
        {
            if (animator == null || walking == isWalking)
                return;

            isWalking = walking;
            animator.speed = walking ? walkAnimatorSpeed : animatorSpeed;
            if (hasWalkingParam)
                animator.SetBool(walkingBoolName, walking);
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
            currentValue = threat.Value;
            creepProgress = 0f;
            ScheduleCreep();
            ScheduleGlimpse();
            ScheduleTwitch();
            Refresh(snap: true);
        }

        private void OnDisable()
        {
            if (threat != null)
                threat.ThreatChanged -= OnThreatChanged;
            blinkRoutine = null;
            glimpseRoutine = null;
        }

        private void OnThreatChanged(float value)
        {
            // Berhasil memblokir penipuan: sosok mundur, merayapnya ikut berkurang.
            if (value < currentValue)
                creepProgress *= 0.5f;

            currentValue = value;
            Refresh(snap: false);
        }

        // Uji cepat: menu debug ada di komponen Threat Level System (klik kanan header > Debug).

        private ThreatStage CurrentStage => ThreatLevelSystem.StageFor(currentValue);

        // 0 = di titik jauh, 1 = di titik dekat.
        private float Progress()
        {
            float p;
            ThreatStage stage = CurrentStage;
            if (stage < ThreatStage.Danger)
                p = 0f;
            else if (stage == ThreatStage.Danger)
                p = Mathf.Lerp(0f, 0.3f, Mathf.InverseLerp(ThreatLevelSystem.DangerAbove, ThreatLevelSystem.CriticalAbove, currentValue));
            else
                p = Mathf.Lerp(0.3f, 1f, Mathf.InverseLerp(ThreatLevelSystem.CriticalAbove, ThreatLevelSystem.Max, currentValue));

            return Mathf.Clamp01(p + creepProgress);
        }

        private void Refresh(bool snap)
        {
            wantVisible = CurrentStage >= ThreatStage.Danger;
            if (!wantVisible)
                creepProgress = 0f;

            targetPosition = Vector3.Lerp(farPoint.position, nearPoint.position, Progress());

            if (wantVisible && glimpseRoutine != null)
            {
                StopCoroutine(glimpseRoutine);
                glimpseRoutine = null;
            }

            if (snap)
            {
                figure.position = wantVisible ? targetPosition : farPoint.position;
                figure.gameObject.SetActive(wantVisible);
                return;
            }

            bool active = figure.gameObject.activeSelf;

            if (wantVisible && !active)
            {
                if (blinkRoutine == null)
                {
                    figure.position = blinkMove ? targetPosition : farPoint.position;
                    figure.gameObject.SetActive(true);
                }
                else
                {
                    StartBlink(true);
                }
                return;
            }

            if (!blinkMove)
                return; // mode jalan: dikerjakan Update()

            bool needsMove = (figure.position - targetPosition).sqrMagnitude > 0.04f;
            if (active && (needsMove || !wantVisible))
                StartBlink(wantVisible);
        }

        private void StartBlink(bool visibleAfter)
        {
            if (blinkRoutine != null)
                StopCoroutine(blinkRoutine);
            blinkRoutine = StartCoroutine(BlinkTo(visibleAfter));
        }

        private IEnumerator BlinkTo(bool visibleAfter)
        {
            figure.gameObject.SetActive(false);
            yield return new WaitForSeconds(Random.Range(blinkSeconds.x, blinkSeconds.y));

            // Posisi dibaca setelah menunggu, supaya selalu memakai target terbaru.
            figure.position = visibleAfter ? targetPosition : farPoint.position;
            figure.gameObject.SetActive(visibleAfter);
            blinkRoutine = null;
        }

        private IEnumerator Glimpse()
        {
            figure.position = farPoint.position;
            figure.gameObject.SetActive(true);
            yield return new WaitForSeconds(Random.Range(glimpseDuration.x, glimpseDuration.y));

            if (!wantVisible)
                figure.gameObject.SetActive(false);
            glimpseRoutine = null;
            ScheduleGlimpse();
        }

        private void ScheduleCreep() =>
            nextCreepAt = Time.time + Random.Range(creepIntervalSeconds.x, creepIntervalSeconds.y);

        private void ScheduleGlimpse() =>
            nextGlimpseAt = Time.time + Random.Range(glimpseIntervalSeconds.x, glimpseIntervalSeconds.y);

        private void ScheduleTwitch() =>
            nextTwitchAt = Time.time + Random.Range(twitchIntervalSeconds.x, twitchIntervalSeconds.y);

        private void Update()
        {
            float now = Time.time;

            // Merayap: maju sedikit tiap beberapa detik selama Bahaya/Kritis.
            if (wantVisible && creep && now >= nextCreepAt)
            {
                creepProgress = Mathf.Min(creepMax, creepProgress + creepStep);
                ScheduleCreep();
                Refresh(snap: false);
            }

            // Sekilas di Waspada.
            if (!wantVisible && glimpseInAlert && CurrentStage == ThreatStage.Alert
                && glimpseRoutine == null && blinkRoutine == null && now >= nextGlimpseAt)
            {
                glimpseRoutine = StartCoroutine(Glimpse());
            }

            if (!figure.gameObject.activeSelf)
            {
                isWalking = false; // Animator ter-reset saat objek dimatikan
                return;
            }

            if (!blinkMove)
            {
                figure.position = Vector3.MoveTowards(figure.position, targetPosition, moveSpeed * Time.deltaTime);
                SetWalking((figure.position - targetPosition).sqrMagnitude > 0.0004f);

                if (!wantVisible && glimpseRoutine == null
                    && (figure.position - farPoint.position).sqrMagnitude < 0.01f)
                {
                    figure.gameObject.SetActive(false);
                    return;
                }
            }

            if (now >= nextTwitchAt)
            {
                twitchUntil = now + 0.12f;
                twitchYaw = Random.Range(-1f, 1f) * twitchDegrees;
                ScheduleTwitch();
            }

            if (lookTarget != null)
            {
                Vector3 dir = lookTarget.position - figure.position;
                dir.y = 0f;
                if (dir.sqrMagnitude > 0.001f)
                {
                    float yaw = yawOffset + (now < twitchUntil ? twitchYaw : 0f);
                    float lean = leanDegrees * Progress();
                    figure.rotation = Quaternion.Euler(0f, yaw, 0f)
                                      * Quaternion.LookRotation(dir)
                                      * Quaternion.Euler(0f, 0f, lean);
                }
            }
        }
    }
}