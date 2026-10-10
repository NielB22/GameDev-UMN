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

        public static ThreatStage StageFor(float value)
        {
            if (value >= Max) return ThreatStage.Compromised;
            if (value > 75f) return ThreatStage.Critical;
            if (value > 50f) return ThreatStage.Danger;
            if (value > 25f) return ThreatStage.Alert;
            return ThreatStage.Safe;
        }
    }
}
