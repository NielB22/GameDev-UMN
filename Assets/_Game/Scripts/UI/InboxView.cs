using System.Collections;
using System.Text;
using CyberWatch.Messages;
using CyberWatch.Session;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CyberWatch.UI
{
    /// <summary>
    /// Inbox window: shows the current message (sender, subject, body, links) and the
    /// TERIMA / BLOKIR buttons, plus short feedback after each decision.
    /// </summary>
    public class InboxView : MonoBehaviour
    {
        [Tooltip("Found automatically in the scene when empty.")]
        [SerializeField] private InboxController inbox;

        [Header("Message")]
        [SerializeField] private GameObject messageRoot;
        [SerializeField] private TMP_Text channelText;
        [SerializeField] private TMP_Text senderText;
        [SerializeField] private TMP_Text subjectText;
        [SerializeField] private TMP_Text bodyText;

        [Header("Empty state / feedback")]
        [SerializeField] private TMP_Text statusText;
        [SerializeField, Min(0.2f)] private float feedbackSeconds = 1.6f;

        [Header("Buttons")]
        [SerializeField] private Button acceptButton;
        [SerializeField] private Button blockButton;

        [Header("Colors")]
        [SerializeField] private Color linkColor = new Color(0.45f, 0.75f, 1f);
        [SerializeField] private Color correctColor = new Color(0.35f, 0.85f, 0.45f);
        [SerializeField] private Color wrongColor = new Color(0.95f, 0.3f, 0.25f);
        [SerializeField] private Color idleColor = new Color(0.6f, 0.6f, 0.55f);

        private Coroutine feedbackRoutine;

        private void Awake()
        {
            if (inbox == null)
                inbox = FindAnyObjectByType<InboxController>();
        }

        private void OnEnable()
        {
            if (acceptButton != null) acceptButton.onClick.AddListener(OnAccept);
            if (blockButton != null) blockButton.onClick.AddListener(OnBlock);
            if (inbox == null)
                return;
            inbox.MessageArrived += Show;
            inbox.MessageResolved += OnResolved;
            inbox.InboxEmpty += OnEmpty;

            if (inbox.Current != null) Show(inbox.Current);
            else ShowStatus("Menunggu pesan masuk...", idleColor);
        }

        private void OnDisable()
        {
            if (acceptButton != null) acceptButton.onClick.RemoveListener(OnAccept);
            if (blockButton != null) blockButton.onClick.RemoveListener(OnBlock);
            if (inbox == null)
                return;
            inbox.MessageArrived -= Show;
            inbox.MessageResolved -= OnResolved;
            inbox.InboxEmpty -= OnEmpty;
        }

        private void OnAccept() { if (inbox != null) inbox.Decide(PlayerDecision.Accept); }

        private void OnBlock() { if (inbox != null) inbox.Decide(PlayerDecision.Block); }

        private void Show(MessageData message)
        {
            if (feedbackRoutine != null)
            {
                StopCoroutine(feedbackRoutine);
                feedbackRoutine = null;
            }

            SetMessageVisible(true);
            channelText.text = message.channel == MessageChannel.Email ? "EMAIL" : "SMS";
            senderText.text = $"<b>{message.senderName}</b>  <noparse><{message.senderAddress}></noparse>";
            subjectText.text = message.channel == MessageChannel.Email ? message.subject : "";
            bodyText.text = BuildBody(message);
        }

        private string BuildBody(MessageData message)
        {
            var sb = new StringBuilder(message.body);
            if (message.links != null && message.links.Length > 0)
            {
                string hex = ColorUtility.ToHtmlStringRGB(linkColor);
                sb.Append("\n");
                // <link="index"> lets LinkTooltip find which link is hovered.
                for (int i = 0; i < message.links.Length; i++)
                    sb.Append($"\n<link=\"{i}\"><color=#{hex}><u>{message.links[i].displayText}</u></color></link>");
            }
            return sb.ToString();
        }

        private void OnResolved(DecisionRecord record)
        {
            string text = record.IsCorrect
                ? "KEPUTUSAN TEPAT"
                : "KEPUTUSAN KELIRU  -  Threat Level naik";
            feedbackRoutine = StartCoroutine(Feedback(text, record.IsCorrect ? correctColor : wrongColor));
        }

        private IEnumerator Feedback(string text, Color color)
        {
            ShowStatus(text, color);
            yield return new WaitForSeconds(feedbackSeconds);
            ShowStatus("Menunggu pesan masuk...", idleColor);
            feedbackRoutine = null;
        }

        private void OnEmpty() => ShowStatus("Tidak ada pesan baru. Bertahan sampai 06:00.", idleColor);

        private void ShowStatus(string text, Color color)
        {
            SetMessageVisible(false);
            statusText.text = text;
            statusText.color = color;
        }

        private void SetMessageVisible(bool visible)
        {
            if (messageRoot != null) messageRoot.SetActive(visible);
            if (statusText != null) statusText.gameObject.SetActive(!visible);
            if (acceptButton != null) acceptButton.interactable = visible;
            if (blockButton != null) blockButton.interactable = visible;
        }
    }
}
