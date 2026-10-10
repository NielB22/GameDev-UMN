using System;
using UnityEngine;

namespace CyberWatch.Messages
{
    public enum MessageChannel
    {
        Email,
        Sms
    }

    public enum PlayerDecision
    {
        Accept,
        Block
    }

    /// <summary>Red flags a player can look for. A message can have several.</summary>
    [Flags]
    public enum ScamIndicator
    {
        None = 0,
        SuspiciousSender = 1 << 0, // misspelled / look-alike domain
        UrgentTone = 1 << 1,       // pressure or threats ("blocked in 5 minutes!")
        AsksForCode = 1 << 2,      // asks for OTP / PIN / password
        MaliciousLink = 1 << 3     // link text differs from the real URL
    }

    [Serializable]
    public struct MessageLink
    {
        [Tooltip("Text shown in the message body.")]
        public string displayText;

        [Tooltip("Real destination, revealed when the player hovers the link.")]
        public string actualUrl;
    }

    /// <summary>
    /// One incoming email/SMS. Create via Assets > Create > Cyber Watch > Message.
    /// Content lives in assets, not code, so new messages need no programming.
    /// </summary>
    [CreateAssetMenu(fileName = "MSG_New", menuName = "Cyber Watch/Message", order = 0)]
    public class MessageData : ScriptableObject
    {
        [Header("Header")]
        public MessageChannel channel = MessageChannel.Email;
        public string senderName;
        [Tooltip("Email address or phone number, shown as-is (include any misspelling here).")]
        public string senderAddress;
        public string subject;

        [Header("Content")]
        [TextArea(4, 12)] public string body;
        public MessageLink[] links = Array.Empty<MessageLink>();

        [Header("Answer")]
        public bool isScam;
        public ScamIndicator indicators;

        [Tooltip("Threat Level added when the player gets this message wrong.")]
        [Range(0f, 100f)] public float threatPenalty = 15f;

        [Tooltip("Threat Level removed when the player correctly blocks a scam.")]
        [Range(0f, 100f)] public float threatRecovery = 5f;

        [Tooltip("Shown in the end-of-shift report when the player gets this wrong.")]
        [TextArea(2, 6)] public string explanation;

        public PlayerDecision CorrectDecision => isScam ? PlayerDecision.Block : PlayerDecision.Accept;

        public bool IsCorrect(PlayerDecision decision) => decision == CorrectDecision;
    }
}
