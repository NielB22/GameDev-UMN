using System;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace CyberWatch.Player
{
    public enum PlayerMode
    {
        Walk, // exploring the office (first or third person)
        Desk  // sitting at the IT desk, working in the inbox
    }

    public enum CameraView
    {
        FirstPerson,
        ThirdPerson
    }

    /// <summary>
    /// Switches between walk mode and desk mode inside Gameplay_Desktop_Scene.
    /// Walk: first-person or third-person camera ([V] toggles), WASD/mouse, cursor locked, inbox hidden.
    /// Desk: Cinemachine blends to the monitor view, cursor free, inbox shown.
    /// The HUD (threat, score, clock) stays visible in both modes; the shift keeps running.
    /// [E] sits down when the player is inside the desk zone; [E] or [Esc] stands up.
    /// </summary>
    public class PlayerModeController : MonoBehaviour
    {
        [Header("Player")]
        [SerializeField] private PlayerMovement player;
        [SerializeField] private ThirdPersonCameraRig cameraRig;
        [SerializeField] private FirstPersonCameraRig firstPersonRig;

        [Header("Cameras (Cinemachine)")]
        [Tooltip("Third-person camera (CM_PlayerCam).")]
        [SerializeField] private CinemachineCamera playerCamera;
        [Tooltip("First-person camera (CM_PlayerFPP).")]
        [SerializeField] private CinemachineCamera firstPersonCamera;
        [SerializeField] private CinemachineCamera deskCamera;
        [SerializeField] private int activePriority = 20;
        [SerializeField] private int inactivePriority = 10;

        [Header("Camera view (walk mode only)")]
        [SerializeField] private CameraView startView = CameraView.FirstPerson;
        [SerializeField] private Key toggleViewKey = Key.V;
        [Tooltip("Blend time when switching first <-> third person (desk blends keep the Brain's default).")]
        [SerializeField, Min(0f)] private float viewBlendSeconds = 0.35f;

        [Header("Desk")]
        [Tooltip("Canvas of UI_Desktop (inbox). Hidden in walk mode; the HUD canvas is not touched.")]
        [SerializeField] private Canvas desktopCanvas;
        [Tooltip("Where the player must stand to sit down (around the IT chair).")]
        [SerializeField] private Vector3 deskZoneCenter = new Vector3(0f, 0f, 1.5f);
        [SerializeField, Min(0.2f)] private float deskZoneRadius = 1.1f;

        [Header("Start")]
        [SerializeField] private PlayerMode startMode = PlayerMode.Walk;

        [Header("Character visibility")]
        [Tooltip("The body is hidden (desk) or drawn as shadow only (first person) once the camera has arrived, " +
                 "or earlier if the blending camera comes this close to the body (so it never clips through it).")]
        [SerializeField, Min(0.1f)] private float hideWhenCameraCloserThan = 0.8f;

        [Header("Input")]
        [SerializeField] private string interactAction = "Player/Interact";

        private enum BodyState { Visible, ShadowOnly, Hidden }

        private InputAction interact;
        private CinemachineBrain brain;
        private CinemachineBlendDefinition deskBlend;
        private Renderer[] playerRenderers;
        private ShadowCastingMode[] rendererShadows;
        private BodyState bodyState = BodyState.Visible;
        private GraphicRaycaster desktopRaycaster;

        public PlayerMode Mode { get; private set; }
        public CameraView View { get; private set; }

        /// <summary>True when the player stands close enough to the desk to sit down.</summary>
        public bool CanSit => Mode == PlayerMode.Walk && player != null && IsInDeskZone(player.transform.position);

        /// <summary>Raised after the mode changed.</summary>
        public event Action<PlayerMode> ModeChanged;

        /// <summary>Raised after the walk-mode camera view changed.</summary>
        public event Action<CameraView> ViewChanged;

        private void Awake()
        {
            if (player == null) player = FindAnyObjectByType<PlayerMovement>();
            if (cameraRig == null) cameraRig = FindAnyObjectByType<ThirdPersonCameraRig>();
            if (firstPersonRig == null) firstPersonRig = FindAnyObjectByType<FirstPersonCameraRig>();
            if (player != null)
            {
                playerRenderers = player.GetComponentsInChildren<Renderer>(true);
                rendererShadows = new ShadowCastingMode[playerRenderers.Length];
                for (int i = 0; i < playerRenderers.Length; i++)
                    rendererShadows[i] = playerRenderers[i].shadowCastingMode;
            }
            if (desktopCanvas != null) desktopRaycaster = desktopCanvas.GetComponent<GraphicRaycaster>();
            interact = InputSystem.actions != null ? InputSystem.actions.FindAction(interactAction) : null;
            brain = Camera.main != null ? Camera.main.GetComponent<CinemachineBrain>() : FindAnyObjectByType<CinemachineBrain>();
            if (brain != null) deskBlend = brain.DefaultBlend;
            if (firstPersonCamera == null) startView = CameraView.ThirdPerson;
            View = startView;
        }

        // On the first frame the Cinemachine Brain cuts straight to the active camera (no blend).
        private void Start() => SetMode(startMode);

        private void Update()
        {
            bool interactPressed = PressedThisFrame(interact);
            bool escapePressed = Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
            bool togglePressed = Keyboard.current != null && Keyboard.current[toggleViewKey].wasPressedThisFrame;

            if (Mode == PlayerMode.Walk && interactPressed && CanSit)
                SetMode(PlayerMode.Desk);
            else if (Mode == PlayerMode.Desk && (interactPressed || escapePressed))
                SetMode(PlayerMode.Walk);
            else if (Mode == PlayerMode.Walk && togglePressed && firstPersonCamera != null)
                SetView(View == CameraView.FirstPerson ? CameraView.ThirdPerson : CameraView.FirstPerson);

            UpdateBody();
        }

        public void SetMode(PlayerMode mode)
        {
            bool desk = mode == PlayerMode.Desk;
            Mode = mode;

            if (player != null) player.SetControlEnabled(!desk);
            if (!desk)
            {
                // Stand up looking the way the character faces.
                if (cameraRig != null) cameraRig.AlignBehindTarget();
                if (firstPersonRig != null) firstPersonRig.AlignWithTarget();
            }

            if (brain != null) brain.DefaultBlend = deskBlend;
            ApplyCameras();

            if (desktopCanvas != null) desktopCanvas.enabled = desk;
            if (desktopRaycaster != null) desktopRaycaster.enabled = desk;

            Cursor.lockState = desk ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = desk;

            ModeChanged?.Invoke(mode);
        }

        /// <summary>First or third person while walking. Keeps looking the same way.</summary>
        public void SetView(CameraView view)
        {
            if (view == View)
                return;

            if (cameraRig != null && firstPersonRig != null)
            {
                if (view == CameraView.FirstPerson)
                    firstPersonRig.SetOrientation(cameraRig.Yaw, cameraRig.Pitch);
                else
                    cameraRig.SetOrientation(firstPersonRig.Yaw, Mathf.Max(firstPersonRig.Pitch, 0f));
            }

            View = view;
            if (brain != null)
                brain.DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.EaseInOut, viewBlendSeconds);
            ApplyCameras();
            ViewChanged?.Invoke(view);
        }

        // Priorities + which rig reads the mouse, from the current mode and view.
        private void ApplyCameras()
        {
            bool desk = Mode == PlayerMode.Desk;
            bool fpp = View == CameraView.FirstPerson && firstPersonCamera != null;

            SetPriority(deskCamera, desk);
            SetPriority(playerCamera, !desk && !fpp);
            SetPriority(firstPersonCamera, !desk && fpp);

            if (cameraRig != null) cameraRig.InputEnabled = !desk && !fpp;
            if (firstPersonRig != null) firstPersonRig.InputEnabled = !desk && fpp;
            if (player != null) player.FaceCameraYaw = fpp;
        }

        private void SetPriority(CinemachineCamera cam, bool active)
        {
            if (cam != null)
                cam.Priority.Value = active ? activePriority : inactivePriority;
        }

        private CinemachineCamera TargetCamera =>
            Mode == PlayerMode.Desk ? deskCamera
            : View == CameraView.FirstPerson && firstPersonCamera != null ? firstPersonCamera
            : playerCamera;

        // Hide the body only once the camera has arrived (or is about to pass through it);
        // show it again as soon as the camera has moved away from it.
        private void UpdateBody()
        {
            BodyState wanted = Mode == PlayerMode.Desk ? BodyState.Hidden
                : View == CameraView.FirstPerson && firstPersonCamera != null ? BodyState.ShadowOnly
                : BodyState.Visible;
            if (wanted == bodyState)
                return;

            bool settled = brain == null || (!brain.IsBlending && ReferenceEquals(brain.ActiveVirtualCamera, TargetCamera));
            bool cameraNear = brain != null && player != null &&
                Vector3.Distance(brain.transform.position, player.transform.position + Vector3.up * 0.8f) < hideWhenCameraCloserThan;

            bool apply = wanted == BodyState.Visible ? settled || !cameraNear : settled || cameraNear;
            if (apply)
                SetBody(wanted);
        }

        private void SetBody(BodyState state)
        {
            bodyState = state;
            if (playerRenderers == null)
                return;
            for (int i = 0; i < playerRenderers.Length; i++)
            {
                Renderer r = playerRenderers[i];
                r.enabled = state != BodyState.Hidden;
                r.shadowCastingMode = state == BodyState.ShadowOnly ? ShadowCastingMode.ShadowsOnly : rendererShadows[i];
            }
        }

        private bool IsInDeskZone(Vector3 position)
        {
            Vector3 d = position - deskZoneCenter;
            d.y = 0f;
            return d.sqrMagnitude <= deskZoneRadius * deskZoneRadius;
        }

        // Reads the bound buttons directly so a "Hold" interaction on the action does not delay the press.
        private static bool PressedThisFrame(InputAction action)
        {
            if (action == null)
                return Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;
            foreach (InputControl control in action.controls)
                if (control is ButtonControl button && button.wasPressedThisFrame)
                    return true;
            return false;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.2f, 0.9f, 0.4f, 0.6f);
            Gizmos.DrawWireSphere(deskZoneCenter + Vector3.up * 0.05f, deskZoneRadius);
        }
    }
}
