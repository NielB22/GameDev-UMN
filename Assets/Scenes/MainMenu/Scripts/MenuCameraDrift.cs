using UnityEngine;

namespace CyberWatch.Menu
{
    /// <summary>
    /// Unsettling idle camera for the main menu (put it on Main Camera):
    /// - slow push-in / pull-back, like someone slowly creeping forward,
    /// - subtle hand-held sway (Perlin noise), never jerky,
    /// - every so often the camera slowly "glances" aside (toward the window) and back.
    /// Everything is relative to the camera's starting pose, so you can still place the shot in the editor.
    /// </summary>
    public class MenuCameraDrift : MonoBehaviour
    {
        [Header("Creep forward / back")]
        [SerializeField, Min(0f)] private float creepDistance = 0.35f;
        [SerializeField, Min(1f)] private float creepPeriod = 26f;

        [Header("Hand-held sway (degrees)")]
        [SerializeField] private Vector3 swayAngles = new Vector3(0.6f, 0.9f, 0.35f);
        [SerializeField, Min(0.01f)] private float swaySpeed = 0.12f;
        [SerializeField, Min(0f)] private float swayPosition = 0.02f;

        [Header("Slow glance")]
        [Tooltip("Degrees the camera turns during a glance (positive = to the right).")]
        [SerializeField] private float glanceYaw = 9f;
        [SerializeField] private float glancePitch = -2f;
        [SerializeField] private Vector2 glanceInterval = new Vector2(14f, 26f);
        [Tooltip("Seconds to turn, hold, and turn back.")]
        [SerializeField] private Vector3 glanceTiming = new Vector3(2.5f, 2f, 3f);

        private Vector3 basePosition;
        private Quaternion baseRotation;
        private float seed;
        private float nextGlanceAt;
        private float glanceStart = -1f;

        private void Start()
        {
            basePosition = transform.localPosition;
            baseRotation = transform.localRotation;
            seed = Random.value * 100f;
            ScheduleGlance(Time.unscaledTime);
        }

        private void LateUpdate()
        {
            float t = Time.unscaledTime;

            // Creep: smooth 0..1..0 along the view direction.
            float creep = (1f - Mathf.Cos(t * 2f * Mathf.PI / creepPeriod)) * 0.5f * creepDistance;

            // Sway: Perlin noise in -1..1 per axis.
            float n = t * swaySpeed;
            Vector3 noise = new Vector3(
                Mathf.PerlinNoise(seed, n) * 2f - 1f,
                Mathf.PerlinNoise(seed + 17.3f, n) * 2f - 1f,
                Mathf.PerlinNoise(seed + 41.7f, n) * 2f - 1f);

            float glance = GlanceWeight(t);

            Quaternion offset = Quaternion.Euler(
                noise.x * swayAngles.x + glance * glancePitch,
                noise.y * swayAngles.y + glance * glanceYaw,
                noise.z * swayAngles.z);

            Quaternion rotation = baseRotation * offset;
            transform.localRotation = rotation;
            transform.localPosition = basePosition
                + baseRotation * Vector3.forward * creep
                + baseRotation * new Vector3(noise.y, noise.x, 0f) * swayPosition;
        }

        // 0 = looking normally, 1 = fully turned. Ease in/out so it never snaps.
        private float GlanceWeight(float t)
        {
            if (glanceStart < 0f)
            {
                if (t >= nextGlanceAt) glanceStart = t;
                else return 0f;
            }

            float local = t - glanceStart;
            float turn = glanceTiming.x, hold = glanceTiming.y, back = glanceTiming.z;
            if (local < turn) return Mathf.SmoothStep(0f, 1f, local / turn);
            if (local < turn + hold) return 1f;
            if (local < turn + hold + back) return Mathf.SmoothStep(1f, 0f, (local - turn - hold) / back);

            glanceStart = -1f;
            ScheduleGlance(t);
            return 0f;
        }

        private void ScheduleGlance(float now) => nextGlanceAt = now + Random.Range(glanceInterval.x, glanceInterval.y);
    }
}
