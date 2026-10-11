using System;
using UnityEngine;

namespace CyberWatch.Session
{
    /// <summary>
    /// In-game night-shift clock (default 00:00 -> 06:00). Real duration is configurable,
    /// so the whole shift can be shortened for testing.
    /// </summary>
    public class ShiftClock : MonoBehaviour
    {
        [SerializeField, Range(0, 23)] private int startHour = 0;
        [SerializeField, Range(1, 24)] private int endHour = 6;
        [Tooltip("How long the whole shift lasts in real seconds (360 = 1 game hour per real minute).")]
        [SerializeField, Min(10f)] private float realDurationSeconds = 360f;
        [SerializeField] private bool startOnPlay = true;

        private float elapsed;
        private int lastReportedMinute = -1;

        public bool IsRunning { get; private set; }
        public bool IsFinished { get; private set; }

        /// <summary>0 at shift start, 1 at shift end.</summary>
        public float Progress => Mathf.Clamp01(elapsed / realDurationSeconds);

        /// <summary>Minutes since midnight of the in-game time (e.g. 90 = 01:30).</summary>
        public int CurrentMinuteOfDay =>
            startHour * 60 + Mathf.FloorToInt(Progress * ShiftLengthMinutes);

        /// <summary>"HH:MM".</summary>
        public string FormattedTime
        {
            get
            {
                int minutes = CurrentMinuteOfDay % (24 * 60);
                return $"{minutes / 60:00}:{minutes % 60:00}";
            }
        }

        private int ShiftLengthMinutes => (endHour - startHour + 24) % 24 * 60;

        /// <summary>Raised whenever the in-game minute changes (for clock UI).</summary>
        public event Action<string> TimeChanged;

        /// <summary>Raised once when the clock reaches the end hour.</summary>
        public event Action ShiftEnded;

        private void Start()
        {
            lastReportedMinute = CurrentMinuteOfDay;
            TimeChanged?.Invoke(FormattedTime);
            if (startOnPlay)
                StartClock();
        }

        public void StartClock()
        {
            if (!IsFinished)
                IsRunning = true;
        }

        public void StopClock() => IsRunning = false;

        private void Update()
        {
            if (!IsRunning)
                return;

            elapsed += Time.deltaTime;

            int minute = CurrentMinuteOfDay;
            if (minute != lastReportedMinute)
            {
                lastReportedMinute = minute;
                TimeChanged?.Invoke(FormattedTime);
            }

            if (elapsed >= realDurationSeconds)
            {
                IsRunning = false;
                IsFinished = true;
                ShiftEnded?.Invoke();
            }
        }
    }
}
