using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CyberWatch.Player
{
    /// <summary>
    /// Player flashlight (walk mode only). [F] switches it on/off.
    /// - Warm white spot light. First person: held near the eyes; third person: from the chest.
    ///   Both point where the camera looks.
    /// - Battery drains while on. Low battery: dimmer, with small flickers.
    ///   Empty: flickers, then dies.
    /// - Dead: shake the mouse / touchpad quickly (left-right or up-down) to bring it back;
    ///   it flickers back on with a full battery.
    /// - Sitting down or standing up always switches it off (default: no flashlight); press [F] again.
    /// </summary>
    [RequireComponent(typeof(Light))]
    public class Flashlight : MonoBehaviour
    {
        public enum State { Off, On, Dying, Dead, Reviving }

        [Header("References (found automatically when empty)")]
        [SerializeField] private PlayerModeController modes;
        [SerializeField] private Transform player;

        [Header("Light")]
        [SerializeField] private Color color = new Color(1f, 0.92f, 0.76f);
        [SerializeField, Min(0f)] private float intensity = 8f;
        [SerializeField] private bool startOn;
        [SerializeField] private Key toggleKey = Key.F;
        [Tooltip("First person: offset from the camera (right, up, forward).")]
        [SerializeField] private Vector3 firstPersonOffset = new Vector3(0.18f, -0.2f, 0.15f);
        [Tooltip("Third person: offset from the player's feet (right, up, forward).")]
        [SerializeField] private Vector3 thirdPersonOffset = new Vector3(0.15f, 1.15f, 0.3f);
        [Tooltip("How quickly the beam follows the view. Lower = more hand sway.")]
        [SerializeField, Min(1f)] private float followSharpness = 14f;

        [Header("Battery")]
        [Tooltip("Seconds of light from a full battery.")]
        [SerializeField, Min(1f)] private float batterySeconds = 90f;
        [Tooltip("Below this fraction the light is dimmer and flickers now and then.")]
        [SerializeField, Range(0f, 1f)] private float lowBattery = 0.2f;
        [SerializeField, Range(0f, 1f)] private float lowBatteryIntensity = 0.55f;
        [SerializeField] private Vector2 lowFlickerInterval = new Vector2(1.5f, 4f);

        [Header("Die / revive transition")]
        [SerializeField, Min(0.1f)] private float dyingSeconds = 1.6f;
        [SerializeField, Min(0.1f)] private float revivingSeconds = 1.1f;

        [Header("Shake to revive")]
        [SerializeField] private string lookAction = "Player/Look";
        [Tooltip("Mouse movement (pixels per frame) that counts as a shake stroke.")]
        [SerializeField, Min(0.5f)] private float shakeMinDelta = 10f;
        [Tooltip("Direction changes needed within the window.")]
        [SerializeField, Min(2)] private int shakeReversals = 6;
        [SerializeField, Min(0.2f)] private float shakeWindowSeconds = 1.2f;

        private Light beam;
        private InputAction look;
        private Camera mainCamera;
        private float battery = 1f;
        private float multiplier;
        private bool suspended; // sitting at the desk
        private float nextLowFlickerAt;
        private Coroutine transition;
        private readonly float[] reversalTimes = new float[32];
        private int reversalCount;
        private int lastSignX;
        private int lastSignY;

        public State Current { get; private set; } = State.Off;

        /// <summary>0..1</summary>
        public float Battery => battery;

        /// <summary>0..1 while dead: how close the shaking is to bringing the light back.</summary>
        public float ShakeProgress => Current == State.Dead ? Mathf.Clamp01(ReversalsInWindow() / (float)shakeReversals) : 0f;

        public event Action<State> StateChanged;

        private void Awake()
        {
            beam = GetComponent<Light>();
            beam.type = LightType.Spot;
            beam.color = color;
            if (modes == null) modes = FindAnyObjectByType<PlayerModeController>();
            if (player == null)
            {
                GameObject p = GameObject.FindGameObjectWithTag("Player");
                if (p != null) player = p.transform;
            }
            look = InputSystem.actions != null ? InputSystem.actions.FindAction(lookAction) : null;
            mainCamera = Camera.main;
            SetState(startOn ? State.On : State.Off);
            Apply(startOn ? 1f : 0f);
        }

        private void OnEnable()
        {
            look?.Enable();
            if (modes != null) modes.ModeChanged += OnModeChanged;
        }

        private void OnDisable()
        {
            if (modes != null) modes.ModeChanged -= OnModeChanged;
        }

        // Sitting down or standing up always switches the flashlight off: the player turns it on again
        // with [F] when needed. A dead battery stays dead (still needs shaking).
        private void OnModeChanged(PlayerMode mode)
        {
            suspended = mode == PlayerMode.Desk;

            if (Current == State.Reviving && transition != null)
            {
                StopCoroutine(transition);
                transition = null;
                SetState(State.Off); // battery was already refilled
            }
            if (Current == State.On)
                SetState(State.Off);

            Apply(Current == State.Off ? 0f : multiplier);
        }

        private void Update()
        {
            if (!suspended && Keyboard.current != null && Keyboard.current[toggleKey].wasPressedThisFrame)
                Toggle();

            switch (Current)
            {
                case State.On:
                    if (!suspended)
                        battery = Mathf.Max(0f, battery - Time.deltaTime / batterySeconds);
                    if (battery <= 0f)
                        StartTransition(Dying());
                    else
                        Apply(SteadyLevel());
                    break;

                case State.Dead:
                    if (!suspended && TrackShake())
                        StartTransition(Reviving());
                    break;
            }
        }

        private void LateUpdate()
        {
            if (mainCamera == null) mainCamera = Camera.main;
            if (mainCamera == null || player == null)
                return;

            Transform cam = mainCamera.transform;
            bool firstPerson = modes == null || modes.View == CameraView.FirstPerson;
            Vector3 position = firstPerson
                ? cam.position + cam.rotation * firstPersonOffset
                : player.position + player.rotation * thirdPersonOffset;
            Quaternion rotation = cam.rotation;

            float t = 1f - Mathf.Exp(-followSharpness * Time.deltaTime);
            transform.position = position;
            transform.rotation = Quaternion.Slerp(transform.rotation, rotation, t);
        }

        /// <summary>Lose sequence: switched off with a full battery, so [F] surely works.</summary>
        public void ResetForBlackout()
        {
            if (transition != null)
            {
                StopCoroutine(transition);
                transition = null;
            }
            battery = 1f;
            SetState(State.Off);
            Apply(0f);
        }

        private void Toggle()
        {
            if (Current == State.On)
            {
                SetState(State.Off);
                Apply(0f);
            }
            else if (Current == State.Off && battery > 0f)
            {
                SetState(State.On);
                Apply(SteadyLevel());
            }
        }

        // Full brightness, or dim with the odd quick flicker when the battery is low.
        private float SteadyLevel()
        {
            if (battery > lowBattery)
                return 1f;
            if (Time.time >= nextLowFlickerAt)
            {
                nextLowFlickerAt = Time.time + UnityEngine.Random.Range(lowFlickerInterval.x, lowFlickerInterval.y);
                return 0.1f;
            }
            return lowBatteryIntensity;
        }

        private void StartTransition(IEnumerator routine)
        {
            if (transition != null) StopCoroutine(transition);
            transition = StartCoroutine(routine);
        }

        // Flickers with fewer and fewer "on" moments, then goes dark.
        private IEnumerator Dying()
        {
            SetState(State.Dying);
            yield return Flicker(dyingSeconds, from: lowBatteryIntensity, rising: false);
            Apply(0f);
            ResetShake();
            SetState(State.Dead);
            transition = null;
        }

        // Same flicker in reverse: sputters, then holds steady with a full battery.
        private IEnumerator Reviving()
        {
            SetState(State.Reviving);
            battery = 1f;
            yield return Flicker(revivingSeconds, from: 1f, rising: true);
            SetState(State.On);
            Apply(1f);
            transition = null;
        }

        private IEnumerator Flicker(float seconds, float from, bool rising)
        {
            float start = Time.time;
            while (Time.time - start < seconds)
            {
                float k = (Time.time - start) / seconds;  // 0..1
                float onChance = rising ? Mathf.Lerp(0.25f, 0.9f, k) : Mathf.Lerp(0.75f, 0.1f, k);
                bool on = UnityEngine.Random.value < onChance;
                Apply(on ? from * UnityEngine.Random.Range(0.5f, 1f) : 0f);
                yield return new WaitForSeconds(UnityEngine.Random.Range(0.03f, 0.12f));
            }
        }

        // Counts direction changes of the mouse on either axis; enough of them in the window = revive.
        private bool TrackShake()
        {
            if (look == null)
                return false;

            Vector2 delta = look.ReadValue<Vector2>();
            RegisterStroke(delta.x, ref lastSignX);
            RegisterStroke(delta.y, ref lastSignY);
            return ReversalsInWindow() >= shakeReversals;
        }

        private void RegisterStroke(float d, ref int last)
        {
            if (Mathf.Abs(d) < shakeMinDelta)
                return;
            int sign = d > 0f ? 1 : -1;
            if (last != 0 && sign != last)
            {
                reversalTimes[reversalCount % reversalTimes.Length] = Time.time;
                reversalCount++;
            }
            last = sign;
        }

        private int ReversalsInWindow()
        {
            int n = 0;
            int stored = Mathf.Min(reversalCount, reversalTimes.Length);
            for (int i = 0; i < stored; i++)
                if (Time.time - reversalTimes[i] <= shakeWindowSeconds)
                    n++;
            return n;
        }

        private void ResetShake()
        {
            reversalCount = 0;
            lastSignX = 0;
            lastSignY = 0;
        }

        private void SetState(State next)
        {
            if (next == Current)
                return;
            Current = next;
            StateChanged?.Invoke(next);
        }

        private void Apply(float level)
        {
            multiplier = level;
            float shown = suspended ? 0f : level;
            beam.intensity = intensity * shown;
            beam.enabled = shown > 0.001f;
        }
    }
}
