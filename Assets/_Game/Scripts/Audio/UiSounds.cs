using CyberWatch.Messages;
using UnityEngine;
using UnityEngine.UI;

namespace CyberWatch.Audio
{
    /// <summary>
    /// Interface sounds of a scene: a click on every Button (found at start, inactive ones too)
    /// and the "new mail" chime when a message arrives in the inbox (if there is one).
    /// </summary>
    public class UiSounds : MonoBehaviour
    {
        [Tooltip("Found automatically in the scene when empty (optional).")]
        [SerializeField] private InboxController inbox;
        [SerializeField, Range(0f, 1f)] private float clickLevel = 0.8f;
        [SerializeField, Range(0f, 1f)] private float mailLevel = 0.9f;

        private void Start()
        {
            if (inbox == null) inbox = FindAnyObjectByType<InboxController>();
            if (inbox != null) inbox.MessageArrived += OnMessageArrived;

            foreach (Button b in FindObjectsByType<Button>(FindObjectsInactive.Include))
                b.onClick.AddListener(PlayClick);
        }

        private void OnDestroy()
        {
            if (inbox != null) inbox.MessageArrived -= OnMessageArrived;
        }

        private void PlayClick()
        {
            if (GameAudio.Clips != null)
                GameAudio.Play2D(GameAudio.Clips.uiClick, AudioChannel.UI, clickLevel, Random.Range(0.95f, 1.05f));
        }

        private void OnMessageArrived(MessageData message)
        {
            if (GameAudio.Clips != null)
                GameAudio.Play2D(GameAudio.Clips.mailNotify, AudioChannel.UI, mailLevel);
        }
    }
}
