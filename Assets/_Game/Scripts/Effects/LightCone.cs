using UnityEngine;

namespace CyberWatch.Effects
{
    /// <summary>
    /// Fake volumetric beam: a faint additive cone under a spot light (street lamps), so the
    /// figure standing below reads as "in the spotlight". Cheap: one small mesh, no real
    /// volumetrics. Follows the light: dims and vanishes when the light flickers or dies.
    /// Put it on the GameObject that has the Light; the cone is built at runtime along the
    /// light's forward axis.
    /// </summary>
    [RequireComponent(typeof(Light))]
    public class LightCone : MonoBehaviour
    {
        [Tooltip("M_LightCone (URP Particles/Unlit, additive).")]
        [SerializeField] private Material material;

        [Header("Shape")]
        [Tooltip("Cone length (meters). Lamp height is about 5.5 m.")]
        [SerializeField, Min(0.5f)] private float length = 5.6f;
        [Tooltip("Full opening angle of the visible beam (narrower than the light itself looks best).")]
        [SerializeField, Range(5f, 120f)] private float angle = 45f;
        [Tooltip("Radius at the lamp end, so the cone does not start from a sharp point.")]
        [SerializeField, Min(0f)] private float topRadius = 0.18f;
        [SerializeField, Range(8, 48)] private int segments = 24;

        [Header("Look")]
        [Tooltip("Brightness of the beam at full light intensity.")]
        [SerializeField, Range(0f, 1f)] private float strength = 0.07f;
        [Tooltip("Beam is brighter near the lamp: higher = fades out faster toward the ground.")]
        [SerializeField, Range(0.2f, 4f)] private float falloff = 1.2f;

        private Light source;
        private float baseIntensity;
        private MeshRenderer coneRenderer;
        private Material instance;
        private Texture2D gradient;
        private Mesh mesh;
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        private void Awake()
        {
            source = GetComponent<Light>();
            baseIntensity = Mathf.Max(0.0001f, source.intensity);
            if (material == null)
            {
                Debug.LogWarning("[LightCone] Material belum diisi (M_LightCone).", this);
                enabled = false;
                return;
            }
            Build();
        }

        private void OnDestroy()
        {
            if (instance != null) Destroy(instance);
            if (gradient != null) Destroy(gradient);
            if (mesh != null) Destroy(mesh);
        }

        private void Build()
        {
            // Vertical gradient: bright at the lamp, fading to nothing at the ground.
            gradient = new Texture2D(1, 64, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < 64; y++)
            {
                float v = y / 63f;                                     // 0 = lamp, 1 = ground
                float a = Mathf.Pow(1f - v, falloff) * Mathf.SmoothStep(0f, 1f, v / 0.08f);
                gradient.SetPixel(0, y, new Color(1f, 1f, 1f, a));
            }
            gradient.Apply(false);

            mesh = BuildMesh();

            var go = new GameObject("LightCone", typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(transform, false);
            go.GetComponent<MeshFilter>().sharedMesh = mesh;
            coneRenderer = go.GetComponent<MeshRenderer>();
            coneRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            coneRenderer.receiveShadows = false;
            instance = new Material(material) { mainTexture = gradient };
            coneRenderer.sharedMaterial = instance;
        }

        // Open cone along +Z (the light's forward): ring of vertices at the lamp and at the far end.
        private Mesh BuildMesh()
        {
            float bottomRadius = topRadius + Mathf.Tan(angle * 0.5f * Mathf.Deg2Rad) * length;
            var verts = new Vector3[(segments + 1) * 2];
            var uvs = new Vector2[verts.Length];
            var tris = new int[segments * 6];
            for (int i = 0; i <= segments; i++)
            {
                float a = i / (float)segments * Mathf.PI * 2f;
                Vector2 dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                verts[i * 2] = new Vector3(dir.x * topRadius, dir.y * topRadius, 0f);
                verts[i * 2 + 1] = new Vector3(dir.x * bottomRadius, dir.y * bottomRadius, length);
                uvs[i * 2] = new Vector2(i / (float)segments, 0f);
                uvs[i * 2 + 1] = new Vector2(i / (float)segments, 1f);
            }
            for (int i = 0; i < segments; i++)
            {
                int t = i * 6, v = i * 2;
                tris[t] = v; tris[t + 1] = v + 2; tris[t + 2] = v + 1;
                tris[t + 3] = v + 1; tris[t + 4] = v + 2; tris[t + 5] = v + 3;
            }
            var m = new Mesh { name = "LightConeMesh", vertices = verts, uv = uvs, triangles = tris };
            m.RecalculateBounds();
            return m;
        }

        private void LateUpdate()
        {
            // Follow the lamp: flickers and outages show in the beam too.
            float level = source.enabled ? source.intensity / baseIntensity : 0f;
            bool visible = level > 0.02f;
            if (coneRenderer.enabled != visible)
                coneRenderer.enabled = visible;
            if (!visible)
                return;

            Color c = source.color * (strength * level);
            c.a = 1f;
            instance.SetColor(BaseColorId, c);
        }
    }
}
