using System;
using System.Collections;
using System.Collections.Generic;
using CyberWatch.Environment;
using CyberWatch.Player;
using TMPro;
using UnityEngine;

namespace CyberWatch.Session
{
    /// <summary>
    /// Ending when the system is Compromised (Threat 100%), played instead of cutting
    /// straight to the report:
    /// 1. every light dies one by one (street lamps, office, desk lamp, monitor), then the moon fades;
    /// 2. when the monitor dies the HUD disappears, the player is forced up (cannot sit again) and
    ///    can already use the flashlight;
    /// 3. 2 s later a hint asks to switch on the flashlight [F] (only if it is still off);
    /// 4. (next steps) the figure waits inside the office -> jumpscare -> report.
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

        [Header("Temporary end (until the jumpscare step exists)")]
        [Tooltip("Seconds after the flashlight is on before the report is shown.")]
        [SerializeField, Min(0f)] private float endAfterFlashlight = 4f;

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
            yield return LightsOut();

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

            // TODO step 2-3: figure inside the office + jumpscare.
            yield return new WaitForSeconds(endAfterFlashlight);
            onFinished?.Invoke();
        }

        // Lights die one at a time: far street lamps first, the monitor last.
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
