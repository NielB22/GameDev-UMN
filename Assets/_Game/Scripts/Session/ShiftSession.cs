using System.Collections.Generic;
using CyberWatch.Messages;

namespace CyberWatch.Session
{
    public enum ShiftOutcome
    {
        InProgress,
        Survived,   // reached 06:00 with threat below 100%
        Compromised // threat reached 100%
    }

    public readonly struct DecisionRecord
    {
        public readonly int Number;
        public readonly MessageData Message;
        public readonly PlayerDecision Decision;

        public DecisionRecord(int number, MessageData message, PlayerDecision decision)
        {
            Number = number;
            Message = message;
            Decision = decision;
        }

        public bool IsCorrect => Message.IsCorrect(Decision);
    }

    /// <summary>
    /// Plain data for one night shift. Filled during gameplay, read by the report scene.
    /// </summary>
    public class ShiftSession
    {
        private readonly List<DecisionRecord> decisions = new List<DecisionRecord>();

        public ShiftOutcome Outcome { get; set; } = ShiftOutcome.InProgress;
        public IReadOnlyList<DecisionRecord> Decisions => decisions;

        public DecisionRecord Record(MessageData message, PlayerDecision decision)
        {
            var record = new DecisionRecord(decisions.Count + 1, message, decision);
            decisions.Add(record);
            return record;
        }

        public int CorrectCount
        {
            get
            {
                int count = 0;
                foreach (DecisionRecord d in decisions)
                    if (d.IsCorrect) count++;
                return count;
            }
        }

        /// <summary>0-1. Returns 0 when nothing was decided.</summary>
        public float Accuracy => decisions.Count == 0 ? 0f : (float)CorrectCount / decisions.Count;

        public List<DecisionRecord> GetMistakes()
        {
            var mistakes = new List<DecisionRecord>();
            foreach (DecisionRecord d in decisions)
                if (!d.IsCorrect) mistakes.Add(d);
            return mistakes;
        }
    }
}
