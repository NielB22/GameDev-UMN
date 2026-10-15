using CyberWatch.Effects;
using UnityEngine;
using UnityEngine.UI;

namespace CyberWatch.UI
{
    /// <summary>
    /// Makes the desk inbox (UI_Desktop) look like an old CRT monitor:
    /// - thin scanlines and darker rounded edges (fake tube curve), always on;
    /// - during a ScreenGlitch (only shown when the Threat rises) the inbox window jumps a few
    ///   pixels sideways, like a broken signal.
    /// Text is never altered, so links and senders stay readable.
    /// Put it on the UI_Desktop root (the Canvas). The overlays are built at runtime and never
    /// block clicks or hover.
    /// </summary>
    public class CrtScreenEffect : MonoBehaviour
    {
        [Tooltip("The window that shakes during a screen glitch (InboxWindow).")]
        [SerializeField] private RectTransform window;

        [Header("Scanlines")]
        [SerializeField, Range(0f, 1f)] private float scanlineAlpha = 0.12f;
        [Tooltip("Screen pixels per scanline.")]
        [SerializeField, Min(2)] private int scanlinePixels = 3;

        [Header("Tube edges")]
        [SerializeField, Range(0f, 1f)] private float edgeDarkness = 0.45f;
        [Tooltip("0 = darkening starts at the center, 1 = only in the corners.")]
        [SerializeField, Range(0f, 1f)] private float edgeStart = 0.55f;

        [Header("Signal glitch (with ScreenGlitch)")]
        [Tooltip("Max sideways jump in pixels at full glitch strength.")]
        [SerializeField, Min(0f)] private float glitchPixels = 6f;

        private RawImage scanlines;
        private Texture2D scanTex;
        private Texture2D edgeTex;
        private Vector2 windowHome;
        private int lastScreenHeight;

        private void Awake()
        {
            if (window != null) windowHome = window.anchoredPosition;
            Build();
        }

        private void OnDisable()
        {
            if (window != null) window.anchoredPosition = windowHome;
        }

        private void OnDestroy()
        {
            if (scanTex != null) Destroy(scanTex);
            if (edgeTex != null) Destroy(edgeTex);
        }

        private void Build()
        {
            // Scanlines: one dark row out of N, tiled over the screen.
            scanTex = new Texture2D(1, scanlinePixels, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Repeat
            };
            for (int y = 0; y < scanlinePixels; y++)
                scanTex.SetPixel(0, y, new Color(0f, 0f, 0f, y == 0 ? 1f : 0f));
            scanTex.Apply(false);
            scanlines = CreateRaw("CRT_Scanlines", scanTex, new Color(1f, 1f, 1f, scanlineAlpha));

            // Edges: black, transparent in the middle, darker toward the rounded corners.
            const int size = 64;
            edgeTex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float u = (x + 0.5f) / size * 2f - 1f, v = (y + 0.5f) / size * 2f - 1f;
                // Superellipse distance gives the squarish CRT tube shape.
                float d = Mathf.Pow(Mathf.Pow(Mathf.Abs(u), 4f) + Mathf.Pow(Mathf.Abs(v), 4f), 0.25f);
                float a = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(edgeStart, 1.05f, d));
                edgeTex.SetPixel(x, y, new Color(0f, 0f, 0f, a));
            }
            edgeTex.Apply(false);
            CreateRaw("CRT_Edges", edgeTex, new Color(1f, 1f, 1f, edgeDarkness));
        }

        private RawImage CreateRaw(string name, Texture texture, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(RawImage));
            go.transform.SetParent(transform, false); // last child = drawn on top
            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var raw = go.GetComponent<RawImage>();
            raw.texture = texture;
            raw.color = color;
            raw.raycastTarget = false;
            return raw;
        }

        private void Update()
        {
            // Keep one scanline every N real screen pixels whatever the resolution.
            if (Screen.height != lastScreenHeight)
            {
                lastScreenHeight = Screen.height;
                scanlines.uvRect = new Rect(0f, 0f, 1f, Screen.height / (float)scanlinePixels);
            }

            if (window == null)
                return;

            float glitch = ScreenGlitch.Strength;
            if (glitch > 0.01f)
            {
                window.anchoredPosition = windowHome + new Vector2(
                    Random.Range(-1f, 1f) * glitchPixels * glitch,
                    Random.Range(-0.3f, 0.3f) * glitchPixels * glitch);
            }
            else if (window.anchoredPosition != windowHome)
            {
                window.anchoredPosition = windowHome;
            }
        }
    }
}
