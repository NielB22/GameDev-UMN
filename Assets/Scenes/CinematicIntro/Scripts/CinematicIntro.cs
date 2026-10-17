using System;
using System.Collections;
using System.Collections.Generic;
using CyberWatch.Audio;
using CyberWatch.Core;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace CyberWatch.Cinematic
{
    public enum CueKind { Sound, StaticHit, FlickerLight, ShowObject, HideObject, Shake }

    public enum SoundId
    {
        None, StaticShort, Riser, PowerDown, LightFlicker, MailNotify,
        CrtOff, Whisper, UiClick, FlashlightClick, HorrorDrone, Heartbeat
    }

    /// <summary>
    /// Sinematik pembuka bergaya film (Main Menu -> sinematik -> Gameplay).
    ///
    /// Urutan: kartu studio -> COLD OPEN (shot kota, kantor, pengintai) -> JUDUL menghantam layar
    /// -> shot masuk ke kantor dan meja kerja -> panel CARA BERMAIN -> mulai shift.
    ///
    /// Tiap shot punya: jalur kamera (titik Transform), FOV, goyang kamera, fokus (depth of field),
    /// warna (color grade), kabut, subtitle, dan CUE (suara, kedip lampu, statik, munculnya sosok, getar).
    /// Overlay gaya rekaman: [REC], jam yang menghitung mundur ke 00:00:00, dan nama lokasi.
    /// Spasi / Enter / Esc melewati sinematik.
    ///
    /// Scene diperbarui otomatis lewat menu CyberWatch > Upgrade Cinematic to Film (v2).
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
        public class ShotLook
        {
            [Tooltip("Ditambahkan di atas warna profile (PP_Night).")]
            [Range(-2f, 2f)] public float exposure;
            [Range(-50f, 50f)] public float contrast;
            [Range(-60f, 60f)] public float saturation;
            [Tooltip("Negatif = dingin (biru), positif = hangat (oranye).")]
            [Range(-40f, 40f)] public float temperature;
            [Range(0f, 2f)] public float bloom;
            [Range(0f, 0.5f)] public float vignette;
            [Range(0f, 0.5f)] public float chromatic;
            [Range(0f, 0.5f)] public float grain;
        }

        [Serializable]
        public class Cue
        {
            public CueKind kind;
            [Tooltip("Detik sejak shot mulai.")] public float time;
            public SoundId sound;
            [Range(0f, 1f)] public float volume = 0.8f;
            [Tooltip("Kosong = suara 'di kepala' (2D). Diisi = suara datang dari titik ini (3D).")]
            public Transform soundAt;
            [Tooltip("Suara dari luar kantor (teredam).")] public bool outside;
            [Tooltip("Mulai suara supaya SELESAI tepat di akhir shot (untuk riser menuju judul).")]
            public bool fromEnd;
            public Light light;
            [Tooltip("Lama kedip lampu / lama statik / lama getar (detik).")] public float seconds = 1f;
            public GameObject target;
            [Tooltip("Kekuatan statik (0-1) atau getar (derajat).")] public float amount = 0.5f;
        }

        [Serializable]
        public class Shot
        {
            public string name = "Shot";
            [Tooltip("Posisi + arah depan tiap titik menentukan jalur dan arah pandang kamera.")]
            public Transform[] points;
            [Min(1f)] public float duration = 8f;
            [Range(20f, 90f)] public float fov = 60f;
            [Tooltip("Kalau > 0, FOV berubah pelan dari FOV awal ke nilai ini (push-in / zoom).")]
            [Min(0f)] public float fovEnd;
            [Tooltip("0 = kecepatan konstan, 1 = pelan di awal dan akhir.")]
            [Range(0f, 1f)] public float ease = 0.5f;
            [Tooltip("Goyang halus kamera (meter).")] [Min(0f)] public float handheld = 0.01f;
            [Tooltip("Kalau diisi, kamera selalu menatap objek ini (menggantikan arah titik).")]
            public Transform lookAt;
            [Tooltip("Teks lokasi di pojok kiri atas, mis. EXT. JALAN RAYA - MALAM.")]
            public string stamp;
            [Tooltip("Objek yang aktif selama shot ini.")]
            public GameObject reveal;

            [Header("Fokus (depth of field)")]
            [Tooltip("Kosong = semua tajam. Diisi = kamera fokus ke objek ini, latar buram.")]
            public Transform focusTarget;
            [Tooltip("Kalau diisi, fokus berpindah pelan dari objek di atas ke objek ini (rack focus).")]
            public Transform focusTargetEnd;
            [Range(0f, 1f)] public float rackAt = 0.5f;
            [Range(0.05f, 1f)] public float rackLength = 0.3f;
            [Tooltip("Makin kecil = latar makin buram.")] [Range(1f, 16f)] public float dofAperture = 2.4f;
            [Range(20f, 200f)] public float dofFocalLength = 75f;

            [Header("Suasana")]
            [Tooltip("Kepadatan kabut (0 = tanpa kabut). Luar ruangan sekitar 0.02.")]
            [Min(0f)] public float fog;
            public ShotLook look = new ShotLook();

            public Caption[] captions;
            public Cue[] cues;
        }

        [Serializable]
        public class Page
        {
            public string title;
            [TextArea(6, 18)] public string body;
        }

        private enum State { Intro, Playing, Tutorial, Leaving }

        private struct ScheduledCue
        {
            public float time;
            public Cue cue;
        }

        // ---------- pengaturan ----------

        [Header("Kamera")]
        [Tooltip("Kosong = Main Camera.")]
        [SerializeField] private Camera cam;
        [Tooltip("Jarak titik pandang di depan tiap titik (dipakai untuk menghitung arah).")]
        [SerializeField, Min(1f)] private float lookDistance = 6f;

        [Header("Alur")]
        [SerializeField] private Shot[] shots;
        [Tooltip("Judul muncul SETELAH shot dengan nomor ini (0 = setelah shot pertama). -1 = judul di awal.")]
        [SerializeField] private int titleAfterShot = -1;
        [Tooltip("Lama judul bertahan di layar sebelum memudar.")]
        [SerializeField, Min(0f)] private float titleSeconds = 2.2f;
        [Tooltip("Lama layar menghitam saat pindah antar shot.")]
        [SerializeField, Min(0.1f)] private float cutFadeSeconds = 0.45f;
        [SerializeField, Min(0.1f)] private float fadeOutSeconds = 0.9f;

        [Header("Judul (gaya film)")]
        [SerializeField] private TMP_Text titleLabel;
        [SerializeField] private TMP_Text subtitleLabel;
        [SerializeField] private SoundId titleSound = SoundId.PowerDown;
        [SerializeField, Range(0f, 1f)] private float titleSoundVolume = 0.9f;

        [Header("Kartu studio")]
        [SerializeField] private CanvasGroup studioGroup;
        [SerializeField, Min(0f)] private float studioSeconds = 2.2f;

        [Header("Overlay kamera (REC, jam, lokasi)")]
        [SerializeField] private CanvasGroup overlayGroup;
        [SerializeField] private TMP_Text recLabel;
        [SerializeField] private TMP_Text timecodeText;
        [SerializeField] private TMP_Text stampText;
        [Tooltip("Jam di pojok kanan atas berjalan sekian kali lebih cepat dan tepat 00:00:00 saat judul muncul.")]
        [SerializeField, Min(1f)] private float clockRate = 4f;

        [Header("Statik (efek pindah shot)")]
        [SerializeField] private RawImage staticOverlay;

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
        [SerializeField] private Button nextButton;
        [SerializeField] private TMP_Text nextButtonLabel;
        [SerializeField] private Page[] pages;

        [Header("Tampilan film")]
        [Tooltip("Global Volume di scene. Otomatis dicari kalau kosong.")]
        [SerializeField] private Volume volume;
        [Tooltip("Vignette minimum selama sinematik.")]
        [SerializeField, Range(0f, 1f)] private float vignetteIntensity = 0.3f;
        [Tooltip("Film grain minimum selama sinematik.")]
        [SerializeField, Range(0f, 1f)] private float grainIntensity = 0.28f;
        [Tooltip("Matikan kalau layar terasa terlalu buram atau berat.")]
        [SerializeField] private bool depthOfField = true;
        [SerializeField] private Color fogColor = new Color(0.02f, 0.03f, 0.05f);
        [Tooltip("Seberapa cepat warna berubah antar shot.")]
        [SerializeField, Min(0.5f)] private float lookBlendSpeed = 3f;

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
        private Coroutine overlayRoutine;

        private readonly List<ScheduledCue> queue = new List<ScheduledCue>();
        private int queueIndex;

        private double clock;
        private float clockRateNow = 1f;
        private float shakeEnd;
        private float shakeDuration = 1f;
        private float shakeAmount;
        private float subtitleSpacing;
        private float fogNow;
        private Texture2D noiseTexture;

        // post-processing
        private bool lookReady;
        private ColorAdjustments colorAdj;
        private WhiteBalance whiteBalance;
        private Bloom bloom;
        private Vignette vignette;
        private ChromaticAberration chromatic;
        private FilmGrain grain;
        private DepthOfField dof;
        private float baseExposure, baseContrast, baseSaturation, baseTemperature;
        private float baseBloom, baseVignette, baseChromatic, baseGrain;
        private ShotLook lookNow = new ShotLook();

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
                Debug.LogError("[CinematicIntro] Shot atau kamera belum diisi. Jalankan menu CyberWatch > Upgrade Cinematic to Film (v2).", this);
                enabled = false;
                return;
            }

            SetupLook();
            SetupClock();

            if (tutorialPanel != null) tutorialPanel.SetActive(false);
            if (skipHint != null) skipHint.SetActive(true);
            if (nextButton != null) nextButton.onClick.AddListener(OnNext);
            if (fadeGroup != null) fadeGroup.alpha = 1f;
            if (titleGroup != null) titleGroup.alpha = 0f;
            if (studioGroup != null) studioGroup.alpha = 0f;
            if (overlayGroup != null) overlayGroup.alpha = 0f;
            if (subtitleLabel != null) subtitleSpacing = subtitleLabel.characterSpacing;
            SetLetterbox(0f);
            SetCaption(null, 0f);
            CreateNoise();

            foreach (Shot s in shots)
                if (s.reveal != null) s.reveal.SetActive(false);
            ResetCueObjects();

            StartCoroutine(Run());
        }

        private void OnDestroy()
        {
            if (nextButton != null) nextButton.onClick.RemoveListener(OnNext);
            if (noiseTexture != null) Destroy(noiseTexture);
            RenderSettings.fog = false;
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

            float dt = Time.deltaTime;
            if (state == State.Tutorial || state == State.Leaving)
                shotTime += dt; // goyang halus tetap jalan saat panel cara bermain tampil

            EvaluatePose(currentShot, currentU, shotTime, out Vector3 pos, out Quaternion rot);
            rot = ApplyShake(rot);
            cam.transform.SetPositionAndRotation(pos, rot);
            cam.fieldOfView = currentShot.fovEnd > 0.01f
                ? Mathf.Lerp(currentShot.fov, currentShot.fovEnd, currentU)
                : currentShot.fov;

            UpdateLook(currentShot, pos, rot * Vector3.forward, dt);
            UpdateFog(currentShot, dt);
            UpdateClock(dt);
        }

        // ---------- alur utama ----------

        private IEnumerator Run()
        {
            state = State.Intro;
            BeginShot(shots[0]);

            // 1. Kartu studio.
            if (studioGroup != null && studioSeconds > 0f)
            {
                yield return TweenGroup(studioGroup, 0f, 1f, 0.9f);
                yield return WaitSeconds(studioSeconds);
                yield return TweenGroup(studioGroup, studioGroup.alpha, 0f, 0.6f);
            }

            // 2. Judul di awal (kalau tidak memakai cold open).
            if (titleAfterShot < 0 && !skipRequested)
                yield return TitleSlam();

            // 3. Shot demi shot.
            if (!skipRequested)
            {
                StartLetterbox(letterboxHeight, 1.2f);
                ShowOverlay(1f, 1.0f);

                for (int i = 0; i < shots.Length && !skipRequested; i++)
                {
                    Shot shot = shots[i];
                    bool last = i == shots.Length - 1;

                    BeginShot(shot);
                    state = State.Playing;
                    StartFade(0f, cutFadeSeconds);
                    ScheduleCues(shot);

                    float t = 0f;
                    bool fadingOut = false;
                    while (t < shot.duration && !skipRequested)
                    {
                        t += Time.deltaTime;
                        shotTime = t;
                        currentU = Ease(t / shot.duration, shot.ease);
                        UpdateCaptions(shot, t);
                        RunCues(t);

                        if (!last && !fadingOut && t >= shot.duration - cutFadeSeconds)
                        {
                            fadingOut = true;
                            StartFade(1f, cutFadeSeconds);
                        }
                        yield return null;
                    }

                    if (shot.reveal != null) shot.reveal.SetActive(false);
                    SetCaption(null, 0f);

                    if (i == titleAfterShot && !skipRequested && !last)
                    {
                        yield return TitleSlam();
                        ShowOverlay(1f, 0.2f);
                    }
                }
            }

            // 4. Berhenti di pose akhir shot terakhir, lalu tampilkan cara bermain.
            foreach (Shot s in shots)
                if (s.reveal != null) s.reveal.SetActive(false);
            ResetCueObjects();

            Shot finalShot = shots[shots.Length - 1];
            BeginShot(finalShot);
            currentU = 1f;
            ShowFinalCueObjects(finalShot);
            SetCaption(null, 0f);
            StartLetterbox(0f, 0.8f);
            SetOverlay(0f);
            StartFade(0f, 0.8f);

            if (skipHint != null) skipHint.SetActive(false);
            state = State.Tutorial;
            ShowPage(0);
        }

        private IEnumerator TitleSlam()
        {
            SetOverlay(0f);
            SetCaption(null, 0f);
            yield return WaitSeconds(0.55f); // jeda hitam sebelum judul

            clock = 86400.0;  // tepat tengah malam
            clockRateNow = 1f;
            PlayId(titleSound, titleSoundVolume);
            StartCoroutine(StaticFlash(0.28f, 0.9f));
            TriggerShake(0.5f, 0.45f);

            if (titleGroup != null) titleGroup.alpha = 1f;
            if (subtitleLabel != null) subtitleLabel.alpha = 0f;

            RectTransform rt = titleLabel != null ? titleLabel.rectTransform : null;
            Vector2 basePos = rt != null ? rt.anchoredPosition : Vector2.zero;

            // Judul menghantam: membesar lalu mengecil dengan getar horizontal.
            float t = 0f;
            while (t < 0.8f && !skipRequested)
            {
                t += Time.deltaTime;
                float inv = 1f - Mathf.Clamp01(t / 0.8f);
                if (rt != null)
                {
                    rt.localScale = Vector3.one * Mathf.Lerp(1f, 1.3f, inv * inv);
                    rt.anchoredPosition = basePos + new Vector2(UnityEngine.Random.Range(-1f, 1f) * 22f * inv, 0f);
                }
                yield return null;
            }
            if (rt != null)
            {
                rt.localScale = Vector3.one;
                rt.anchoredPosition = basePos;
            }

            // Subtitle muncul sambil jarak antar huruf merapat.
            float s = 0f;
            while (s < 1.4f && !skipRequested)
            {
                s += Time.deltaTime;
                float k = Mathf.Clamp01(s / 1.4f);
                if (subtitleLabel != null)
                {
                    subtitleLabel.alpha = k;
                    subtitleLabel.characterSpacing = Mathf.Lerp(subtitleSpacing + 40f, subtitleSpacing, k);
                }
                yield return null;
            }
            if (subtitleLabel != null)
            {
                subtitleLabel.alpha = 1f;
                subtitleLabel.characterSpacing = subtitleSpacing;
            }

            yield return WaitSeconds(titleSeconds);
            yield return TweenGroup(titleGroup, titleGroup != null ? titleGroup.alpha : 0f, 0f, 0.8f);
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

        // ---------- cue (suara, lampu, statik, objek, getar) ----------

        private void ScheduleCues(Shot shot)
        {
            queue.Clear();
            queueIndex = 0;
            if (shot.cues == null)
                return;

            foreach (Cue c in shot.cues)
            {
                if (c == null)
                    continue;

                float t = c.time;
                if (c.fromEnd)
                {
                    AudioClip clip = Resolve(c.sound);
                    float length = clip != null ? clip.length : c.seconds;
                    t = Mathf.Max(0f, shot.duration - length - 0.05f);
                }
                queue.Add(new ScheduledCue { time = t, cue = c });
            }

            queue.Sort((a, b) => a.time.CompareTo(b.time));
        }

        private void RunCues(float t)
        {
            while (queueIndex < queue.Count && t >= queue[queueIndex].time)
                Fire(queue[queueIndex++].cue);
        }

        private void Fire(Cue c)
        {
            switch (c.kind)
            {
                case CueKind.Sound:
                    PlayCueSound(c);
                    break;

                case CueKind.StaticHit:
                    StartCoroutine(StaticFlash(c.seconds > 0.01f ? c.seconds : 0.18f, c.amount));
                    PlayCueSound(c);
                    break;

                case CueKind.FlickerLight:
                    if (c.light != null)
                        StartCoroutine(FlickerLight(c.light, c.seconds));
                    PlayCueSound(c);
                    break;

                case CueKind.ShowObject:
                    if (c.target != null) c.target.SetActive(true);
                    PlayCueSound(c);
                    break;

                case CueKind.HideObject:
                    if (c.target != null) c.target.SetActive(false);
                    break;

                case CueKind.Shake:
                    TriggerShake(c.seconds, c.amount);
                    break;
            }
        }

        private void PlayCueSound(Cue c)
        {
            AudioClip clip = Resolve(c.sound);
            if (clip == null)
                return;

            if (c.soundAt == null)
                GameAudio.Play2D(clip, AudioChannel.Sfx, c.volume);
            else if (c.outside)
                GameAudio.PlayOutside(clip, c.soundAt.position, AudioChannel.Sfx, c.volume);
            else
                GameAudio.PlayAt(clip, c.soundAt.position, AudioChannel.Sfx, c.volume);
        }

        private static void PlayId(SoundId id, float volume)
        {
            AudioClip clip = Resolve(id);
            if (clip != null)
                GameAudio.Play2D(clip, AudioChannel.Sfx, volume);
        }

        private static AudioClip Resolve(SoundId id)
        {
            AudioLibrary lib = GameAudio.Clips;
            if (lib == null)
                return null;

            switch (id)
            {
                case SoundId.StaticShort: return lib.staticShort;
                case SoundId.Riser: return lib.blackoutRiser;
                case SoundId.PowerDown: return lib.powerDown;
                case SoundId.LightFlicker: return lib.lightFlicker;
                case SoundId.MailNotify: return lib.mailNotify;
                case SoundId.CrtOff: return lib.crtOff;
                case SoundId.Whisper: return lib.slenderVoice;
                case SoundId.UiClick: return lib.uiClick;
                case SoundId.FlashlightClick: return lib.flashlightClick;
                case SoundId.HorrorDrone: return lib.horrorDrone;
                case SoundId.Heartbeat: return lib.heartbeatLoop;
                default: return null;
            }
        }

        private IEnumerator FlickerLight(Light l, float seconds)
        {
            float baseIntensity = l.intensity;
            float t = 0f;
            while (t < seconds)
            {
                l.intensity = baseIntensity * (UnityEngine.Random.value < 0.5f
                    ? UnityEngine.Random.Range(0f, 0.25f)
                    : UnityEngine.Random.Range(0.7f, 1.1f));
                float wait = UnityEngine.Random.Range(0.03f, 0.12f);
                t += wait;
                yield return new WaitForSeconds(wait);
            }
            l.intensity = baseIntensity;
        }

        private IEnumerator StaticFlash(float seconds, float amount)
        {
            if (staticOverlay == null)
                yield break;

            float t = 0f;
            while (t < seconds)
            {
                t += Time.deltaTime;
                float k = 1f - Mathf.Clamp01(t / seconds);
                Color c = staticOverlay.color;
                c.a = amount * k * UnityEngine.Random.Range(0.55f, 1f);
                staticOverlay.color = c;
                staticOverlay.uvRect = new Rect(UnityEngine.Random.value, UnityEngine.Random.value,
                                                1.5f, 1.5f * UnityEngine.Random.Range(0.8f, 1.2f));
                yield return null;
            }

            Color end = staticOverlay.color;
            end.a = 0f;
            staticOverlay.color = end;
        }

        private void CreateNoise()
        {
            if (staticOverlay == null)
                return;

            const int size = 128;
            noiseTexture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Point
            };

            var pixels = new Color32[size * size];
            for (int i = 0; i < pixels.Length; i++)
            {
                byte v = (byte)UnityEngine.Random.Range(0, 256);
                pixels[i] = new Color32(v, v, v, 255);
            }
            noiseTexture.SetPixels32(pixels);
            noiseTexture.Apply(false, false);

            staticOverlay.texture = noiseTexture;
            staticOverlay.color = new Color(1f, 1f, 1f, 0f);
        }

        private void TriggerShake(float seconds, float degrees)
        {
            shakeDuration = Mathf.Max(0.05f, seconds);
            shakeEnd = Time.time + shakeDuration;
            shakeAmount = degrees;
        }

        private Quaternion ApplyShake(Quaternion rot)
        {
            float now = Time.time;
            if (now >= shakeEnd)
                return rot;

            float k = (shakeEnd - now) / shakeDuration;
            float amp = shakeAmount * k * k;
            float n = now * 18f;
            return rot * Quaternion.Euler(Noise(seed + 51f, n) * amp, Noise(seed + 63f, n) * amp, Noise(seed + 77f, n) * amp * 0.5f);
        }

        private void ResetCueObjects()
        {
            foreach (Shot s in shots)
            {
                if (s.cues == null) continue;
                foreach (Cue c in s.cues)
                    if (c != null && c.kind == CueKind.ShowObject && c.target != null)
                        c.target.SetActive(false);
            }
        }

        private static void ShowFinalCueObjects(Shot shot)
        {
            if (shot.cues == null)
                return;
            foreach (Cue c in shot.cues)
                if (c != null && c.kind == CueKind.ShowObject && c.target != null)
                    c.target.SetActive(true);
        }

        // ---------- kamera ----------

        private void BeginShot(Shot shot)
        {
            currentShot = shot;
            currentU = 0f;
            shotTime = 0f;
            if (cam != null)
                cam.fieldOfView = shot.fov;
            if (shot.reveal != null)
                shot.reveal.SetActive(true);
            if (stampText != null)
                stampText.text = shot.stamp ?? string.Empty;
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

                pos = CatmullRom(PosPoint(p, seg - 1), PosPoint(p, seg), PosPoint(p, seg + 1), PosPoint(p, seg + 2), t);
                target = CatmullRom(LookPoint(p, seg - 1), LookPoint(p, seg), LookPoint(p, seg + 1), LookPoint(p, seg + 2), t);
            }

            if (shot.lookAt != null)
                target = shot.lookAt.position;

            Vector3 dir = target - pos;
            Quaternion rot = Quaternion.LookRotation(dir.sqrMagnitude > 0.0001f ? dir : Vector3.forward);

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

        private static Vector3 PosPoint(Transform[] p, int i) => p[Mathf.Clamp(i, 0, p.Length - 1)].position;

        private Vector3 LookPoint(Transform[] p, int i)
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

        // ---------- warna, fokus, kabut, jam ----------

        private void SetupLook()
        {
            UniversalAdditionalCameraData data = cam.GetUniversalAdditionalCameraData();
            if (data != null)
                data.renderPostProcessing = true;

            if (volume == null)
                volume = FindAnyObjectByType<Volume>();
            if (volume == null)
                return;

            // .profile = salinan khusus saat Play, file profile aslinya (PP_Night) tidak berubah.
            VolumeProfile profile = volume.profile;

            colorAdj = Ensure<ColorAdjustments>(profile, out _);
            whiteBalance = Ensure<WhiteBalance>(profile, out _);
            bloom = Ensure<Bloom>(profile, out _);
            vignette = Ensure<Vignette>(profile, out bool vignetteExisted);
            chromatic = Ensure<ChromaticAberration>(profile, out _);
            grain = Ensure<FilmGrain>(profile, out bool grainExisted);
            dof = Ensure<DepthOfField>(profile, out _);

            if (!vignetteExisted)
                vignette.smoothness.Override(0.5f);
            if (!grainExisted)
                grain.type.Override(FilmGrainLookup.Thin1);

            baseExposure = Base(colorAdj.postExposure, 0f);
            baseContrast = Base(colorAdj.contrast, 0f);
            baseSaturation = Base(colorAdj.saturation, 0f);
            baseTemperature = Base(whiteBalance.temperature, 0f);
            baseBloom = Base(bloom.intensity, 0f);
            baseVignette = Base(vignette.intensity, 0f);
            baseChromatic = Base(chromatic.intensity, 0f);
            baseGrain = Base(grain.intensity, 0f);

            lookReady = true;
        }

        private static T Ensure<T>(VolumeProfile profile, out bool existed) where T : VolumeComponent
        {
            existed = profile.TryGet(out T component);
            if (!existed)
                component = profile.Add<T>(true);
            component.active = true;
            return component;
        }

        private static float Base(VolumeParameter<float> parameter, float fallback) =>
            parameter.overrideState ? parameter.value : fallback;

        private void UpdateLook(Shot shot, Vector3 camPos, Vector3 camForward, float dt)
        {
            if (!lookReady)
                return;

            ShotLook target = shot.look ?? new ShotLook();
            float k = 1f - Mathf.Exp(-lookBlendSpeed * dt);
            lookNow.exposure = Mathf.Lerp(lookNow.exposure, target.exposure, k);
            lookNow.contrast = Mathf.Lerp(lookNow.contrast, target.contrast, k);
            lookNow.saturation = Mathf.Lerp(lookNow.saturation, target.saturation, k);
            lookNow.temperature = Mathf.Lerp(lookNow.temperature, target.temperature, k);
            lookNow.bloom = Mathf.Lerp(lookNow.bloom, target.bloom, k);
            lookNow.vignette = Mathf.Lerp(lookNow.vignette, target.vignette, k);
            lookNow.chromatic = Mathf.Lerp(lookNow.chromatic, target.chromatic, k);
            lookNow.grain = Mathf.Lerp(lookNow.grain, target.grain, k);

            colorAdj.postExposure.Override(baseExposure + lookNow.exposure);
            colorAdj.contrast.Override(baseContrast + lookNow.contrast);
            colorAdj.saturation.Override(baseSaturation + lookNow.saturation);
            whiteBalance.temperature.Override(baseTemperature + lookNow.temperature);
            bloom.intensity.Override(baseBloom + lookNow.bloom);
            vignette.intensity.Override(Mathf.Clamp01(Mathf.Max(baseVignette, vignetteIntensity) + lookNow.vignette));
            chromatic.intensity.Override(Mathf.Clamp01(baseChromatic + lookNow.chromatic));
            grain.intensity.Override(Mathf.Clamp01(Mathf.Max(baseGrain, grainIntensity) + lookNow.grain));

            if (depthOfField && shot.focusTarget != null)
            {
                float distance = FocusDistance(shot.focusTarget, camPos, camForward);
                if (shot.focusTargetEnd != null)
                {
                    float distanceEnd = FocusDistance(shot.focusTargetEnd, camPos, camForward);
                    float rack = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((currentU - shot.rackAt) / Mathf.Max(0.01f, shot.rackLength)));
                    distance = Mathf.Lerp(distance, distanceEnd, rack);
                }

                dof.mode.Override(DepthOfFieldMode.Bokeh);
                dof.focusDistance.Override(distance);
                dof.aperture.Override(shot.dofAperture);
                dof.focalLength.Override(shot.dofFocalLength);
            }
            else
            {
                dof.mode.Override(DepthOfFieldMode.Off);
            }
        }

        private static float FocusDistance(Transform target, Vector3 camPos, Vector3 camForward) =>
            Mathf.Max(0.5f, Vector3.Dot(target.position - camPos, camForward));

        private void UpdateFog(Shot shot, float dt)
        {
            fogNow = Mathf.MoveTowards(fogNow, shot.fog, dt * 0.08f);
            bool on = fogNow > 0.0005f;
            RenderSettings.fog = on;
            if (on)
            {
                RenderSettings.fogMode = FogMode.ExponentialSquared;
                RenderSettings.fogColor = fogColor;
                RenderSettings.fogDensity = fogNow;
            }
        }

        private void SetupClock()
        {
            clockRateNow = clockRate;
            if (titleAfterShot < 0 || titleAfterShot >= shots.Length)
            {
                clock = 23.0 * 3600.0 + 58.0 * 60.0;
                clockRateNow = 1f;
                return;
            }

            // Perkiraan lama sampai judul, supaya jam tiba di 00:00:00 hampir tepat saat judul menghantam.
            float coldOpen = studioSeconds + 1.5f;
            for (int i = 0; i <= titleAfterShot; i++)
                coldOpen += shots[i].duration;
            coldOpen += titleAfterShot * cutFadeSeconds;

            clock = Math.Max(0.0, 86400.0 - clockRate * coldOpen);
        }

        private void UpdateClock(float dt)
        {
            if (state == State.Intro || state == State.Playing)
                clock += dt * clockRateNow;

            if (timecodeText != null)
            {
                int s = (int)(clock % 86400.0);
                timecodeText.text = $"{s / 3600:00}:{s / 60 % 60:00}:{s % 60:00}";
            }

            if (recLabel != null)
                recLabel.alpha = Mathf.Sin(Time.time * 5f) > 0f ? 1f : 0.2f;
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

        private void ShowOverlay(float target, float delay)
        {
            if (overlayGroup == null)
                return;
            if (overlayRoutine != null) StopCoroutine(overlayRoutine);
            overlayRoutine = StartCoroutine(OverlayRoutine(target, delay));
        }

        private void SetOverlay(float alpha)
        {
            if (overlayRoutine != null)
            {
                StopCoroutine(overlayRoutine);
                overlayRoutine = null;
            }
            if (overlayGroup != null)
                overlayGroup.alpha = alpha;
        }

        private IEnumerator OverlayRoutine(float target, float delay)
        {
            if (delay > 0f)
                yield return new WaitForSeconds(delay);
            yield return TweenGroup(overlayGroup, overlayGroup.alpha, target, 0.5f);
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

            cam.fieldOfView = shot.fovEnd > 0.01f ? Mathf.Lerp(shot.fov, shot.fovEnd, previewProgress) : shot.fov;
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
                        Vector3 pt = CatmullRom(PosPoint(shot.points, seg - 1), PosPoint(shot.points, seg),
                                                PosPoint(shot.points, seg + 1), PosPoint(shot.points, seg + 2), f - seg);
                        Gizmos.DrawLine(prev, pt);
                        prev = pt;
                    }
                }
            }
        }
#endif
    }
}