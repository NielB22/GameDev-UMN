using UnityEngine;
using UnityEngine.InputSystem;

namespace CyberWatch.Player
{
    /// <summary>
    /// First-person view for walk mode (put it on CM_PlayerFPP).
    /// Sits at the player's eyes; mouse / right stick looks around. The body follows the
    /// camera's yaw (PlayerMovement.FaceCameraYaw) and is drawn as shadow only, so the
    /// camera never shows the inside of the head.
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

        private InputAction look;
        private float yaw;
        private float pitch;

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

            UpdatePose();
        }

        private void UpdatePose()
        {
            if (target == null)
                return;
            Quaternion flat = Quaternion.Euler(0f, yaw, 0f);
            Vector3 eyes = target.position + Vector3.up * eyeHeight + flat * Vector3.forward * forwardOffset;
            transform.SetPositionAndRotation(eyes, Quaternion.Euler(pitch, yaw, 0f));
        }
    }
}
