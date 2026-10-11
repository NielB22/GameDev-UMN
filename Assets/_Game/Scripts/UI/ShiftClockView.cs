using CyberWatch.Session;
using TMPro;
using UnityEngine;

namespace CyberWatch.UI
{
    /// <summary>Shows the in-game shift time ("HH:MM") from <see cref="ShiftClock"/>.</summary>
    public class ShiftClockView : MonoBehaviour
    {
        [Tooltip("Found automatically in the scene when empty.")]
        [SerializeField] private ShiftClock clock;
        [SerializeField] private TMP_Text timeText;

        private void Awake()
        {
            if (clock == null)
                clock = FindAnyObjectByType<ShiftClock>();
        }

        private void OnEnable()
        {
            if (clock == null)
                return;
            clock.TimeChanged += Show;
            Show(clock.FormattedTime);
        }

        private void OnDisable()
        {
            if (clock != null)
                clock.TimeChanged -= Show;
        }

        private void Show(string time)
        {
            if (timeText != null)
                timeText.text = time;
        }
    }
}
