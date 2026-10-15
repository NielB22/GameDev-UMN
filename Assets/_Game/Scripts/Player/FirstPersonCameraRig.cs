using CyberWatch.Threat;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CyberWatch.Player
{
    /// <summary>
    /// First-person view for walk mode (put it on CM_PlayerFPP).
    /// Sits at the player's eyes; mouse / right stick looks around. The body follows the
    /// camera's yaw (PlayerMovement.FaceCameraYaw) and is drawn as shadow only, so the
    /// camera never shows the inside of the head.
    /// Head bob while walking (bigger when running) and slow breathing when standing still,
    /// breathing faster as the Threat rises.
    /// </summary>
    public class FirstPersonCameraRig : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [Tooltip("Eye height above the player's feet (character is scaled 0.85).")]
        [SerializeField, Min(0.5f)] private float eyeHeight = 1.38f;
        [Tooltip("Pushes the eyes slightly forward so the view starts in front of the face.")]
        [SerializeField] private float forwardOffset = 0.08f;

        [Header("Look")]
        [SerializeField] private Vector2 pitchLimits = new Vector2(-75f, 75f);
        [Tooltip("Degrees per pixel of mouse movement.")]
        [SerializeField, Min(0.01f)] private float mouseSensitivity = 0.12f;
        [Tooltip("Degrees per second at full stick.")]
        [SerializeField, Min(1f)] private float stickSensitivity = 160f;
        [SerializeField] private string lookAction = "Player/Look";

        [Header("Head bob (walking)")]
        [Tooltip("Up/down movement per step (meters).")]
        [SerializeField, Range(0f, 0.1f)] private float bobVertical = 0.03f;
        [Tooltip("Side-to-side sway per stride (meters).")]
        [SerializeField, Range(0f, 0.1f)] private float bobLateral = 0.018f;
        [Tooltip("Slight head roll with the sway (degrees).")]
        [SerializeField, Range(0f, 3f)] private float bobRoll = 0.6f;
        [Tooltip("Distance covered per step (meters). Shorter = faster bobbing.")]
        [SerializeField, Min(0.1f)] private float stepLength = 0.7f;
        [Tooltip("Bob size multiplier while running.")]
        [SerializeField, Min(1f)] private float runMultiplier = 1.6f;
        [Tooltip("How fast the bob fades in/out when starting/stopping.")]
        [SerializeField, Min(0.1f)] private float bobBlend = 6f;

        [Header("Breathing (standing still)")]
        [SerializeField, Range(0f, 0.03f)] private float breathAmount = 0.006f;
        [Tooltip("Breaths per second when calm.")]
        [SerializeField, Min(0.05f)] private float breathRate = 0.22f;
        [Tooltip("Breaths per second at 100% Threat (faster, more nervous).")]
        [SerializeField, Min(0.05f)] private float breathRateCritical = 0.5f;
        [Tooltip("Breath size multiplier at 100% Threat.")]
        [SerializeField, Min(1f)] private float breathAmountCritical = 1.8f;

        private InputAction look;
        private float yaw;
        private float pitch;
        private PlayerMovement movement;
        private ThreatLevelSystem threat;
        private float stepPhase;
        private float breathPhase;
        private float bobWeight;

        /// <summary>When false the rig ignores look input (other view active, or sitting).</summary>
        public bool InputEnabled { get; set; } = true;

        public float Yaw => yaw;
        public float Pitch => pitch;

        /// <summary>Used when switching views so the camera keeps looking the same way.</summary>
        public void SetOrientation(float newYaw, float newPitch)
        {
            yaw = newYaw;
            pitch = Mathf.Clamp(newPitch, pitchLimits.x, pitchLimits.y);
            UpdatePose();
        }

        /// <summary>Looks straight ahead the way the character faces (used when standing up).</summary>
        public void AlignWithTarget()
        {
            if (target != null)
                yaw = target.eulerAngles.y;
            pitch = 0f;
            UpdatePose();
        }

        private void Awake()
        {
            if (target == null)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null) target = player.transform;
            }
            look = InputSystem.actions != null ? InputSystem.actions.FindAction(lookAction) : null;
            if (target != null) movement = target.GetComponent<PlayerMovement>();
            threat = FindAnyObjectByType<ThreatLevelSystem>();
            AlignWithTarget();
        }

        private void OnEnable() => look?.Enable();

        private void LateUpdate()
        {
            if (target == null)
                return;

            if (InputEnabled && look != null)
            {
                Vector2 delta = look.ReadValue<Vector2>();
                bool isMouse = look.activeControl != null && look.activeControl.device is Pointer;
                Vector2 step = isMouse ? delta * mouseSensitivity : delta * (stickSensitivity * Time.deltaTime);
                yaw += step.x;
                pitch = Mathf.Clamp(pitch - step.y, pitchLimits.x, pitchLimits.y);
            }

            AdvanceBob();
            UpdatePose();
        }

        // Step phase follows the distance walked (so faster = quicker steps); breathing always runs.
        private void AdvanceBob()
        {
            float dt = Time.deltaTime;
            float speed = movement != null && movement.ControlEnabled ? movement.HorizontalSpeed : 0f;
            bool walking = speed > 0.1f;

            stepPhase += speed / stepLength * Mathf.PI * dt;
            float targetWeight = !walking ? 0f : movement.IsRunning ? runMultiplier : 1f;
            bobWeight = Mathf.MoveTowards(bobWeight, targetWeight, bobBlend * dt);

            float danger = threat != null ? threat.Value / ThreatLevelSystem.Max : 0f;
            breathPhase += Mathf.Lerp(breathRate, breathRateCritical, danger) * Mathf.PI * 2f * dt;
        }

        private void UpdatePose()
        {
            if (target == null)
                return;
            Quaternion flat = Quaternion.Euler(0f, yaw, 0f);
            Vector3 eyes = target.position + Vector3.up * eyeHeight + flat * Vector3.forward * forwardOffset;

            // Head bob: up/down twice per stride, side sway once per stride; breathing when still.
            float danger = threat != null ? threat.Value / ThreatLevelSystem.Max : 0f;
            float breath = Mathf.Sin(breathPhase) * breathAmount * Mathf.Lerp(1f, breathAmountCritical, danger)
                           * Mathf.Clamp01(1f - bobWeight);
            float up = Mathf.Sin(stepPhase * 2f) * bobVertical * bobWeight + breath;
            float side = Mathf.Sin(stepPhase) * bobLateral * bobWeight;
            float roll = -Mathf.Sin(stepPhase) * bobRoll * bobWeight;

            eyes += flat * new Vector3(side, up, 0f);
            transform.SetPositionAndRotation(eyes, Quaternion.Euler(pitch, yaw, roll));
        }
    }
}
