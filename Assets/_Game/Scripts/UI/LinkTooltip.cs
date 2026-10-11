using CyberWatch.Messages;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace CyberWatch.UI
{
    /// <summary>
    /// Hovering a link in the message body shows a tooltip with the link's REAL destination,
    /// like a browser status bar. Links are tagged by <see cref="InboxView"/> as &lt;link="index"&gt;.
    /// </summary>
    public class LinkTooltip : MonoBehaviour
    {
        [Tooltip("Found automatically in the scene when empty.")]
        [SerializeField] private InboxController inbox;
        [SerializeField] private TMP_Text bodyText;

        [Header("Tooltip")]
        [SerializeField] private RectTransform tooltip;
        [SerializeField] private Image tooltipBackground;
        [SerializeField] private TMP_Text tooltipText;
        [SerializeField] private Vector2 cursorOffset = new Vector2(18f, -18f);
        [SerializeField] private Vector2 padding = new Vector2(14f, 10f);
        [SerializeField, Min(100f)] private float maxWidth = 620f;

        [Header("Hint")]
        [Tooltip("Color the tooltip red when the real URL differs from the text shown. Turn off for a harder game.")]
        [SerializeField] private bool highlightMismatch = true;
        [SerializeField] private Color normalColor = new Color(0.08f, 0.09f, 0.11f, 0.96f);
        [SerializeField] private Color mismatchColor = new Color(0.45f, 0.08f, 0.07f, 0.96f);

        private int shownLink = -1;

        private void Awake()
        {
            if (inbox == null)
                inbox = FindAnyObjectByType<InboxController>();
            Hide();
        }

        private void Update()
        {
            MessageData message = inbox != null ? inbox.Current : null;
            Mouse mouse = Mouse.current;
            if (message == null || mouse == null || bodyText == null || !bodyText.gameObject.activeInHierarchy)
            {
                Hide();
                return;
            }

            Vector2 screen = mouse.position.ReadValue();
            int hit = TMP_TextUtilities.FindIntersectingLink(bodyText, screen, null);
            if (hit < 0 || !int.TryParse(bodyText.textInfo.linkInfo[hit].GetLinkID(), out int index)
                || message.links == null || index < 0 || index >= message.links.Length)
            {
                Hide();
                return;
            }

            if (index != shownLink)
                Show(message.links[index]);
            shownLink = index;
            Follow(screen);
        }

        private void Show(MessageLink link)
        {
            bool mismatch = !SameDestination(link.displayText, link.actualUrl);
            string text = $"<b>URL TUJUAN:</b>\n<noparse>{link.actualUrl}</noparse>";
            if (highlightMismatch && mismatch)
                text += "\n<b>PERHATIAN:</b> berbeda dari teks link!";

            tooltipText.text = text;
            tooltipBackground.color = highlightMismatch && mismatch ? mismatchColor : normalColor;

            Vector2 size = tooltipText.GetPreferredValues(text, maxWidth, 0f);
            size.x = Mathf.Min(size.x, maxWidth);
            tooltip.sizeDelta = size + padding * 2f;
            tooltip.gameObject.SetActive(true);
        }

        private void Follow(Vector2 screen)
        {
            var parent = (RectTransform)tooltip.parent;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screen, null, out Vector2 local);

            // Flip to the left/up near the screen edges so the tooltip stays visible.
            bool left = screen.x > Screen.width * 0.6f;
            bool up = screen.y < Screen.height * 0.3f;
            tooltip.pivot = new Vector2(left ? 1f : 0f, up ? 0f : 1f);
            Vector2 offset = new Vector2(left ? -cursorOffset.x : cursorOffset.x, up ? -cursorOffset.y : cursorOffset.y);
            tooltip.localPosition = local + offset;
        }

        private void Hide()
        {
            shownLink = -1;
            if (tooltip != null)
                tooltip.gameObject.SetActive(false);
        }

        /// <summary>True when the shown text and the real URL point to the same place (ignores http/https/www).</summary>
        private static bool SameDestination(string shown, string actual) => Normalize(shown) == Normalize(actual);

        private static string Normalize(string url)
        {
            if (string.IsNullOrEmpty(url))
                return "";
            string s = url.Trim().ToLowerInvariant();
            int scheme = s.IndexOf("://", System.StringComparison.Ordinal);
            if (scheme >= 0) s = s.Substring(scheme + 3);
            if (s.StartsWith("www.")) s = s.Substring(4);
            return s.TrimEnd('/');
        }
    }
}
