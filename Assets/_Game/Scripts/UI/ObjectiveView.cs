using CyberWatch.Core;
using CyberWatch.Session;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CyberWatch.UI
{
    /// <summary>
    /// Always-visible objective panel under the HUD clock:
    ///   TARGET 02:00
    ///   5 / 8 PESAN
    /// White when the target is already met, pulsing orange when it is not and the hour is almost
    /// over. After the last check (05:00) it reads "BERTAHAN SAMPAI 06:00".
    /// Built at runtime inside the HUD canvas, copying the clock panel's look, so the HUD prefab
    /// and the scene need no changes. Numbers come from MessageQuotaRules (same as the rules text).
    /// </summary>
    public class ObjectiveView : MonoBehaviour
    {
        [Tooltip("Found automatically in the scene when empty.")]
        [SerializeField] private ShiftClock clock;
        [Tooltip("HUD panel to sit under and copy the style from.")]
        [SerializeField] private string anchorPanelName = "ClockPanel";
        [Tooltip("Last hour (into the shift) that is checked; after it the panel says to hold out.")]
        [SerializeField, Min(1)] private int lastCheckedHour = 5;
        [Tooltip("Warn (orange pulse) when less than this part of the hour is left and the target is not met.")]
        [SerializeField, Range(0f, 1f)] private float warnWhenHourLeft = 0.4f;

        [Header("Look")]
        [SerializeField] private Color labelColor = new Color(0.78f, 0.78f, 0.72f, 1f);
        [SerializeField] private Color okColor = new Color(0.95f, 0.86f, 0.55f, 1f);
        [SerializeField] private Color warnColor = new Color(1f, 0.55f, 0.15f, 1f);

        private TMP_Text title;
        private TMP_Text value;
        private bool built;

        private void Awake()
        {
            if (clock == null) clock = FindAnyObjectByType<ShiftClock>();
        }

        private void Start() => Build();

        private void Build()
        {
            GameObject anchor = GameObject.Find(anchorPanelName);
            if (anchor == null || clock == null)
            {
                enabled = false;
                return;
            }
            var anchorRt = (RectTransform)anchor.transform;
            TMP_Text[] anchorTexts = anchor.GetComponentsInChildren<TMP_Text>(true);
            TMP_FontAsset labelFont = anchorTexts.Length > 0 ? anchorTexts[0].font : null;
            TMP_FontAsset valueFont = anchorTexts.Length > 1 ? anchorTexts[1].font : labelFont;

            var panel = new GameObject("ObjectivePanel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(anchorRt.parent, false);
            var rt = (RectTransform)panel.transform;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(1f, 1f);
            rt.anchoredPosition = anchorRt.anchoredPosition + new Vector2(0f, -anchorRt.sizeDelta.y - 12f);
            rt.sizeDelta = new Vector2(anchorRt.sizeDelta.x, 80f);
            var bg = panel.GetComponent<Image>();
            Image anchorBg = anchor.GetComponent<Image>();
            bg.color = anchorBg != null ? anchorBg.color : new Color(0.02f, 0.025f, 0.03f, 0.72f);
            bg.raycastTarget = false;

            title = AddText(rt, "Title", labelFont, 16f, labelColor, new Vector2(0f, -10f), 22f, true);
            value = AddText(rt, "Value", valueFont, 34f, okColor, new Vector2(0f, 8f), 40f, false);
            built = true;
        }

        private static TMP_Text AddText(RectTransform parent, string name, TMP_FontAsset font, float size,
            Color color, Vector2 pos, float height, bool top)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            float y = top ? 1f : 0f;
            rt.anchorMin = new Vector2(0f, y);
            rt.anchorMax = new Vector2(1f, y);
            rt.pivot = new Vector2(0.5f, y);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(-20f, height);
            var t = go.GetComponent<TextMeshProUGUI>();
            if (font != null) t.font = font;
            t.fontSize = size;
            t.enableAutoSizing = true;
            t.fontSizeMax = size;
            t.fontSizeMin = size * 0.5f;
            t.color = color;
            t.alignment = TextAlignmentOptions.Center;
            t.raycastTarget = false;
            return t;
        }

        private void Update()
        {
            if (!built)
                return;

            int hoursDone = clock.HoursElapsed;
            if (hoursDone >= lastCheckedHour || clock.IsFinished)
            {
                SetText("OBJEKTIF", $"BERTAHAN SAMPAI {clock.HourOfDay(clock.ShiftHours):00}:00", okColor);
                return;
            }

            int nextHour = hoursDone + 1;
            int required = nextHour * MessageQuotaRules.PerHour();
            int answered = GameManager.Instance.CurrentShift.Decisions.Count;
            bool met = answered >= required;

            // Part of the current in-game hour that is left (1 = just started, 0 = about to change).
            float hourLeft = 1f - Mathf.Repeat(clock.Progress * clock.ShiftHours, 1f);
            Color c = okColor;
            if (!met && hourLeft < warnWhenHourLeft)
            {
                c = warnColor;
                c.a = Mathf.Lerp(0.5f, 1f, (Mathf.Sin(Time.time * 6f) + 1f) * 0.5f);
            }

            SetText($"TARGET {clock.HourOfDay(nextHour):00}:00",
                    $"{Mathf.Min(answered, required)} / {required} PESAN", c);
        }

        private void SetText(string titleText, string valueText, Color valueColor)
        {
            if (title.text != titleText) title.text = titleText;
            if (value.text != valueText) value.text = valueText;
            value.color = valueColor;
        }
    }
}
