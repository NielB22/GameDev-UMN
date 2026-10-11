using CyberWatch.Core;
using CyberWatch.Messages;
using CyberWatch.Session;
using TMPro;
using UnityEngine;

namespace CyberWatch.UI
{
    /// <summary>
    /// Score: correct decisions out of decided messages ("BENAR 3 / 4").
    /// </summary>
    public class ScoreView : MonoBehaviour
    {
        [Tooltip("Found automatically in the scene when empty.")]
        [SerializeField] private InboxController inbox;
        [SerializeField] private TMP_Text scoreText;

        private void Awake()
        {
            if (inbox == null)
                inbox = FindAnyObjectByType<InboxController>();
        }

        private void OnEnable()
        {
            if (inbox != null)
                inbox.MessageResolved += OnResolved;
            Refresh();
        }

        private void OnDisable()
        {
            if (inbox != null)
                inbox.MessageResolved -= OnResolved;
        }

        private void OnResolved(DecisionRecord _) => Refresh();

        private void Refresh()
        {
            ShiftSession shift = GameManager.Instance.CurrentShift;
            if (scoreText != null)
                scoreText.text = $"{shift.CorrectCount} / {shift.Decisions.Count}";
        }
    }
}
