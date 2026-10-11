using System;
using System.Collections;
using System.Collections.Generic;
using CyberWatch.Core;
using CyberWatch.Session;
using CyberWatch.Threat;
using UnityEngine;

namespace CyberWatch.Messages
{
    /// <summary>
    /// Delivers messages one at a time and judges the player's decision:
    /// a wrong call raises the Threat Level, correctly blocking a scam lowers it.
    /// Every decision is recorded in the current <see cref="ShiftSession"/> for the report.
    /// </summary>
    public class InboxController : MonoBehaviour
    {
        [SerializeField] private List<MessageData> messages = new List<MessageData>();
        [SerializeField] private bool shuffle;
        [Tooltip("Found automatically in the scene when empty.")]
        [SerializeField] private ThreatLevelSystem threat;

        [Header("Timing (real seconds)")]
        [SerializeField, Min(0f)] private float firstMessageDelay = 3f;
        [SerializeField] private Vector2 nextMessageDelay = new Vector2(4f, 8f);

        private readonly Queue<MessageData> pending = new Queue<MessageData>();
        private bool closed;

        public MessageData Current { get; private set; }
        public int TotalMessages => messages.Count;

        /// <summary>A new message is waiting for a decision.</summary>
        public event Action<MessageData> MessageArrived;

        /// <summary>The player decided on a message (record says if it was correct).</summary>
        public event Action<DecisionRecord> MessageResolved;

        /// <summary>No more messages will arrive this shift.</summary>
        public event Action InboxEmpty;

        private void Awake()
        {
            if (threat == null)
                threat = FindAnyObjectByType<ThreatLevelSystem>();

            var order = new List<MessageData>(messages);
            order.RemoveAll(m => m == null);
            if (shuffle)
            {
                for (int i = order.Count - 1; i > 0; i--)
                {
                    int j = UnityEngine.Random.Range(0, i + 1);
                    (order[i], order[j]) = (order[j], order[i]);
                }
            }
            foreach (MessageData m in order)
                pending.Enqueue(m);
        }

        private void Start() => StartCoroutine(DeliverAfter(firstMessageDelay));

        /// <summary>Stops the inbox when the shift is over; later decisions are ignored.</summary>
        public void Close()
        {
            closed = true;
            Current = null;
            StopAllCoroutines();
        }

        /// <summary>Called by the UI buttons.</summary>
        public void Decide(PlayerDecision decision)
        {
            if (closed || Current == null)
                return;

            MessageData message = Current;
            Current = null;

            DecisionRecord record = GameManager.Instance.CurrentShift.Record(message, decision);
            if (threat != null)
            {
                if (!record.IsCorrect)
                    threat.Increase(message.threatPenalty);
                else if (message.isScam)
                    threat.Decrease(message.threatRecovery);
            }

            MessageResolved?.Invoke(record);
            if (closed) // the decision itself may have ended the shift (threat 100%)
                return;
            StartCoroutine(DeliverAfter(UnityEngine.Random.Range(nextMessageDelay.x, nextMessageDelay.y)));
        }

        private IEnumerator DeliverAfter(float delay)
        {
            yield return new WaitForSeconds(delay);

            if (pending.Count == 0)
            {
                InboxEmpty?.Invoke();
                yield break;
            }

            Current = pending.Dequeue();
            MessageArrived?.Invoke(Current);
        }
    }
}
