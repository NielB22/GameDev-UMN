using CyberWatch.Messages;
using CyberWatch.Player;
using TMPro;
using UnityEngine;

namespace CyberWatch.UI
{
    /// <summary>
    /// HUD hints for walk/desk mode:
    /// - "[E] DUDUK" when the player stands at the IT desk, "[E] / [ESC] BERDIRI" while sitting.
    /// - "PESAN BARU" notice (gently pulsing) while walking and a message is waiting in the inbox.
    /// </summary>
    public class WalkHintsView : MonoBehaviour
    {
        [Tooltip("Found automatically in the scene when empty.")]
        [SerializeField] private PlayerModeController modes;
        [Tooltip("Found automatically in the scene when empty.")]
        [SerializeField] private InboxController inbox;

        [Header("Interaction prompt")]
        [SerializeField] private GameObject promptRoot;
        [SerializeField] private TMP_Text promptText;
        [SerializeField] private string sitText = "[E]  DUDUK DI MEJA";
        [SerializeField] private string standText = "[E] / [ESC]  BERDIRI";

        [Header("New message notice")]
        [SerializeField] private CanvasGroup noticeGroup;
        [SerializeField, Min(0.1f)] private float pulseSpeed = 2.2f;
        [SerializeField, Range(0f, 1f)] private float pulseMinAlpha = 0.45f;

        private void Awake()
        {
            if (modes == null) modes = FindAnyObjectByType<PlayerModeController>();
            if (inbox == null) inbox = FindAnyObjectByType<InboxController>();
        }

        private void Update()
        {
            bool walking = modes == null || modes.Mode == PlayerMode.Walk;

            string prompt = null;
            if (modes != null)
                prompt = walking ? (modes.CanSit ? sitText : null) : standText;
            if (promptRoot != null)
                promptRoot.SetActive(prompt != null);
            if (prompt != null && promptText != null && promptText.text != prompt)
                promptText.text = prompt;

            if (noticeGroup != null)
            {
                bool waiting = walking && inbox != null && inbox.Current != null;
                float pulse = Mathf.Lerp(pulseMinAlpha, 1f, (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f);
                noticeGroup.alpha = waiting ? pulse : 0f;
            }
        }
    }
}
