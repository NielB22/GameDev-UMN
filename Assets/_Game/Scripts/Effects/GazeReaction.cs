using CyberWatch.Environment;
using CyberWatch.Threat;
using Unity.Cinemachine;
using UnityEngine;

namespace CyberWatch.Effects
{
    /// <summary>
    /// "It is looking at me": when the player keeps looking at the figure outside, the view
    /// tightens slightly (a few degrees of field of view), breathing quickens and the screen
    /// edges darken a touch. Look away and it eases back.
    /// Only from 80% Threat, growing step by step up to 100%. Kept subtle on purpose.
    /// Also a slight tunnel vision in the Critical stage (75-100%), even without looking at it.
    /// Other scripts read GazeReaction.Amount (0..1).
    /// </summary>
    public class GazeReaction : MonoBehaviour
    {
        [Tooltip("Found automatically in the scene when empty.")]
        [SerializeField] private ThreatLevelSystem threat;
        [Tooltip("Found automatically in the scene when empty.")]
        [SerializeField] private ThreatSilhouette silhouette;
        [Tooltip("Cameras whose field of view narrows (player FPP/TPP and desk). Empty = all in the scene.")]
        [SerializeField] private CinemachineCamera[] cameras;

        [Header("When does it react")]
        [Tooltip("Threat (%) where the reaction starts (weakest); full strength at 100%.")]
        [SerializeField, Range(0f, 100f)] private float fromThreat = 80f;
        [Tooltip("The figure counts as 'looked at' within this angle from the view center (degrees).")]
        [SerializeField, Range(2f, 40f)] private float lookAngle = 14f;
        [Tooltip("Seconds of looking before the reaction is full.")]
        [SerializeField, Min(0.1f)] private float buildSeconds = 1.2f;
        [Tooltip("Seconds to ease back after looking away.")]
        [SerializeField, Min(0.1f)] private float releaseSeconds = 1.6f;

        [Header("Strength at 100% Threat")]
        [Tooltip("Field of view reduction (degrees). Small = subtle zoom.")]
        [SerializeField, Range(0f, 15f)] private float fovNarrowing = 5f;

        [Header("Tunnel vision (Critical, without looking at the figure)")]
        [Tooltip("Field of view reduction at 100% Threat (degrees), growing smoothly from the Critical stage.")]
        [SerializeField, Range(0f, 10f)] private float criticalNarrowing = 3f;

        private static GazeReaction instance;
        private float stare;      // 0..1 how long we have been looking
        private float amount;     // stare x Threat level
        private float[] baseFov;
        private float tunnel;     // 0..1 Critical tunnel vision, eased

        /// <summary>0..1: how strongly the player is reacting to the figure's gaze right now.</summary>
        public static float Amount => instance != null ? instance.amount : 0f;

        private void Awake()
        {
            instance = this;
            if (threat == null) threat = FindAnyObjectByType<ThreatLevelSystem>();
            if (silhouette == null) silhouette = FindAnyObjectByType<ThreatSilhouette>();
            if (cameras == null || cameras.Length == 0)
                cameras = FindObjectsByType<CinemachineCamera>(FindObjectsInactive.Include);
            baseFov = new float[cameras.Length];
            for (int i = 0; i < cameras.Length; i++)
                baseFov[i] = cameras[i] != null ? cameras[i].Lens.FieldOfView : 60f;
        }

        private void OnDestroy()
        {
            if (instance == this) instance = null;
        }

        private void OnDisable()
        {
            stare = amount = tunnel = 0f;
            ApplyFov();
        }

        private void Update()
        {
            float level = threat != null
                ? Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(fromThreat, ThreatLevelSystem.Max, threat.Value))
                : 0f;

            bool looking = level > 0f && IsLookingAtFigure();
            stare = Mathf.MoveTowards(stare, looking ? 1f : 0f,
                Time.deltaTime / (looking ? buildSeconds : releaseSeconds));

            // Eased so it creeps in rather than snapping.
            amount = Mathf.SmoothStep(0f, 1f, stare) * level;

            float tunnelTarget = threat != null
                ? Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(ThreatLevelSystem.CriticalAbove, ThreatLevelSystem.Max, threat.Value))
                : 0f;
            tunnel = Mathf.MoveTowards(tunnel, tunnelTarget, 0.15f * Time.deltaTime); // very slow
            ApplyFov();
        }

        private bool IsLookingAtFigure()
        {
            if (silhouette == null || !silhouette.FigureVisible)
                return false;
            Camera cam = Camera.main;
            if (cam == null)
                return false;

            // Aim at the upper body of the figure.
            Vector3 target = silhouette.Figure.position + Vector3.up * 1.5f;
            Vector3 toFigure = target - cam.transform.position;
            return Vector3.Angle(cam.transform.forward, toFigure) <= lookAngle;
        }

        private void ApplyFov()
        {
            if (cameras == null)
                return;
            for (int i = 0; i < cameras.Length; i++)
            {
                if (cameras[i] == null) continue;
                var lens = cameras[i].Lens;
                lens.FieldOfView = baseFov[i] - fovNarrowing * amount - criticalNarrowing * tunnel;
                cameras[i].Lens = lens;
            }
        }
    }
}
