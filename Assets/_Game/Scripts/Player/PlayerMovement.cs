using UnityEngine;
using UnityEngine.InputSystem;

namespace CyberWatch.Player
{
    /// <summary>
    /// Third-person walking (walk mode): WASD / left stick moves relative to the camera,
    /// Shift runs, the character turns toward where it walks. Drives the LowPolyPeople
    /// animator, whose idle / walk / run states are switched with triggers.
    /// Input comes from the project-wide actions (InputSystem_Actions, map "Player").
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerMovement : MonoBehaviour
    {
        [Header("Speed (m/s)")]
        [SerializeField, Min(0f)] private float walkSpeed = 1.6f;
        [SerializeField, Min(0f)] private float runSpeed = 3.2f;
        [Tooltip("Degrees per second the character turns toward the move direction.")]
        [SerializeField, Min(1f)] private float turnSpeed = 600f;
        [SerializeField] private float gravity = -15f;

        [Header("References (found automatically when empty)")]
        [SerializeField] private Animator animator;
        [Tooltip("Movement is relative to this camera's facing. Empty = Main Camera.")]
        [SerializeField] private Transform cameraTransform;

        [Header("Input action names (project-wide actions)")]
        [SerializeField] private string moveAction = "Player/Move";
        [SerializeField] private string sprintAction = "Player/Sprint";

        private enum Gait { Idle, Walk, Run }

        private CharacterController controller;
        private InputAction move;
        private InputAction sprint;
        private float verticalSpeed;
        private Gait gait = Gait.Idle;
        private bool controlEnabled = true;

        /// <summary>False while sitting at the desk: the character stops and idles.</summary>
        public bool ControlEnabled => controlEnabled;

        /// <summary>First-person view: the body always faces where the camera looks.</summary>
        public bool FaceCameraYaw { get; set; }

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            if (animator == null)
                animator = GetComponentInChildren<Animator>();

            InputActionAsset actions = InputSystem.actions;
            if (actions != null)
            {
                move = actions.FindAction(moveAction);
                sprint = actions.FindAction(sprintAction);
            }
            if (move == null)
                Debug.LogWarning($"[PlayerMovement] Input action '{moveAction}' not found.", this);
        }

        private void OnEnable()
        {
            move?.Enable();
            sprint?.Enable();
            SetGait(Gait.Idle, force: true);
        }

        /// <summary>Called by the mode controller (sit down / stand up).</summary>
        public void SetControlEnabled(bool enabled)
        {
            controlEnabled = enabled;
            if (!enabled)
                SetGait(Gait.Idle);
        }

        private void Update()
        {
            Vector2 input = controlEnabled && move != null ? move.ReadValue<Vector2>() : Vector2.zero;
            if (input.sqrMagnitude > 1f)
                input.Normalize();

            Vector3 direction = CameraRelative(input);
            bool moving = direction.sqrMagnitude > 0.01f;
            bool running = moving && sprint != null && sprint.IsPressed();

            Vector3 velocity = direction * (running ? runSpeed : walkSpeed);

            // Gravity keeps the character grounded on the floor.
            if (controller.isGrounded && verticalSpeed < 0f)
                verticalSpeed = -2f;
            verticalSpeed += gravity * Time.deltaTime;
            velocity.y = verticalSpeed;

            controller.Move(velocity * Time.deltaTime);

            Transform cam = CameraTransform;
            if (FaceCameraYaw && controlEnabled && cam != null)
            {
                Vector3 forward = cam.forward;
                forward.y = 0f;
                if (forward.sqrMagnitude > 0.001f)
                    transform.rotation = Quaternion.LookRotation(forward);
            }
            else if (moving)
            {
                Quaternion look = Quaternion.LookRotation(new Vector3(direction.x, 0f, direction.z));
                transform.rotation = Quaternion.RotateTowards(transform.rotation, look, turnSpeed * Time.deltaTime);
            }

            SetGait(!moving ? Gait.Idle : running ? Gait.Run : Gait.Walk);
        }

        private Transform CameraTransform =>
            cameraTransform != null ? cameraTransform : (Camera.main != null ? Camera.main.transform : null);

        private Vector3 CameraRelative(Vector2 input)
        {
            Transform cam = CameraTransform;
            Vector3 forward = cam != null ? cam.forward : Vector3.forward;
            Vector3 right = cam != null ? cam.right : Vector3.right;
            forward.y = 0f;
            right.y = 0f;
            forward.Normalize();
            right.Normalize();
            return forward * input.y + right * input.x;
        }

        // The LowPolyPeople controller switches states with triggers: fire one only when the gait changes.
        private void SetGait(Gait next, bool force = false)
        {
            if (animator == null || (next == gait && !force))
                return;
            gait = next;
            animator.ResetTrigger("idle");
            animator.ResetTrigger("walk");
            animator.ResetTrigger("run");
            animator.SetTrigger(next == Gait.Idle ? "idle" : next == Gait.Walk ? "walk" : "run");
        }
    }
}
