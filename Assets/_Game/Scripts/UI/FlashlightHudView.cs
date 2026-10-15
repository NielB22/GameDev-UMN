using CyberWatch.Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CyberWatch.UI
{
    /// <summary>
    /// HUD for the flashlight (walk mode only), in the corner above the [V] hint:
    /// - "[F]  SENTER" + battery bar (warm white, orange when low).
    /// - Dead: pulsing red "SENTER MATI - GOYANGKAN MOUSE!" and the bar shows the shake progress.
    /// </summary>
    public class FlashlightHudView : MonoBehaviour
    {
        [Tooltip("Found automatically in the scene when empty.")]
        [SerializeField] private Flashlight flashlight;
        [Tooltip("Found automatically in the scene when empty.")]
        [SerializeField] private PlayerModeController modes;

        [Header("UI")]
        [SerializeField] private GameObject root;
        [SerializeField] private TMP_Text label;
        [Tooltip("Fill of the battery bar; its anchor max X is set to the battery level.")]
        [SerializeField] private RectTransform barFill;
        [SerializeField] private Graphic barFillGraphic;

        [Header("Texts")]
        [SerializeField] private string readyText = "[F]  SENTER";
        [SerializeField] private string deadText = "SENTER MATI - GOYANGKAN MOUSE!";

        [Header("Colors")]
        [SerializeField] private Color normalColor = new Color(1f, 0.92f, 0.76f, 0.9f);
        [SerializeField] private Color lowColor = new Color(1f, 0.55f, 0.15f, 0.95f);
        [SerializeField] private Color deadColor = new Color(1f, 0.25f, 0.2f, 1f);
        [Tooltip("Battery fraction shown in the low color.")]
        [SerializeField, Range(0f, 1f)] private float lowAt = 0.2f;
        [SerializeField, Min(0.1f)] private float pulseSpeed = 6f;

        private void Awake()
        {
            if (flashlight == null) flashlight = FindAnyObjectByType<Flashlight>();
            if (modes == null) modes = FindAnyObjectByType<PlayerModeController>();
        }

        private void Update()
        {
            bool show = flashlight != null && (modes == null || modes.Mode == PlayerMode.Walk);
            if (root != null && root.activeSelf != show)
                root.SetActive(show);
            if (!show)
                return;

            bool dead = flashlight.Current == Flashlight.State.Dead;
            float level = dead ? flashlight.ShakeProgress : flashlight.Battery;

            Color color = normalColor;
            if (dead)
            {
                color = deadColor;
                color.a *= Mathf.Lerp(0.45f, 1f, (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f);
            }
            else if (flashlight.Current == Flashlight.State.Dying || flashlight.Battery <= lowAt)
            {
                color = lowColor;
            }

            string text = dead ? deadText : readyText;
            if (label != null)
            {
                if (label.text != text) label.text = text;
                label.color = color;
            }

            if (barFill != null)
                barFill.anchorMax = new Vector2(Mathf.Clamp01(level), 1f);
            if (barFillGraphic != null)
                barFillGraphic.color = color;
        }
    }
}
