using UnityEngine;
using UnityEngine.InputSystem;

namespace CyberWatch.Player
{
    /// <summary>
    /// Third-person orbit for the walk-mode Cinemachine camera (put it on CM_PlayerCam).
    /// Mouse / right stick orbits around the player; the camera is pulled in when a wall or
    /// furniture is between it and the player, so it never looks through the small office walls.
    /// The Cinemachine Brain on Main Camera blends between this camera and the desk camera.
    /// </summary>
    public class ThirdPersonCameraRig : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [Tooltip("Point the camera orbits around, relative to the player (about shoulder height).")]
        [SerializeField] private Vector3 pivotOffset = new Vector3(0f, 1.3f, 0f);

        [Header("Orbit")]
        [SerializeField, Min(0.5f)] private float distance = 2.2f;
        [SerializeField] private Vector2 pitchLimits = new Vector2(-20f, 60f);
        [SerializeField] private float startPitch = 12f;
        [Tooltip("Degrees per pixel of mouse movement.")]
        [SerializeField, Min(0.01f)] private float mouseSensitivity = 0.12f;
        [Tooltip("Degrees per second at full stick.")]
        [SerializeField, Min(1f)] private float stickSensitivity = 160f;
        [SerializeField] private string lookAction = "Player/Look";

        [Header("Collision")]
        [SerializeField, Min(0.05f)] private float collisionRadius = 0.2f;
        [Tooltip("Closest the camera may get to the pivot.")]
        [SerializeField, Min(0.1f)] private float minDistance = 0.4f;
        [Tooltip("How fast the camera moves back out after an obstacle is gone (m/s).")]
        [SerializeField, Min(0.1f)] private float returnSpeed = 3f;

        private InputAction look;
        private float yaw;
        private float pitch;
        private float currentDistance;

        /// <summary>When false the rig ignores look input (e.g. while sitting at the desk).</summary>
        public bool InputEnabled { get; set; } = true;

        public float Yaw => yaw;
        public float Pitch => pitch;

        /// <summary>Used when switching views so the camera keeps looking the same way.</summary>
        public void SetOrientation(float newYaw, float newPitch)
        {
            yaw = newYaw;
            pitch = Mathf.Clamp(newPitch, pitchLimits.x, pitchLimits.y);
        }

        /// <summary>Turns the orbit to look the same way as the player (used when standing up).</summary>
        public void AlignBehindTarget()
        {
            if (target != null)
                yaw = target.eulerAngles.y;
            pitch = startPitch;
        }

        private void Awake()
        {
            if (target == null)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null) target = player.transform;
            }
            look = InputSystem.actions != null ? InputSystem.actions.FindAction(lookAction) : null;
            currentDistance = distance;
            AlignBehindTarget();
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

            Vector3 pivot = target.position + pivotOffset;
            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 back = rotation * Vector3.back;

            // Pull in when something blocks the view (the player itself is on "Ignore Raycast").
            float wanted = distance;
            if (Physics.SphereCast(pivot, collisionRadius, back, out RaycastHit hit, distance,
                    Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                wanted = Mathf.Max(minDistance, hit.distance - 0.05f);

            currentDistance = wanted < currentDistance
                ? wanted                                                      // snap in, never clip
                : Mathf.MoveTowards(currentDistance, wanted, returnSpeed * Time.deltaTime);

            transform.SetPositionAndRotation(pivot + back * currentDistance, rotation);
        }
    }
}
