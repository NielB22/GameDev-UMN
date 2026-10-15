using System.Collections;
using CyberWatch.Effects;
using CyberWatch.Threat;
using UnityEngine;

namespace CyberWatch.Environment
{
    /// <summary>
    /// Sosok di luar jendela (Threat Silhouette) yang bereaksi pada Threat Level.
    /// Sosok hanya terlihat di genangan cahaya lampu jalan (di luar gelap total).
    /// - Waspada (26-50%): sesekali LEWAT sekilas, berjalan menyeberangi cahaya salah satu
    ///   lampu jalan jauh lalu hilang di kegelapan.
    /// - Bahaya (51-75%): berjalan cepat dari kegelapan, berhenti tepat di bawah salah satu
    ///   lampu jalan jauh, lalu diam menatap pemain.
    /// - Kritis (76-99%): pindah ke bawah lampu parkir, lalu makin dekat ke jendela.
    /// - Threat turun: sosok mundur, lalu hilang di bawah 51%.
    ///
    /// Jalur: lampu jauh (dipilih acak) -> lampu parkir (Far Point) -> dekat kaca (Near Point).
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
        [Tooltip("Titik di bawah lampu-lampu jalan jauh. Bahaya: berdiri di salah satunya. " +
                 "Waspada: lewat menyeberangi cahayanya. Kosong = pakai Far Point.")]
        [SerializeField] private Transform[] lampPoints;
        [Tooltip("Titik di bawah lampu parkir (awal Kritis).")]
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
        [Tooltip("Kalau target sejauh ini atau lebih (meter), sosok menghilang lalu muncul di sana " +
                 "(misal dari lampu jauh ke lampu parkir), bukan berjalan lama di kegelapan.")]
        [SerializeField, Min(0.5f)] private float blinkAboveDistance = 3f;
        [Tooltip("Putar kalau model terlihat membelakangi kamera (coba 180).")]
        [SerializeField] private float yawOffset;

        [Header("Merayap (tegang terus walau Threat tidak berubah)")]
        [SerializeField] private bool creep = true;
        [Tooltip("Jeda antar langkah merayap (detik, acak).")]
        [SerializeField] private Vector2 creepIntervalSeconds = new Vector2(10f, 22f);
        [Tooltip("Seberapa jauh maju per langkah (0-1 dari seluruh jalur).")]
        [SerializeField, Range(0f, 0.5f)] private float creepStep = 0.06f;
        [Tooltip("Batas total tambahan dari merayap.")]
        [SerializeField, Range(0f, 0.6f)] private float creepMax = 0.3f;
        [Tooltip("Di Bahaya sosok tidak maju lebih dari ini (0-0.5 = lampu jauh -> lampu parkir), " +
                 "supaya tetap di cahaya lampu jauh.")]
        [SerializeField, Range(0f, 0.5f)] private float dangerMaxProgress = 0.1f;

        [Header("Datang ke lampu di Bahaya")]
        [Tooltip("Saat pertama muncul di Bahaya, sosok berjalan cepat dari kegelapan lalu berhenti " +
                 "tepat di bawah lampu dan menatap pemain. Mati = langsung muncul di titik lampu.")]
        [SerializeField] private bool arriveWalking = true;
        [Tooltip("Dari sejauh ini (meter, sejajar jalan) sosok mulai berjalan ke titik lampu.")]
        [SerializeField, Min(0.5f)] private float arriveDistance = 7f;
        [SerializeField, Min(0.1f)] private float arriveSpeed = 2.8f;
        [Tooltip("Kecepatan animasi jalan saat datang (lebih cepat dari biasa).")]
        [SerializeField, Min(0.05f)] private float arriveAnimatorSpeed = 1.8f;

        [Header("Lewat sekilas di Waspada")]
        [SerializeField] private bool passByInAlert = true;
        [Tooltip("Jeda antar kemunculan (detik, acak).")]
        [SerializeField] private Vector2 passIntervalSeconds = new Vector2(12f, 25f);
        [Tooltip("Arah jalan (sejajar jalan raya).")]
        [SerializeField] private Vector3 passDirection = Vector3.right;
        [Tooltip("Jarak jalan dari titik lampu ke tiap sisi (meter). Mulai & selesai di kegelapan.")]
        [SerializeField, Min(1f)] private float passHalfLength = 9f;
        [SerializeField, Min(0.1f)] private float passSpeed = 1.3f;
        [Tooltip("Kecepatan animasi jalan saat lewat.")]
        [SerializeField, Min(0.05f)] private float passAnimatorSpeed = 1f;

        [Header("Glitch layar (ScreenGlitch)")]
        [Tooltip("Kekuatan statik saat sosok menghilang / muncul di tempat baru (0 = tanpa).")]
        [SerializeField, Range(0f, 1f)] private float blinkGlitch = 0.6f;
        [Tooltip("Kekuatan statik saat sosok mulai datang ke lampu di Bahaya.")]
        [SerializeField, Range(0f, 1f)] private float arriveGlitch = 0.45f;

        [Header("Tampilan seram")]
        [Tooltip("Kecepatan animasi Idle. Lebih pelan = terasa tidak wajar. 1 = normal.")]
        [SerializeField, Min(0.05f)] private float animatorSpeed = 0.4f;
        [Tooltip("Kepala/badan miring ke samping, makin besar saat makin dekat.")]
        [SerializeField, Range(0f, 15f)] private float leanDegrees = 6f;
        [Tooltip("Kedutan singkat (putar mendadak lalu kembali).")]
        [SerializeField, Range(0f, 25f)] private float twitchDegrees = 10f;
        [SerializeField] private Vector2 twitchIntervalSeconds = new Vector2(3f, 8f);

        [Header("Animasi jalan")]
        [Tooltip("Nama parameter Bool di Animator Controller yang memutar animasi Walk.")]
        [SerializeField] private string walkingBoolName = "Walking";
        [Tooltip("Kecepatan animasi saat berjalan. Samakan dengan Move Speed supaya kaki tidak meluncur.")]
        [SerializeField, Min(0.05f)] private float walkAnimatorSpeed = 0.7f;

        /// <summary>The figure model (for effects that react to the player looking at it).</summary>
        public Transform Figure => figure;

        /// <summary>True while the figure is shown in the world.</summary>
        public bool FigureVisible => figure != null && figure.gameObject.activeInHierarchy;

        private Animator animator;
        private bool hasWalkingParam;
        private bool isWalking;
        private Vector3 targetPosition;
        private bool wantVisible;
        private float currentValue;
        private float creepProgress;
        private float nextCreepAt;
        private float nextPassAt;
        private float nextTwitchAt;
        private float twitchUntil;
        private float twitchYaw;
        private Transform lampPoint;
        private Coroutine blinkRoutine;
        private Coroutine passRoutine;
        private Coroutine arriveRoutine;

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

        private void SetWalking(bool walking, float walkSpeed)
        {
            if (animator == null || walking == isWalking)
                return;

            isWalking = walking;
            animator.speed = walking ? walkSpeed : animatorSpeed;
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
            PickLamp();
            ScheduleCreep();
            SchedulePass();
            ScheduleTwitch();
            Refresh(snap: true);
        }

        private void OnDisable()
        {
            if (threat != null)
                threat.ThreatChanged -= OnThreatChanged;
            blinkRoutine = null;
            passRoutine = null;
            arriveRoutine = null;
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

        private bool HasLamps => lampPoints != null && lampPoints.Length > 0;

        // Lampu jauh tempat sosok berdiri di Bahaya (dipilih acak tiap kali ia muncul lagi).
        private void PickLamp()
        {
            lampPoint = farPoint;
            if (!HasLamps)
                return;
            Transform pick = lampPoints[Random.Range(0, lampPoints.Length)];
            if (pick != null)
                lampPoint = pick;
        }

        // 0 = lampu jauh, 0.5 = lampu parkir (Far Point), 1 = dekat kaca (Near Point).
        private float Progress()
        {
            ThreatStage stage = CurrentStage;
            if (stage < ThreatStage.Danger)
                return 0f;
            if (stage == ThreatStage.Danger)
            {
                float danger = Mathf.Lerp(0f, dangerMaxProgress * 0.5f,
                    Mathf.InverseLerp(ThreatLevelSystem.DangerAbove, ThreatLevelSystem.CriticalAbove, currentValue));
                return Mathf.Min(dangerMaxProgress, danger + creepProgress);
            }
            float critical = Mathf.Lerp(0.5f, 1f,
                Mathf.InverseLerp(ThreatLevelSystem.CriticalAbove, ThreatLevelSystem.Max, currentValue));
            return Mathf.Clamp01(critical + creepProgress);
        }

        private Vector3 PathPosition(float p)
        {
            Vector3 start = lampPoint != null ? lampPoint.position : farPoint.position;
            return p <= 0.5f
                ? Vector3.Lerp(start, farPoint.position, p * 2f)
                : Vector3.Lerp(farPoint.position, nearPoint.position, (p - 0.5f) * 2f);
        }

        private Vector3 HiddenPosition => lampPoint != null ? lampPoint.position : farPoint.position;

        private void Refresh(bool snap)
        {
            bool wasVisible = wantVisible;
            wantVisible = CurrentStage >= ThreatStage.Danger;
            if (!wantVisible)
                creepProgress = 0f;
            if (wantVisible && !wasVisible)
                PickLamp();

            targetPosition = PathPosition(Progress());

            if (wantVisible && passRoutine != null)
            {
                StopCoroutine(passRoutine);
                passRoutine = null;
                figure.gameObject.SetActive(false);
            }

            // Sedang berjalan ke lampu, tapi stage berubah: hentikan, lanjut dengan aturan biasa.
            if (arriveRoutine != null && (!wantVisible || CurrentStage != ThreatStage.Danger))
            {
                StopCoroutine(arriveRoutine);
                arriveRoutine = null;
                SetWalking(false, walkAnimatorSpeed);
            }

            if (snap)
            {
                figure.position = wantVisible ? targetPosition : HiddenPosition;
                figure.gameObject.SetActive(wantVisible);
                return;
            }

            if (arriveRoutine != null)
                return; // Arrive() membaca target terbaru sendiri

            bool active = figure.gameObject.activeSelf;

            // Baru muncul di Bahaya: berjalan cepat dari gelap ke bawah lampu.
            if (wantVisible && !wasVisible && arriveWalking && CurrentStage == ThreatStage.Danger)
            {
                if (blinkRoutine != null)
                {
                    StopCoroutine(blinkRoutine);
                    blinkRoutine = null;
                }
                arriveRoutine = StartCoroutine(Arrive());
                ScreenGlitch.Trigger(arriveGlitch);
                return;
            }

            if (wantVisible && !active)
            {
                if (blinkRoutine == null)
                {
                    figure.position = targetPosition;
                    figure.gameObject.SetActive(true);
                }
                else
                {
                    StartBlink(true);
                }
                return;
            }

            bool farJump = (figure.position - targetPosition).sqrMagnitude > blinkAboveDistance * blinkAboveDistance;
            if (!blinkMove && !farJump && wantVisible)
                return; // berjalan: dikerjakan Update()

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
            figure.position = visibleAfter ? targetPosition : HiddenPosition;
            figure.gameObject.SetActive(visibleAfter);
            ScreenGlitch.Trigger(blinkGlitch); // menutupi "teleport" sosok
            blinkRoutine = null;
        }

        // Bahaya: datang cepat dari kegelapan (sejajar jalan), berhenti tepat di bawah lampu.
        // Setelah sampai, Update() memutarnya menatap pemain.
        private IEnumerator Arrive()
        {
            Vector3 dir = WalkDirection();
            figure.position = targetPosition - dir * arriveDistance;
            figure.rotation = Quaternion.LookRotation(dir) * Quaternion.Euler(0f, yawOffset, 0f);
            figure.gameObject.SetActive(true);
            isWalking = false;
            SetWalking(true, arriveAnimatorSpeed);

            while ((figure.position - targetPosition).sqrMagnitude > 0.0004f)
            {
                figure.position = Vector3.MoveTowards(figure.position, targetPosition, arriveSpeed * Time.deltaTime);
                yield return null;
            }

            figure.position = targetPosition;
            SetWalking(false, walkAnimatorSpeed);
            arriveRoutine = null;
        }

        // Sejajar jalan, arah acak (kiri ke kanan atau sebaliknya).
        private Vector3 WalkDirection()
        {
            Vector3 dir = passDirection;
            dir.y = 0f;
            return (dir.sqrMagnitude > 0.001f ? dir.normalized : Vector3.right) * (Random.value < 0.5f ? -1f : 1f);
        }

        // Waspada: berjalan menyeberangi cahaya satu lampu jauh, dari gelap ke gelap.
        private IEnumerator PassBy()
        {
            Transform lamp = HasLamps ? lampPoints[Random.Range(0, lampPoints.Length)] : farPoint;
            Vector3 dir = WalkDirection();
            Vector3 end = lamp.position + dir * passHalfLength;
            figure.position = lamp.position - dir * passHalfLength;
            figure.rotation = Quaternion.LookRotation(dir) * Quaternion.Euler(0f, yawOffset, 0f);
            figure.gameObject.SetActive(true);
            isWalking = false;
            SetWalking(true, passAnimatorSpeed);

            while ((figure.position - end).sqrMagnitude > 0.01f)
            {
                figure.position = Vector3.MoveTowards(figure.position, end, passSpeed * Time.deltaTime);
                yield return null;
            }

            SetWalking(false, passAnimatorSpeed);
            figure.gameObject.SetActive(false);
            passRoutine = null;
            SchedulePass();
        }

        private void ScheduleCreep() =>
            nextCreepAt = Time.time + Random.Range(creepIntervalSeconds.x, creepIntervalSeconds.y);

        private void SchedulePass() =>
            nextPassAt = Time.time + Random.Range(passIntervalSeconds.x, passIntervalSeconds.y);

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

            // Lewat sekilas di Waspada.
            if (!wantVisible && passByInAlert && CurrentStage == ThreatStage.Alert
                && passRoutine == null && blinkRoutine == null && now >= nextPassAt)
            {
                passRoutine = StartCoroutine(PassBy());
            }

            if (passRoutine != null || arriveRoutine != null)
                return; // arah & gerak diatur PassBy() / Arrive()

            if (!figure.gameObject.activeSelf)
            {
                isWalking = false; // Animator ter-reset saat objek dimatikan
                return;
            }

            if (!blinkMove && blinkRoutine == null)
            {
                figure.position = Vector3.MoveTowards(figure.position, targetPosition, moveSpeed * Time.deltaTime);
                SetWalking((figure.position - targetPosition).sqrMagnitude > 0.0004f, walkAnimatorSpeed);
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
