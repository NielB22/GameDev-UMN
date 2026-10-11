using CyberWatch.Threat;
using UnityEngine;

namespace CyberWatch.Environment
{
    /// <summary>
    /// Wall-mounted security camera. Sweeps slowly while idle; once the Threat Level
    /// reaches <see cref="trackFromStage"/> it turns to follow the target (the player)
    /// and its LED starts blinking. Turning back below that stage returns it to sweeping.
    ///
    /// Prefab layout: root sits on the wall with +Z pointing into the room,
    /// <see cref="pivot"/> is rotated (its +Z = where the lens looks).
    /// </summary>
    public class CctvCamera : MonoBehaviour
    {
        [Header("Parts")]
        [SerializeField] private Transform pivot;
        [SerializeField] private GameObject recordingLed;

        [Header("Idle sweep")]
        [Tooltip("Resting direction, degrees left/right of the wall normal.")]
        [SerializeField, Range(-80f, 80f)] private float restYaw;
        [Tooltip("Degrees looking down while idle.")]
        [SerializeField, Range(-10f, 80f)] private float restPitch = 25f;
        [SerializeField, Range(0f, 80f)] private float sweepAngle = 30f;
        [Tooltip("Seconds for one full left-right-left sweep. 0 disables sweeping.")]
        [SerializeField, Min(0f)] private float sweepPeriod = 14f;

        [Header("Tracking")]
        [SerializeField] private bool canTrack = true;
        [SerializeField] private ThreatStage trackFromStage = ThreatStage.Danger;
        [Tooltip("Leave empty to follow the main camera (the player's view).")]
        [SerializeField] private Transform target;
        [Tooltip("Optional. Found automatically in the scene when empty; without one the camera only sweeps.")]
        [SerializeField] private ThreatLevelSystem threatSource;

        [Header("Motion")]
        [Tooltip("Degrees per second. Slow turning feels more unsettling than snapping.")]
        [SerializeField, Min(1f)] private float turnSpeed = 35f;
        [SerializeField, Range(0f, 90f)] private float maxYaw = 75f;
        [SerializeField] private Vector2 pitchLimits = new Vector2(-5f, 70f);

        [Header("LED")]
        [Tooltip("Blink interval while tracking, in seconds.")]
        [SerializeField, Min(0.05f)] private float trackingBlinkInterval = 0.35f;

        private float yaw;
        private float pitch;
        private float sweepPhase;

        public bool IsTracking { get; private set; }

        public Transform Target
        {
            get => target != null ? target : (Camera.main != null ? Camera.main.transform : null);
            set => target = value;
        }

        private void Awake()
        {
            yaw = restYaw;
            pitch = restPitch;
            // Offset each camera so several CCTVs never sweep in sync.
            sweepPhase = Random.value * Mathf.PI * 2f;
            ApplyRotation();
        }

        private void Start()
        {
            if (threatSource == null)
                threatSource = FindFirstObjectByType<ThreatLevelSystem>();

            if (threatSource != null)
            {
                threatSource.StageChanged += OnStageChanged;
                SetTracking(ShouldTrack(threatSource.Stage));
            }
        }

        private void OnDestroy()
        {
            if (threatSource != null)
                threatSource.StageChanged -= OnStageChanged;
        }

        private void OnStageChanged(ThreatStage previous, ThreatStage current) => SetTracking(ShouldTrack(current));

        private bool ShouldTrack(ThreatStage stage) => canTrack && stage >= trackFromStage;

        /// <summary>Force tracking on/off, e.g. from a scripted event.</summary>
        public void SetTracking(bool tracking)
        {
            IsTracking = tracking;
            // Steady LED while idle; Update() blinks it while tracking.
            if (recordingLed != null)
                recordingLed.SetActive(true);
        }

        private void Update()
        {
            float targetYaw, targetPitch;
            Transform followed = IsTracking ? Target : null;

            if (followed != null)
            {
                Vector3 local = transform.InverseTransformDirection(followed.position - pivot.position);
                targetYaw = Mathf.Atan2(local.x, local.z) * Mathf.Rad2Deg;
                targetPitch = -Mathf.Atan2(local.y, new Vector2(local.x, local.z).magnitude) * Mathf.Rad2Deg;
            }
            else
            {
                float sweep = sweepPeriod > 0f
                    ? Mathf.Sin(Time.time * (2f * Mathf.PI / sweepPeriod) + sweepPhase) * sweepAngle
                    : 0f;
                targetYaw = restYaw + sweep;
                targetPitch = restPitch;
            }

            targetYaw = Mathf.Clamp(targetYaw, -maxYaw, maxYaw);
            targetPitch = Mathf.Clamp(targetPitch, pitchLimits.x, pitchLimits.y);

            float step = turnSpeed * Time.deltaTime;
            yaw = Mathf.MoveTowardsAngle(yaw, targetYaw, step);
            pitch = Mathf.MoveTowardsAngle(pitch, targetPitch, step);
            ApplyRotation();

            if (recordingLed != null && IsTracking)
                recordingLed.SetActive(Mathf.Repeat(Time.time, trackingBlinkInterval * 2f) < trackingBlinkInterval);
        }

        private void ApplyRotation()
        {
            if (pivot != null)
                pivot.localRotation = Quaternion.Euler(pitch, yaw, 0f);
        }
    }
}
