using System.Collections;
using CyberWatch.Session;
using TMPro;
using UnityEngine;

namespace CyberWatch.UI
{
    /// <summary>
    /// Banner at the top of the screen when the hourly message target is missed:
    /// "TARGET 02:00 TIDAK TERCAPAI  ·  +24 THREAT". Pulses for a few seconds, then fades.
    /// Builds its own overlay canvas at runtime; uses the HUD's font.
    /// </summary>
    public class QuotaWarningView : MonoBehaviour
    {
        [Tooltip("Found automatically in the scene when empty.")]
        [SerializeField] private MessageQuota quota;
        [SerializeField] private string format = "TARGET {0:00}:00 TIDAK TERCAPAI  -  {1} PESAN TERTINGGAL  -  +{2} THREAT";
        [SerializeField] private Color color = new Color(1f, 0.55f, 0.15f, 1f);
        [SerializeField, Min(10f)] private float fontSize = 34f;
        [SerializeField, Min(0.5f)] private float showSeconds = 4f;

        private GameObject canvasGo;
        private TMP_Text text;
        private Coroutine showing;

        private void Awake()
        {
            if (quota == null) quota = FindAnyObjectByType<MessageQuota>();
        }

        private void OnEnable()
        {
            if (quota != null) quota.TargetMissed += OnTargetMissed;
        }

        private void OnDisable()
        {
            if (quota != null) quota.TargetMissed -= OnTargetMissed;
        }

        private void OnTargetMissed(int hour, int missing, float threatAdded)
        {
            Build();
            text.text = string.Format(format, hour, missing, Mathf.RoundToInt(threatAdded));
            if (showing != null) StopCoroutine(showing);
            showing = StartCoroutine(Show());
        }

        private IEnumerator Show()
        {
            canvasGo.SetActive(true);
            for (float t = 0f; t < showSeconds; t += Time.deltaTime)
            {
                float pulse = Mathf.Lerp(0.55f, 1f, (Mathf.Sin(t * 6f) + 1f) * 0.5f);
                float fade = Mathf.Clamp01((showSeconds - t) / 0.6f); // fade out at the end
                Color c = color;
                c.a = pulse * fade;
                text.color = c;
                yield return null;
            }
            canvasGo.SetActive(false);
            showing = null;
        }

        private void Build()
        {
            if (canvasGo != null)
                return;
            TMP_Text hudText = FindAnyObjectByType<TMP_Text>(); // borrow the HUD font
            canvasGo = new GameObject("QuotaWarning", typeof(Canvas));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 300; // above HUD and inbox, below glitch and endings

            var go = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(canvasGo.transform, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(0.1f, 1f);
            rt.anchorMax = new Vector2(0.9f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -150f);
            rt.sizeDelta = new Vector2(0f, 60f);

            text = go.GetComponent<TextMeshProUGUI>();
            if (hudText != null) text.font = hudText.font;
            text.alignment = TextAlignmentOptions.Center;
            text.enableAutoSizing = true;
            text.fontSizeMax = fontSize;
            text.fontSizeMin = 18f;
            text.raycastTarget = false;
            canvasGo.SetActive(false);
        }
    }
}
