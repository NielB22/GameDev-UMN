using System;
using UnityEngine;

namespace CyberWatch.Threat
{
    /// <summary>Threat stages from the design doc.</summary>
    public enum ThreatStage
    {
        Safe,        // 0-25%
        Alert,       // 26-50%
        Danger,      // 51-75%
        Critical,    // 76-99%
        Compromised  // 100% -> lose
    }

    /// <summary>
    /// Owns the Threat Level value. Environment reactions (lights, CCTV, silhouette, audio)
    /// subscribe to the events instead of polling, so each effect stays independent.
    /// </summary>
    public class ThreatLevelSystem : MonoBehaviour
    {
        public const float Max = 100f;

        // Stage boundaries (design doc). A value ABOVE the boundary belongs to the next stage.
        // Every system that needs thresholds uses these, so they can never drift apart.
        public const float AlertAbove = 25f;
        public const float DangerAbove = 50f;
        public const float CriticalAbove = 75f;

        [SerializeField, Range(0f, Max)] private float startingThreat;

        public float Value { get; private set; }
        public float Normalized => Value / Max;
        public ThreatStage Stage { get; private set; }

        /// <summary>New value (0-100).</summary>
        public event Action<float> ThreatChanged;

        /// <summary>Previous stage, new stage.</summary>
        public event Action<ThreatStage, ThreatStage> StageChanged;

        /// <summary>Raised once when the threat reaches 100%.</summary>
        public event Action Compromised;

        private void Awake()
        {
            Value = Mathf.Clamp(startingThreat, 0f, Max);
            Stage = StageFor(Value);
        }

        public void Increase(float amount) => SetValue(Value + Mathf.Abs(amount));

        public void Decrease(float amount) => SetValue(Value - Mathf.Abs(amount));

        public void SetValue(float value)
        {
            if (Stage == ThreatStage.Compromised)
                return;

            float clamped = Mathf.Clamp(value, 0f, Max);
            if (Mathf.Approximately(clamped, Value))
                return;

            Value = clamped;
            ThreatChanged?.Invoke(Value);

            ThreatStage newStage = StageFor(Value);
            if (newStage == Stage)
                return;

            ThreatStage previous = Stage;
            Stage = newStage;
            StageChanged?.Invoke(previous, newStage);

            if (newStage == ThreatStage.Compromised)
                Compromised?.Invoke();
        }

        // ---------- Testing helpers ----------
        // The ONLY place to change Threat while testing: in Play Mode, right-click the
        // "Threat Level System" component header > Debug > ...  Every listener reacts
        // (bar, CCTV, lights, silhouette, post-process, win/lose).

        [ContextMenu("Debug/Threat +25")]
        private void DebugIncrease() => DebugSet(Value + 25f);

        [ContextMenu("Debug/Threat -25")]
        private void DebugDecrease() => DebugSet(Value - 25f);

        [ContextMenu("Debug/Set 10 (Aman)")]
        private void DebugSafe() => DebugSet(10f);

        [ContextMenu("Debug/Set 35 (Waspada)")]
        private void DebugAlert() => DebugSet(35f);

        [ContextMenu("Debug/Set 55 (Bahaya)")]
        private void DebugDanger() => DebugSet(55f);

        [ContextMenu("Debug/Set 80 (Kritis)")]
        private void DebugCritical() => DebugSet(80f);

        [ContextMenu("Debug/Set 95 (Kritis, hampir kalah)")]
        private void DebugAlmostLost() => DebugSet(95f);

        private void DebugSet(float value)
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[ThreatLevelSystem] Debug menu works in Play Mode only.", this);
                return;
            }
            if (Stage == ThreatStage.Compromised)
            {
                Debug.LogWarning("[ThreatLevelSystem] Shift already lost (100%); Threat is locked.", this);
                return;
            }
            SetValue(value);
        }

        public static ThreatStage StageFor(float value)
        {
            if (value >= Max) return ThreatStage.Compromised;
            if (value > CriticalAbove) return ThreatStage.Critical;
            if (value > DangerAbove) return ThreatStage.Danger;
            if (value > AlertAbove) return ThreatStage.Alert;
            return ThreatStage.Safe;
        }
    }
}
