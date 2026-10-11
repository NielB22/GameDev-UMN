using CyberWatch.Threat;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CyberWatch.UI
{
    /// <summary>
    /// Threat Level bar (the player's "life"): fill width, percentage, stage name and color.
    /// The bar animates smoothly toward the real value.
    /// </summary>
    public class ThreatMeterView : MonoBehaviour
    {
        [Tooltip("Found automatically in the scene when empty.")]
        [SerializeField] private ThreatLevelSystem threat;

        [Header("Parts")]
        [Tooltip("Child of the bar background; its right anchor is moved to show the value.")]
        [SerializeField] private RectTransform fill;
        [SerializeField] private Image fillImage;
        [SerializeField] private TMP_Text valueText;
        [SerializeField] private TMP_Text stageText;

        [Header("Look")]
        [Tooltip("Percent per second the bar moves toward the real value.")]
        [SerializeField, Min(1f)] private float fillSpeed = 60f;
        [SerializeField] private Color safeColor = new Color(0.35f, 0.85f, 0.45f);
        [SerializeField] private Color alertColor = new Color(0.95f, 0.80f, 0.30f);
        [SerializeField] private Color dangerColor = new Color(1.00f, 0.50f, 0.15f);
        [SerializeField] private Color criticalColor = new Color(0.90f, 0.15f, 0.12f);

        private float shown;

        private void Awake()
        {
            if (threat == null)
                threat = FindAnyObjectByType<ThreatLevelSystem>();
        }

        private void OnEnable()
        {
            if (threat == null)
                return;
            threat.StageChanged += OnStageChanged;
            shown = threat.Value;
            Render(threat.Stage);
        }

        private void OnDisable()
        {
            if (threat != null)
                threat.StageChanged -= OnStageChanged;
        }

        private void OnStageChanged(ThreatStage previous, ThreatStage current) => Render(current);

        private void Update()
        {
            if (threat == null || Mathf.Approximately(shown, threat.Value))
                return;
            shown = Mathf.MoveTowards(shown, threat.Value, fillSpeed * Time.deltaTime);
            Render(threat.Stage);
        }

        private void Render(ThreatStage stage)
        {
            if (fill != null)
                fill.anchorMax = new Vector2(shown / ThreatLevelSystem.Max, fill.anchorMax.y);

            Color color = ColorFor(stage);
            if (fillImage != null)
                fillImage.color = color;
            if (valueText != null)
                valueText.text = $"{Mathf.RoundToInt(shown)}%";
            if (stageText != null)
            {
                stageText.text = LabelFor(stage);
                stageText.color = color;
            }
        }

        private Color ColorFor(ThreatStage stage) => stage switch
        {
            ThreatStage.Safe => safeColor,
            ThreatStage.Alert => alertColor,
            ThreatStage.Danger => dangerColor,
            _ => criticalColor
        };

        private static string LabelFor(ThreatStage stage) => stage switch
        {
            ThreatStage.Safe => "AMAN",
            ThreatStage.Alert => "WASPADA",
            ThreatStage.Danger => "BAHAYA",
            ThreatStage.Critical => "KRITIS",
            _ => "SYSTEM COMPROMISED"
        };
    }
}
