using System;
using CyberWatch.Core;
using CyberWatch.Threat;
using UnityEngine;

namespace CyberWatch.Session
{
    /// <summary>
    /// Hourly message target, so leaving the inbox alone is not a free win.
    /// At every hour change (01:00 ... 05:00) the player must have answered at least
    /// hours x <see cref="MessageQuotaRules.PerHour()"/> messages. Every message still missing adds
    /// <see cref="MessageQuotaRules.PenaltyPerMissing()"/> Threat. A shortfall is punished once:
    /// later checks only count what is newly missing.
    /// 06:00 is not checked: reaching it ends the shift.
    /// </summary>
    public class MessageQuota : MonoBehaviour
    {
        [Tooltip("Found automatically in the scene when empty.")]
        [SerializeField] private ShiftClock clock;
        [Tooltip("Found automatically in the scene when empty.")]
        [SerializeField] private ThreatLevelSystem threat;

        private int checkedHours;
        private int punishedMissing;

        /// <summary>
        /// Raised when a target is missed: (hour of day, messages missing now, Threat added).
        /// </summary>
        public event Action<int, int, float> TargetMissed;

        private void Awake()
        {
            if (clock == null) clock = FindAnyObjectByType<ShiftClock>();
            if (threat == null) threat = FindAnyObjectByType<ThreatLevelSystem>();
        }

        private void Update()
        {
            if (clock == null || threat == null || !clock.IsRunning || threat.Value >= ThreatLevelSystem.Max)
                return;

            int hours = clock.HoursElapsed;
            while (checkedHours < hours)
            {
                checkedHours++;
                Check(checkedHours);
            }
        }

        private void Check(int hoursIntoShift)
        {
            int required = hoursIntoShift * MessageQuotaRules.PerHour();
            int answered = GameManager.Instance.CurrentShift.Decisions.Count;
            int missing = Mathf.Max(0, required - answered);
            int newlyMissing = Mathf.Max(0, missing - punishedMissing);
            if (newlyMissing == 0)
                return;

            punishedMissing += newlyMissing;
            float amount = newlyMissing * MessageQuotaRules.PenaltyPerMissing();
            threat.Increase(amount);
            TargetMissed?.Invoke(clock.HourOfDay(hoursIntoShift), newlyMissing, amount);
        }
    }
}
