using System;
using System.Collections;
using CyberWatch.Core;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace CyberWatch.Cinematic
{
    /// <summary>
    /// Sinematik pembuka (Main Menu -> sinematik -> Gameplay).
    ///
    /// Urutan: kartu judul -> beberapa SHOT kamera (kota, kantor dari luar, sosok di kejauhan,
    /// terbang masuk lewat jendela) dengan subtitle -> panel CARA BERMAIN -> mulai shift.
    ///
    /// Setiap shot = rangkaian titik (Transform). Posisi titik = posisi kamera, arah depan (forward)
    /// titik = arah pandang. Kamera meluncur mulus (Catmull-Rom) lewat titik-titik itu.
    /// Spasi / Enter / Esc melewati sinematik.
    ///
    /// Scene dibuat otomatis lewat menu CyberWatch > Build Cinematic Scene.
    /// </summary>
    public class CinematicIntro : MonoBehaviour
    {
        // ---------- tipe data (tampil di Inspector) ----------

        [Serializable]
        public class Caption
        {
            [TextArea(1, 3)] public string text;
            [Tooltip("Detik sejak shot mulai.")] public float start;
            public float duration = 3.5f;
        }

        [Serializable]
        public class Shot
        {
            public string name = "Shot";
            [Tooltip("Posisi + arah depan tiap titik menentukan jalur dan arah pandang kamera.")]
            public Transform[] points;
            [Min(1f)] public float duration = 8f;
            [Range(20f, 90f)] public float fov = 60f;
            [Tooltip("0 = kecepatan konstan, 1 = pelan di awal dan akhir.")]
            [Range(0f, 1f)] public float ease = 0.5f;
            [Tooltip("Goyang halus kamera (meter).")] [Min(0f)] public float handheld = 0.01f;
            [Tooltip("Objek yang hanya aktif selama shot ini (mis. sosok di kejauhan).")]
            public GameObject reveal;
            public Caption[] captions;
        }

        [Serializable]
        public class Page
        {
            public string title;
            [TextArea(6, 18)] public string body;
        }

        private enum State { Intro, Playing, Tutorial, Leaving }

        // ---------- pengaturan ----------

        [Header("Kamera")]
        [Tooltip("Kosong = Main Camera.")]
        [SerializeField] private Camera cam;
        [Tooltip("Jarak titik pandang di depan tiap titik (dipakai untuk menghitung arah).")]
        [SerializeField, Min(1f)] private float lookDistance = 6f;

        [Header("Alur")]
        [SerializeField] private Shot[] shots;
        [SerializeField, Min(0f)] private float titleSeconds = 3.5f;
        [Tooltip("Lama layar menghitam saat pindah antar shot.")]
        [SerializeField, Min(0.1f)] private float cutFadeSeconds = 0.45f;
        [SerializeField, Min(0.1f)] private float fadeOutSeconds = 0.9f;

        [Header("UI - sinematik")]
        [SerializeField] private CanvasGroup fadeGroup;
        [SerializeField] private CanvasGroup titleGroup;
        [SerializeField] private TMP_Text captionText;
        [SerializeField] private RectTransform letterboxTop;
        [SerializeField] private RectTransform letterboxBottom;
        [SerializeField, Min(0f)] private float letterboxHeight = 130f;
        [SerializeField] private GameObject skipHint;

        [Header("UI - cara bermain")]
        [SerializeField] private GameObject tutorialPanel;
        [SerializeField] private TMP_Text tutorialTitle;
        [SerializeField] private TMP_Text tutorialBody;
        [SerializeField] private UnityEngine.UI.Button nextButton;
        [SerializeField] private TMP_Text nextButtonLabel;
        [SerializeField] private Page[] pages;

        [Header("Tampilan film")]
        [Tooltip("Global Volume di scene. Otomatis dicari kalau kosong.")]
        [SerializeField] private Volume volume;
        [SerializeField, Range(0f, 1f)] private float vignetteIntensity = 0.38f;
        [SerializeField, Range(0f, 1f)] private float grainIntensity = 0.3f;

        [Header("Pratinjau di editor (tanpa Play)")]
        [SerializeField] private bool previewInEditor;
        [SerializeField, Min(0)] private int previewShot;
        [SerializeField, Range(0f, 1f)] private float previewProgress;

        // ---------- status ----------

        private State state;
        private bool skipRequested;
        private Shot currentShot;
        private float currentU;
        private float shotTime;
        private float seed;
        private int pageIndex;
        private Coroutine fadeRoutine;
        private Coroutine letterboxRoutine;

        // ---------- siklus hidup ----------

        private void Awake()
        {
            Time.timeScale = 1f;
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            seed = UnityEngine.Random.value * 100f;

            if (cam == null)
                cam = Camera.main;
        }

        private void Start()
        {
            if (shots == null || shots.Length == 0 || cam == null)
            {
                Debug.LogError("[CinematicIntro] Shot atau kamera belum diisi. Jalankan menu CyberWatch > Build Cinematic Scene.", this);
                enabled = false;
                return;
            }

            SetupLook();

            if (tutorialPanel != null) tutorialPanel.SetActive(false);
            if (skipHint != null) skipHint.SetActive(true);
            if (nextButton != null) nextButton.onClick.AddListener(OnNext);
            if (fadeGroup != null) fadeGroup.alpha = 1f;
            if (titleGroup != null) titleGroup.alpha = 0f;
            SetLetterbox(0f);
            SetCaption(null, 0f);

            foreach (Shot s in shots)
                if (s.reveal != null) s.reveal.SetActive(false);

            StartCoroutine(Run());
        }

        private void OnDestroy()
        {
            if (nextButton != null) nextButton.onClick.RemoveListener(OnNext);
        }

        private void Update()
        {
            Keyboard kb = Keyboard.current;
            if (kb == null)
                return;

            bool pressed = kb.spaceKey.wasPressedThisFrame
                           || kb.enterKey.wasPressedThisFrame
                           || kb.numpadEnterKey.wasPressedThisFrame
                           || kb.escapeKey.wasPressedThisFrame;
            if (!pressed)
                return;

            if (state == State.Intro || state == State.Playing)
                skipRequested = true;
            else if (state == State.Tutorial && !kb.escapeKey.wasPressedThisFrame)
                OnNext();
        }

        private void LateUpdate()
        {
            if (currentShot == null || cam == null)
                return;

            if (state == State.Tutorial || state == State.Leaving)
                shotTime += Time.deltaTime; // goyang halus tetap jalan saat panel cara bermain tampil

            EvaluatePose(currentShot, currentU, shotTime, out Vector3 pos, out Quaternion rot);
            cam.transform.SetPositionAndRotation(pos, rot);
        }

        // ---------- alur utama ----------

        private IEnumerator Run()
        {
            state = State.Intro;
            BeginShot(shots[0], 0f);

            // 1. Kartu judul di layar hitam.
            yield return TweenGroup(titleGroup, 0f, 1f, 1f);
            yield return WaitSeconds(titleSeconds);
            yield return TweenGroup(titleGroup, titleGroup != null ? titleGroup.alpha : 0f, 0f, 0.8f);

            // 2. Shot demi shot.
            if (!skipRequested)
            {
                StartLetterbox(letterboxHeight, 1.2f);

                for (int i = 0; i < shots.Length && !skipRequested; i++)
                {
                    Shot shot = shots[i];
                    bool last = i == shots.Length - 1;

                    BeginShot(shot, 0f);
                    state = State.Playing;
                    StartFade(0f, cutFadeSeconds);

                    float t = 0f;
                    bool fadingOut = false;
                    while (t < shot.duration && !skipRequested)
                    {
                        t += Time.deltaTime;
                        shotTime = t;
                        currentU = Ease(t / shot.duration, shot.ease);
                        UpdateCaptions(shot, t);

                        if (!last && !fadingOut && t >= shot.duration - cutFadeSeconds)
                        {
                            fadingOut = true;
                            StartFade(1f, cutFadeSeconds);
                        }
                        yield return null;
                    }

                    if (shot.reveal != null) shot.reveal.SetActive(false);
                    SetCaption(null, 0f);
                }
            }

            // 3. Berhenti di pose akhir shot terakhir, lalu tampilkan cara bermain.
            foreach (Shot s in shots)
                if (s.reveal != null) s.reveal.SetActive(false);

            Shot finalShot = shots[shots.Length - 1];
            BeginShot(finalShot, 1f);
            if (finalShot.reveal != null) finalShot.reveal.SetActive(true);
            SetCaption(null, 0f);
            StartLetterbox(0f, 0.8f);
            StartFade(0f, 0.8f);

            if (skipHint != null) skipHint.SetActive(false);
            state = State.Tutorial;
            ShowPage(0);
        }

        private IEnumerator Leave()
        {
            state = State.Leaving;
            if (nextButton != null) nextButton.interactable = false;

            StartFade(1f, fadeOutSeconds);
            yield return new WaitForSeconds(fadeOutSeconds + 0.05f);

            GameManager.Instance.StartNewShift();
        }

        // ---------- tutorial ----------

        private void ShowPage(int index)
        {
            if (pages == null || pages.Length == 0)
            {
                // Tidak ada halaman: langsung mulai.
                StartCoroutine(Leave());
                return;
            }

            pageIndex = Mathf.Clamp(index, 0, pages.Length - 1);
            if (tutorialPanel != null) tutorialPanel.SetActive(true);
            if (tutorialTitle != null) tutorialTitle.text = pages[pageIndex].title;
            if (tutorialBody != null) tutorialBody.text = pages[pageIndex].body;
            if (nextButtonLabel != null)
                nextButtonLabel.text = pageIndex < pages.Length - 1 ? "LANJUT" : "MULAI SHIFT";
        }

        private void OnNext()
        {
            if (state != State.Tutorial)
                return;

            if (pages != null && pageIndex < pages.Length - 1)
                ShowPage(pageIndex + 1);
            else
                StartCoroutine(Leave());
        }

        // ---------- kamera ----------

        private void BeginShot(Shot shot, float u)
        {
            currentShot = shot;
            currentU = u;
            shotTime = 0f;
            cam.fieldOfView = shot.fov;
            if (shot.reveal != null && u < 1f)
                shot.reveal.SetActive(true);
        }

        private static float Ease(float x, float amount)
        {
            x = Mathf.Clamp01(x);
            return Mathf.Lerp(x, Mathf.SmoothStep(0f, 1f, x), amount);
        }

        private void EvaluatePose(Shot shot, float u, float time, out Vector3 position, out Quaternion rotation)
        {
            Transform[] p = shot.points;
            int n = p != null ? p.Length : 0;
            if (n == 0)
            {
                position = cam.transform.position;
                rotation = cam.transform.rotation;
                return;
            }

            Vector3 pos;
            Vector3 target;

            if (n == 1)
            {
                pos = p[0].position;
                target = pos + p[0].forward * lookDistance;
            }
            else
            {
                float f = Mathf.Clamp01(u) * (n - 1);
                int seg = Mathf.Min((int)f, n - 2);
                float t = f - seg;

                pos = CatmullRom(Pos(p, seg - 1), Pos(p, seg), Pos(p, seg + 1), Pos(p, seg + 2), t);
                target = CatmullRom(Look(p, seg - 1), Look(p, seg), Look(p, seg + 1), Look(p, seg + 2), t);
            }

            Quaternion rot = Quaternion.LookRotation((target - pos).sqrMagnitude > 0.0001f ? target - pos : Vector3.forward);

            if (shot.handheld > 0f)
            {
                float n1 = time * 0.35f;
                Vector3 offset = new Vector3(Noise(seed, n1), Noise(seed + 11f, n1), Noise(seed + 23f, n1)) * shot.handheld;
                pos += rot * offset;
                rot *= Quaternion.Euler(0f, 0f, Noise(seed + 37f, n1) * 0.4f);
            }

            position = pos;
            rotation = rot;
        }

        private Vector3 Pos(Transform[] p, int i) => p[Mathf.Clamp(i, 0, p.Length - 1)].position;

        private Vector3 Look(Transform[] p, int i)
        {
            Transform t = p[Mathf.Clamp(i, 0, p.Length - 1)];
            return t.position + t.forward * lookDistance;
        }

        private static float Noise(float a, float b) => Mathf.PerlinNoise(a, b) * 2f - 1f;

        private static Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            float t2 = t * t;
            float t3 = t2 * t;
            return 0.5f * ((2f * p1)
                           + (-p0 + p2) * t
                           + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2
                           + (-p0 + 3f * p1 - 3f * p2 + p3) * t3);
        }

        // ---------- UI kecil ----------

        private void UpdateCaptions(Shot shot, float t)
        {
            if (shot.captions == null)
            {
                SetCaption(null, 0f);
                return;
            }

            foreach (Caption c in shot.captions)
            {
                if (t < c.start || t > c.start + c.duration)
                    continue;

                float fadeIn = Mathf.Clamp01((t - c.start) / 0.6f);
                float fadeOut = Mathf.Clamp01((c.start + c.duration - t) / 0.6f);
                SetCaption(c.text, Mathf.Min(fadeIn, fadeOut));
                return;
            }

            SetCaption(null, 0f);
        }

        private void SetCaption(string text, float alpha)
        {
            if (captionText == null)
                return;
            if (text != null)
                captionText.text = text;
            captionText.alpha = alpha;
        }

        private void SetLetterbox(float height)
        {
            if (letterboxTop != null)
                letterboxTop.sizeDelta = new Vector2(0f, height);
            if (letterboxBottom != null)
                letterboxBottom.sizeDelta = new Vector2(0f, height);
        }

        private void StartLetterbox(float target, float seconds)
        {
            if (letterboxRoutine != null) StopCoroutine(letterboxRoutine);
            letterboxRoutine = StartCoroutine(LetterboxRoutine(target, seconds));
        }

        private IEnumerator LetterboxRoutine(float target, float seconds)
        {
            float from = letterboxTop != null ? letterboxTop.sizeDelta.y : 0f;
            float t = 0f;
            while (t < seconds)
            {
                t += Time.deltaTime;
                SetLetterbox(Mathf.Lerp(from, target, Mathf.SmoothStep(0f, 1f, t / seconds)));
                yield return null;
            }
            SetLetterbox(target);
        }

        private void StartFade(float target, float seconds)
        {
            if (fadeGroup == null)
                return;
            if (fadeRoutine != null) StopCoroutine(fadeRoutine);
            fadeRoutine = StartCoroutine(TweenGroup(fadeGroup, fadeGroup.alpha, target, seconds));
        }

        private static IEnumerator TweenGroup(CanvasGroup group, float from, float to, float seconds)
        {
            if (group == null)
                yield break;

            float t = 0f;
            while (t < seconds)
            {
                t += Time.deltaTime;
                group.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(t / seconds));
                yield return null;
            }
            group.alpha = to;
        }

        private IEnumerator WaitSeconds(float seconds)
        {
            float t = 0f;
            while (t < seconds && !skipRequested)
            {
                t += Time.deltaTime;
                yield return null;
            }
        }

        // ---------- tampilan film ----------

        private void SetupLook()
        {
            UniversalAdditionalCameraData data = cam.GetUniversalAdditionalCameraData();
            if (data != null)
                data.renderPostProcessing = true;

            if (volume == null)
                volume = FindAnyObjectByType<Volume>();
            if (volume == null)
                return;

            // .profile = salinan khusus saat Play, file profile aslinya tidak berubah.
            VolumeProfile profile = volume.profile;

            if (!profile.TryGet(out Vignette vignette))
                vignette = profile.Add<Vignette>(true);
            vignette.active = true;
            vignette.intensity.Override(vignetteIntensity);
            vignette.smoothness.Override(0.5f);

            if (!profile.TryGet(out FilmGrain grain))
                grain = profile.Add<FilmGrain>(true);
            grain.active = true;
            grain.type.Override(FilmGrainLookup.Thin1);
            grain.intensity.Override(grainIntensity);
        }

        // ---------- bantuan editor ----------

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!previewInEditor || Application.isPlaying || shots == null || shots.Length == 0)
                return;

            if (cam == null)
                cam = Camera.main;
            if (cam == null)
                return;

            Shot shot = shots[Mathf.Clamp(previewShot, 0, shots.Length - 1)];
            if (shot.points == null || shot.points.Length == 0)
                return;

            for (int i = 0; i < shot.points.Length; i++)
                if (shot.points[i] == null)
                    return;

            cam.fieldOfView = shot.fov;
            float savedHandheld = shot.handheld;
            shot.handheld = 0f;
            EvaluatePose(shot, previewProgress, 0f, out Vector3 pos, out Quaternion rot);
            shot.handheld = savedHandheld;
            cam.transform.SetPositionAndRotation(pos, rot);
        }

        private void OnDrawGizmos()
        {
            if (shots == null)
                return;

            for (int s = 0; s < shots.Length; s++)
            {
                Shot shot = shots[s];
                if (shot == null || shot.points == null || shot.points.Length == 0)
                    continue;

                bool valid = true;
                foreach (Transform t in shot.points)
                    if (t == null) { valid = false; break; }
                if (!valid)
                    continue;

                Gizmos.color = Color.HSVToRGB((s * 0.17f) % 1f, 0.7f, 1f);

                foreach (Transform t in shot.points)
                {
                    Gizmos.DrawSphere(t.position, 0.35f);
                    Gizmos.DrawLine(t.position, t.position + t.forward * 2f);
                }

                if (shot.points.Length > 1)
                {
                    Vector3 prev = shot.points[0].position;
                    int steps = (shot.points.Length - 1) * 24;
                    for (int i = 1; i <= steps; i++)
                    {
                        float f = (float)i / steps * (shot.points.Length - 1);
                        int seg = Mathf.Min((int)f, shot.points.Length - 2);
                        Vector3 pt = CatmullRom(Pos(shot.points, seg - 1), Pos(shot.points, seg),
                                                Pos(shot.points, seg + 1), Pos(shot.points, seg + 2), f - seg);
                        Gizmos.DrawLine(prev, pt);
                        prev = pt;
                    }
                }
            }
        }
#endif
    }
}