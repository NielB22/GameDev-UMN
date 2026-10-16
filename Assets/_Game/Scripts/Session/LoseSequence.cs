using System;
using System.Collections;
using System.Collections.Generic;
using CyberWatch.Audio;
using CyberWatch.Effects;
using CyberWatch.Environment;
using CyberWatch.Player;
using TMPro;
using UnityEngine;
using Random = UnityEngine.Random;

namespace CyberWatch.Session
{
    /// <summary>
    /// Ending when the system is Compromised (Threat 100%), played instead of cutting
    /// straight to the report:
    /// 1. every light dies one by one (street lamps, office, desk lamp, monitor), then the moon fades;
    /// 2. when the monitor dies the HUD disappears, the player is forced up (cannot sit again) and
    ///    can already use the flashlight;
    /// 3. 2 s later a hint asks to switch on the flashlight [F] (only if it is still off);
    /// 4. once the light is on, the figure waits in the back-left corner of the office (placed while
    ///    off screen); aiming at its face (or walking up to it) starts the jumpscare -> report.
    /// ShiftController calls Play(); onFinished leads to the report.
    /// </summary>
    public class LoseSequence : MonoBehaviour
    {
        [Header("References (found automatically when empty)")]
        [SerializeField] private PlayerModeController modes;
        [SerializeField] private Flashlight flashlight;
        [SerializeField] private ThreatSilhouette silhouette;
        [SerializeField] private ThreatLightFlicker officeLights;
        [SerializeField] private StreetLampFlicker streetLamps;
        [SerializeField] private MonitorGlow monitor;

        [Header("Blackout")]
        [Tooltip("Pause before the first light dies (seconds).")]
        [SerializeField, Min(0f)] private float startDelay = 1.2f;
        [Tooltip("Pause between two lights dying (seconds).")]
        [SerializeField, Min(0f)] private float betweenLights = 1f;
        [Tooltip("Seconds between the HUD disappearing (monitor off) and the [F] hint.")]
        [SerializeField, Min(0f)] private float promptDelay = 2f;

        [Header("Flashlight prompt")]
        [SerializeField] private string flashlightPrompt = "[F]  NYALAKAN SENTER";
        [SerializeField] private Color promptColor = new Color(1f, 0.92f, 0.76f, 1f);
        [SerializeField, Min(10f)] private float promptSize = 40f;

        [Header("Figure inside the office")]
        [Tooltip("Where the figure waits (world position, feet). Back-left corner seen from the IT desk.")]
        [SerializeField] private Vector3 figureSpot = new Vector3(-3.2f, 0f, -2.4f);
        [Tooltip("The figure only appears there while that spot is out of the player's view.")]
        [SerializeField, Min(0f)] private float figureAppearDelay = 1f;
        [Tooltip("Jumpscare when the view center is within this angle of the face (degrees).")]
        [SerializeField, Range(1f, 30f)] private float faceAngle = 8f;
        [Tooltip("Also triggers if the player walks this close, even without looking at the face.")]
        [SerializeField, Min(0.3f)] private float triggerDistance = 1.6f;
        [Tooltip("Face height below the top of the model (meters).")]
        [SerializeField, Min(0f)] private float faceBelowTop = 0.3f;

        [Header("Jumpscare")]
        [Tooltip("How fast the figure rushes at the camera (m/s).")]
        [SerializeField, Min(1f)] private float chargeSpeed = 14f;
        [Tooltip("Walk animation speed while rushing (fast, unnatural).")]
        [SerializeField, Min(0.1f)] private float chargeAnimatorSpeed = 3f;
        [Tooltip("The face stops this far in front of the camera (meters): small = face fills the screen.")]
        [SerializeField, Min(0.05f)] private float faceStopDistance = 0.28f;
        [Tooltip("Seconds the face stays filling the screen before the cut to black.")]
        [SerializeField, Min(0f)] private float holdSeconds = 0.35f;
        [Tooltip("Camera shake while it rushes in (degrees).")]
        [SerializeField, Range(0f, 5f)] private float shakeDegrees = 1.5f;
        [Tooltip("Seconds of black before the report.")]
        [SerializeField, Min(0f)] private float blackSeconds = 1.5f;

        [Header("Audio levels (0..1, on top of the Jumpscare/SFX volume)")]
        [Tooltip("Riser while the lights die (cut when the monitor dies).")]
        [SerializeField, Range(0f, 1f)] private float blackoutRiserLevel = 0.8f;
        [SerializeField, Range(0f, 1f)] private float powerDownLevel = 0.9f;
        [Tooltip("When the figure starts rushing in.")]
        [SerializeField, Range(0f, 1f)] private float riserLayerLevel = 0.8f;
        [SerializeField, Range(0f, 1f)] private float slenderLevel = 0.9f;
        [Tooltip("When the face fills the screen.")]
        [SerializeField, Range(0f, 1f)] private float screamLevel = 1f;
        [SerializeField, Range(0f, 1f)] private float staticLevel = 0.75f;
        [SerializeField, Range(0f, 1f)] private float impactLevel = 1f;

        private TMP_FontAsset font;
        private GameObject promptCanvas;
        private TMP_Text promptText;

        public bool IsPlaying { get; private set; }

        private void Awake()
        {
            if (modes == null) modes = FindAnyObjectByType<PlayerModeController>();
            if (flashlight == null) flashlight = FindAnyObjectByType<Flashlight>();
            if (silhouette == null) silhouette = FindAnyObjectByType<ThreatSilhouette>();
            if (officeLights == null) officeLights = FindAnyObjectByType<ThreatLightFlicker>();
            if (streetLamps == null) streetLamps = FindAnyObjectByType<StreetLampFlicker>();
            if (monitor == null) monitor = FindAnyObjectByType<MonitorGlow>();
        }

        /// <summary>Starts the ending. onFinished is called when it is over (go to the report).</summary>
        public void Play(Action onFinished)
        {
            if (IsPlaying)
                return;
            IsPlaying = true;
            StartCoroutine(Run(onFinished));
        }

        private IEnumerator Run(Action onFinished)
        {
            // The figure outside is gone: it is coming in.
            if (silhouette != null)
            {
                silhouette.StopAllCoroutines(); // no blink can bring it back
                silhouette.enabled = false;
                if (silhouette.Figure != null) silhouette.Figure.gameObject.SetActive(false);
            }

            yield return new WaitForSeconds(startDelay);
            float riserLength = StartBlackoutRiser();
            if (riserLength > 0.5f) yield return LightsOutTimed(riserLength);
            else yield return LightsOut();
            EndBlackoutAudio(); // riser cut, power-down, then silence

            // The moment the monitor dies: HUD gone, player up, flashlight usable right away.
            HideHud();
            if (modes != null)
            {
                modes.DeskLocked = true;
                if (modes.Mode != PlayerMode.Walk) modes.SetMode(PlayerMode.Walk);
            }
            if (flashlight != null) flashlight.ResetForBlackout();

            // Moon and the rest fade out meanwhile; the hint only helps if the player has not found [F] yet.
            StartCoroutine(FadeRemainingLights());
            for (float t = 0f; t < promptDelay; t += Time.deltaTime)
                yield return null;

            if (flashlight != null && flashlight.Current != Flashlight.State.On)
            {
                ShowPrompt(flashlightPrompt);
                while (flashlight.Current != Flashlight.State.On)
                {
                    PulsePrompt();
                    yield return null;
                }
                HidePrompt();
            }

            // Nothing in front... the figure is now waiting in the back-left corner.
            yield return new WaitForSeconds(figureAppearDelay);
            yield return PlaceFigureUnseen();
            yield return WaitForFaceContact();

            yield return Jumpscare();
            onFinished?.Invoke();
        }

        // The figure rushes at the camera until its face fills the screen, then black.
        private IEnumerator Jumpscare()
        {
            Transform fig = Figure;
            Camera cam = Camera.main;
            if (fig == null || cam == null)
                yield break;

            // Freeze the player: no walking, no looking away.
            var movement = FindAnyObjectByType<PlayerMovement>();
            if (movement != null) movement.SetControlEnabled(false);
            var fpp = FindAnyObjectByType<FirstPersonCameraRig>();
            var tpp = FindAnyObjectByType<ThirdPersonCameraRig>();
            if (fpp != null) fpp.InputEnabled = false;
            if (tpp != null) tpp.InputEnabled = false;

            // Lock the view onto the face.
            Vector3 faceOffset = FacePoint(fig) - fig.position;
            Vector3 lookDir = (FacePoint(fig) - cam.transform.position).normalized;
            float yaw = Mathf.Atan2(lookDir.x, lookDir.z) * Mathf.Rad2Deg;
            float pitch = -Mathf.Asin(Mathf.Clamp(lookDir.y, -1f, 1f)) * Mathf.Rad2Deg;
            if (fpp != null) fpp.SetOrientation(yaw, pitch);
            if (tpp != null) tpp.SetOrientation(yaw, pitch);

            var animator = fig.GetComponentInChildren<Animator>();
            if (animator != null)
            {
                animator.speed = chargeAnimatorSpeed;
                animator.SetBool("Walking", true);
            }
            ScreenGlitch.TriggerAlways(0.6f);
            PlayLayer(GameAudio.Clips?.jumpscareRiser, riserLayerLevel);
            PlayLayer(GameAudio.Clips?.slenderVoice, slenderLevel);

            // Rush: the face heads for a point just in front of the lens.
            while (true)
            {
                Vector3 camPos = cam.transform.position;
                Vector3 target = camPos + cam.transform.forward * faceStopDistance - faceOffset;
                target.y = Mathf.Min(target.y, fig.position.y); // stay on the floor while running
                if (Vector3.Distance(fig.position + faceOffset, camPos + cam.transform.forward * faceStopDistance) < 0.6f)
                    target = camPos + cam.transform.forward * faceStopDistance - faceOffset; // last lunge to the lens
                fig.position = Vector3.MoveTowards(fig.position, target, chargeSpeed * Time.deltaTime);
                FaceCamera(fig);
                Shake(fpp, tpp, yaw, pitch);
                if ((fig.position - target).sqrMagnitude < 0.0004f)
                    break;
                yield return null;
            }

            // Face fills the screen.
            ScreenGlitch.TriggerAlways(1f);
            PlayLayer(GameAudio.Clips?.jumpscareScream, screamLevel);
            PlayLayer(GameAudio.Clips?.jumpscareStatic, staticLevel);
            PlayLayer(GameAudio.Clips?.jumpscareImpact, impactLevel);
            for (float t = 0f; t < holdSeconds; t += Time.deltaTime)
            {
                Shake(fpp, tpp, yaw, pitch);
                yield return null;
            }

            GameAudio.StopAll(); // hard cut to silence with the black
            ShowBlack();
            if (animator != null) animator.speed = 0f;
            yield return new WaitForSeconds(blackSeconds);
        }

        // Rising tension while the lights die. Returns its length: the lights are timed to it, so the
        // riser ends exactly when the monitor (and the HUD) dies. 0 = no riser clip.
        private float StartBlackoutRiser()
        {
            AudioClip clip = GameAudio.Clips?.blackoutRiser;
            if (clip == null)
                return 0f;
            GameAudio.Play2D(clip, AudioChannel.Jumpscare, blackoutRiserLevel);
            return clip.length;
        }

        private void EndBlackoutAudio()
        {
            GameAudio.StopAll();
            if (GameAudio.Clips != null)
                GameAudio.Play2D(GameAudio.Clips.powerDown, AudioChannel.Sfx, powerDownLevel);
        }

        private static void PlayLayer(AudioClip clip, float level) =>
            GameAudio.Play2D(clip, AudioChannel.Jumpscare, level);

        private void Shake(FirstPersonCameraRig fpp, ThirdPersonCameraRig tpp, float yaw, float pitch)
        {
            float y = yaw + Random.Range(-1f, 1f) * shakeDegrees;
            float p = pitch + Random.Range(-1f, 1f) * shakeDegrees;
            if (fpp != null) fpp.SetOrientation(y, p);
            if (tpp != null) tpp.SetOrientation(y, p);
        }

        private void ShowBlack()
        {
            var go = new GameObject("LoseSequenceBlack", typeof(Canvas));
            go.transform.SetParent(transform, false);
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000; // above everything, glitch included
            var img = new GameObject("Black", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            img.transform.SetParent(go.transform, false);
            var rt = (RectTransform)img.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var image = img.GetComponent<UnityEngine.UI.Image>();
            image.color = Color.black;
            image.raycastTarget = false;
        }

        private Transform Figure => silhouette != null ? silhouette.Figure : null;

        // Put the figure in its corner only while that corner is off screen, so it never pops in.
        private IEnumerator PlaceFigureUnseen()
        {
            Transform fig = Figure;
            if (fig == null)
                yield break;
            fig.gameObject.SetActive(false);
            fig.position = figureSpot;

            var planes = new Plane[6];
            var area = new Bounds(figureSpot + Vector3.up * 1.2f, new Vector3(1f, 2.4f, 1f));
            while (true)
            {
                Camera cam = Camera.main;
                if (cam == null) break;
                GeometryUtility.CalculateFrustumPlanes(cam, planes);
                if (!GeometryUtility.TestPlanesAABB(planes, area)) break;
                yield return null;
            }

            FaceCamera(fig);
            fig.gameObject.SetActive(true);
        }

        // Stands still, always turned toward the player, until the player aims at its face
        // (or walks right up to it).
        private IEnumerator WaitForFaceContact()
        {
            Transform fig = Figure;
            if (fig == null)
                yield break;

            while (true)
            {
                Camera cam = Camera.main;
                if (cam != null)
                {
                    FaceCamera(fig);
                    Vector3 face = FacePoint(fig);
                    Vector3 toFace = face - cam.transform.position;
                    bool aimed = Vector3.Angle(cam.transform.forward, toFace) <= faceAngle;
                    Vector3 flat = fig.position - cam.transform.position;
                    flat.y = 0f;
                    if (aimed || flat.magnitude <= triggerDistance)
                        yield break;
                }
                yield return null;
            }
        }

        private void FaceCamera(Transform fig)
        {
            Camera cam = Camera.main;
            if (cam == null) return;
            Vector3 dir = cam.transform.position - fig.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.001f)
                fig.rotation = Quaternion.LookRotation(dir) * Quaternion.Euler(0f, silhouette.YawOffset, 0f);
        }

        // Upper part of the model, from its renderers' bounds.
        private Vector3 FacePoint(Transform fig)
        {
            var renderers = fig.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
                return fig.position + Vector3.up * 2f;
            Bounds b = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);
            return new Vector3(b.center.x, b.max.y - faceBelowTop, b.center.z);
        }

        // Lights die one at a time over the riser's length: far street lamps first, the monitor
        // last, its screen going black exactly when the riser ends.
        private IEnumerator LightsOutTimed(float duration)
        {
            var steps = new List<Func<IEnumerator>>();
            bool ceilingStep = false, deskStep = false;
            if (streetLamps != null)
                for (int i = streetLamps.LampCount - 1; i >= 0; i--)
                {
                    int lamp = i;
                    steps.Add(() => streetLamps.DieOut(lamp));
                }
            if (officeLights != null)
            {
                ceilingStep = officeLights.CeilingOn;
                deskStep = officeLights.DeskOn;
                if (ceilingStep) steps.Add(() => officeLights.DieOutCeiling());
                if (deskStep) steps.Add(() => officeLights.DieOutDesk());
            }

            // The monitor starts sputtering this long before the end and is cut on the last beat.
            float monitorLead = Mathf.Min(1f, duration * 0.3f);
            float spread = Mathf.Max(0f, duration - monitorLead);
            float start = Time.time;

            for (int i = 0; i < steps.Count; i++)
            {
                float at = steps.Count > 1 ? spread * i / steps.Count : 0f;
                while (Time.time - start < at) yield return null;
                StartCoroutine(steps[i]());
            }
            if (officeLights != null)
            {
                // Lights that were already dark still have to be locked off (no sputter for them).
                if (!ceilingStep) StartCoroutine(officeLights.DieOutCeiling());
                if (!deskStep) StartCoroutine(officeLights.DieOutDesk());
            }

            while (Time.time - start < spread) yield return null;
            Coroutine screen = monitor != null ? StartCoroutine(monitor.DieOut()) : null;
            while (Time.time - start < duration) yield return null;
            if (screen != null) StopCoroutine(screen);
            if (monitor != null) monitor.ForceOff();
        }

        // Lights die one at a time: far street lamps first, the monitor last (used without a riser clip).
        private IEnumerator LightsOut()
        {
            if (streetLamps != null)
            {
                for (int i = streetLamps.LampCount - 1; i >= 0; i--)
                {
                    yield return streetLamps.DieOut(i);
                    yield return new WaitForSeconds(betweenLights);
                }
            }

            if (officeLights != null)
            {
                bool ceilingWasOn = officeLights.CeilingOn, deskWasOn = officeLights.DeskOn;
                yield return officeLights.DieOutCeiling();
                if (ceilingWasOn) yield return new WaitForSeconds(betweenLights);
                yield return officeLights.DieOutDesk();
                if (deskWasOn) yield return new WaitForSeconds(betweenLights);
            }

            if (monitor != null)
                yield return monitor.DieOut();
        }

        // Moon and anything else still lit (except the flashlight) fade to black together.
        private IEnumerator FadeRemainingLights()
        {
            var remaining = new List<Light>();
            Light torch = flashlight != null ? flashlight.GetComponent<Light>() : null;
            foreach (Light l in FindObjectsByType<Light>(FindObjectsInactive.Exclude))
                if (l != torch && l.enabled && l.intensity > 0f) remaining.Add(l);

            var start = new float[remaining.Count];
            for (int i = 0; i < remaining.Count; i++) start[i] = remaining[i].intensity;
            for (float t = 0f; t < 1.5f; t += Time.deltaTime)
            {
                float k = 1f - t / 1.5f;
                for (int i = 0; i < remaining.Count; i++)
                    if (remaining[i] != null) remaining[i].intensity = start[i] * k;
                yield return null;
            }
            foreach (Light l in remaining)
                if (l != null) l.enabled = false;
        }

        private void HideHud()
        {
            // Keep a font from the HUD for the prompt before hiding it.
            if (font == null)
            {
                TMP_Text any = FindAnyObjectByType<TMP_Text>();
                if (any != null) font = any.font;
            }

            foreach (Canvas c in FindObjectsByType<Canvas>(FindObjectsInactive.Exclude))
            {
                // Leave the screen-glitch overlay alone; hide HUD and inbox.
                if (c.isRootCanvas && c.gameObject.name != "ScreenGlitchOverlay")
                    c.enabled = false;
            }
        }

        private void ShowPrompt(string text)
        {
            if (promptCanvas == null)
            {
                promptCanvas = new GameObject("LoseSequencePrompt", typeof(Canvas));
                promptCanvas.transform.SetParent(transform, false);
                var canvas = promptCanvas.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 400;

                var go = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
                go.transform.SetParent(promptCanvas.transform, false);
                var rt = (RectTransform)go.transform;
                rt.anchorMin = new Vector2(0f, 0f);
                rt.anchorMax = new Vector2(1f, 0f);
                rt.pivot = new Vector2(0.5f, 0f);
                rt.anchoredPosition = new Vector2(0f, 120f);
                rt.sizeDelta = new Vector2(0f, 80f);

                promptText = go.GetComponent<TextMeshProUGUI>();
                if (font != null) promptText.font = font;
                promptText.fontSize = promptSize;
                promptText.alignment = TextAlignmentOptions.Center;
                promptText.characterSpacing = 4f;
                promptText.raycastTarget = false;
            }

            promptText.text = text;
            promptText.color = promptColor;
            promptCanvas.SetActive(true);
        }

        private void PulsePrompt()
        {
            if (promptText == null) return;
            Color c = promptColor;
            c.a = Mathf.Lerp(0.35f, 1f, (Mathf.Sin(Time.time * 3f) + 1f) * 0.5f);
            promptText.color = c;
        }

        private void HidePrompt()
        {
            if (promptCanvas != null) promptCanvas.SetActive(false);
        }
    }
}
