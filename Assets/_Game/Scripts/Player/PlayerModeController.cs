using System;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.UI;

namespace CyberWatch.Player
{
    public enum PlayerMode
    {
        Walk, // third person, exploring the office
        Desk  // sitting at the IT desk, working in the inbox
    }

    /// <summary>
    /// Switches between walk mode and desk mode inside Gameplay_Desktop_Scene.
    /// Walk: third-person camera, WASD/mouse, cursor locked, inbox hidden.
    /// Desk: Cinemachine blends to the monitor view, cursor free, inbox shown.
    /// The HUD (threat, score, clock) stays visible in both modes; the shift keeps running.
    /// [E] sits down when the player is inside the desk zone; [E] or [Esc] stands up.
    /// </summary>
    public class PlayerModeController : MonoBehaviour
    {
        [Header("Player")]
        [SerializeField] private PlayerMovement player;
        [SerializeField] private ThirdPersonCameraRig cameraRig;

        [Header("Cameras (Cinemachine)")]
        [SerializeField] private CinemachineCamera playerCamera;
        [SerializeField] private CinemachineCamera deskCamera;
        [SerializeField] private int activePriority = 20;
        [SerializeField] private int inactivePriority = 10;

        [Header("Desk")]
        [Tooltip("Canvas of UI_Desktop (inbox). Hidden in walk mode; the HUD canvas is not touched.")]
        [SerializeField] private Canvas desktopCanvas;
        [Tooltip("Where the player must stand to sit down (around the IT chair).")]
        [SerializeField] private Vector3 deskZoneCenter = new Vector3(0f, 0f, 1.5f);
        [SerializeField, Min(0.2f)] private float deskZoneRadius = 1.1f;

        [Header("Start")]
        [SerializeField] private PlayerMode startMode = PlayerMode.Walk;

        [Header("Character visibility")]
        [Tooltip("While sitting down the character is hidden only when the camera has arrived at the desk, " +
                 "or earlier if the blending camera comes this close to the body (so it never clips through it).")]
        [SerializeField, Min(0.1f)] private float hideWhenCameraCloserThan = 0.8f;

        [Header("Input")]
        [SerializeField] private string interactAction = "Player/Interact";

        private InputAction interact;
        private CinemachineBrain brain;
        private bool hidePending;
        private Renderer[] playerRenderers;
        private GraphicRaycaster desktopRaycaster;

        public PlayerMode Mode { get; private set; }

        /// <summary>True when the player stands close enough to the desk to sit down.</summary>
        public bool CanSit => Mode == PlayerMode.Walk && player != null && IsInDeskZone(player.transform.position);

        /// <summary>Raised after the mode changed.</summary>
        public event Action<PlayerMode> ModeChanged;

        private void Awake()
        {
            if (player == null) player = FindAnyObjectByType<PlayerMovement>();
            if (cameraRig == null) cameraRig = FindAnyObjectByType<ThirdPersonCameraRig>();
            if (player != null) playerRenderers = player.GetComponentsInChildren<Renderer>(true);
            if (desktopCanvas != null) desktopRaycaster = desktopCanvas.GetComponent<GraphicRaycaster>();
            interact = InputSystem.actions != null ? InputSystem.actions.FindAction(interactAction) : null;
            brain = Camera.main != null ? Camera.main.GetComponent<CinemachineBrain>() : FindAnyObjectByType<CinemachineBrain>();
        }

        // On the first frame the Cinemachine Brain cuts straight to the active camera (no blend).
        private void Start() => SetMode(startMode);

        private void Update()
        {
            bool interactPressed = PressedThisFrame(interact);
            bool escapePressed = Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;

            if (Mode == PlayerMode.Walk && interactPressed && CanSit)
                SetMode(PlayerMode.Desk);
            else if (Mode == PlayerMode.Desk && (interactPressed || escapePressed))
                SetMode(PlayerMode.Walk);

            if (hidePending && ReadyToHide())
            {
                SetCharacterVisible(false);
                hidePending = false;
            }
        }

        // Hide only once the desk view is reached (or the camera is about to pass through the body).
        private bool ReadyToHide()
        {
            if (brain == null || player == null)
                return true;
            if (!brain.IsBlending && ReferenceEquals(brain.ActiveVirtualCamera, deskCamera))
                return true;
            Vector3 body = player.transform.position + Vector3.up * 0.8f;
            return Vector3.Distance(brain.transform.position, body) < hideWhenCameraCloserThan;
        }

        private void SetCharacterVisible(bool visible)
        {
            if (playerRenderers == null)
                return;
            foreach (Renderer r in playerRenderers) r.enabled = visible;
        }

        public void SetMode(PlayerMode mode)
        {
            bool desk = mode == PlayerMode.Desk;
            Mode = mode;

            if (player != null) player.SetControlEnabled(!desk);
            if (cameraRig != null)
            {
                if (!desk) cameraRig.AlignBehindTarget(); // stand up looking the way the character faces
                cameraRig.InputEnabled = !desk;
            }
            // The character sits "behind" the monitor view: hide it once the camera has arrived
            // (see Update), and show it again immediately when standing up.
            hidePending = desk;
            if (!desk) SetCharacterVisible(true);

            if (playerCamera != null) playerCamera.Priority.Value = desk ? inactivePriority : activePriority;
            if (deskCamera != null) deskCamera.Priority.Value = desk ? activePriority : inactivePriority;

            if (desktopCanvas != null) desktopCanvas.enabled = desk;
            if (desktopRaycaster != null) desktopRaycaster.enabled = desk;

            Cursor.lockState = desk ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = desk;

            ModeChanged?.Invoke(mode);
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
