using System.Collections;
using CyberWatch.Core;
using CyberWatch.Messages;
using CyberWatch.Threat;
using UnityEngine;

namespace CyberWatch.Session
{
    /// <summary>
    /// Decides how the desktop phase ends:
    /// win  = the clock reaches 06:00 while threat is below 100%,
    /// lose = threat reaches 100% (System Compromised).
    /// Waits <see cref="endDelaySeconds"/> before leaving the scene so a blackout/fade
    /// can play later without changing this logic.
    /// </summary>
    public class ShiftController : MonoBehaviour
    {
        [SerializeField] private ShiftClock clock;
        [SerializeField] private ThreatLevelSystem threat;
        [Tooltip("Closed when the shift ends so no more decisions are recorded.")]
        [SerializeField] private InboxController inbox;
        [SerializeField, Min(0f)] private float endDelaySeconds = 2f;

        public bool HasEnded { get; private set; }

        private void Awake()
        {
            // Playing this scene directly (or after a previous shift) still gets a fresh session.
            if (GameManager.Instance.CurrentShift.Outcome != ShiftOutcome.InProgress)
                GameManager.Instance.ResetShift();
        }

        private void OnEnable()
        {
            if (clock != null) clock.ShiftEnded += OnShiftEnded;
            if (threat != null) threat.Compromised += OnCompromised;
        }

        private void OnDisable()
        {
            if (clock != null) clock.ShiftEnded -= OnShiftEnded;
            if (threat != null) threat.Compromised -= OnCompromised;
        }

        private void OnShiftEnded() => End(ShiftOutcome.Survived);

        private void OnCompromised() => End(ShiftOutcome.Compromised);

        private void End(ShiftOutcome outcome)
        {
            if (HasEnded)
                return;

            HasEnded = true;
            if (clock != null)
                clock.StopClock();
            if (inbox != null)
                inbox.Close();

            Debug.Log($"[ShiftController] Shift ended: {outcome}");
            StartCoroutine(EndAfterDelay(outcome));
        }

        private IEnumerator EndAfterDelay(ShiftOutcome outcome)
        {
            yield return new WaitForSeconds(endDelaySeconds);
            GameManager.Instance.EndShift(outcome);
        }
    }
}
