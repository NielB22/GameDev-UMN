using CyberWatch.Threat;
using UnityEngine;

namespace CyberWatch.Audio
{
    /// <summary>
    /// Background sound of the office:
    /// - room tone (2D, always);
    /// - fluorescent buzz coming from the working ceiling light (3D), following that light:
    ///   it dips when the light flickers and goes silent when it dies (Danger stage);
    /// - a horror drone that slowly grows with the Threat;
    /// - a heartbeat from the Critical stage, louder and a bit faster toward 100%.
    /// Works without a ThreatLevelSystem too (e.g. main menu): room tone, buzz and an optional
    /// steady drone (Drone Without Threat).
    /// </summary>
    public class AmbienceAudio : MonoBehaviour
    {
        [Tooltip("Found automatically in the scene when empty (optional).")]
        [SerializeField] private ThreatLevelSystem threat;
        [Tooltip("Light the buzz comes from. Empty = the light named below.")]
        [SerializeField] private Light ceilingLight;
        [SerializeField] private string ceilingLightName = "CeilingLight_Working";

        [Header("Levels (0..1, on top of the Ambience volume)")]
        [SerializeField, Range(0f, 1f)] private float roomToneLevel = 0.5f;
        [SerializeField, Range(0f, 1f)] private float buzzLevel = 0.6f;
        [SerializeField, Range(0f, 1f)] private float droneCalm = 0.12f;
        [SerializeField, Range(0f, 1f)] private float droneCritical = 0.75f;
        [SerializeField, Range(0f, 1f)] private float heartbeatLevel = 0.9f;
        [Tooltip("Steady drone level in scenes without a Threat (menu, intro). 0 = no drone there.")]
        [SerializeField, Range(0f, 1f)] private float droneWithoutThreat = 0f;
        [Tooltip("Heartbeat playback speed at 100% Threat (1 = recorded tempo).")]
        [SerializeField, Range(1f, 2f)] private float heartbeatFastest = 1.25f;
        [Tooltip("How fast drone and heartbeat follow the Threat (level per second).")]
        [SerializeField, Min(0.01f)] private float followSpeed = 0.15f;

        private AudioSource roomTone, buzz, drone, heartbeat;
        private float ceilingBase = 1f;
        private float droneShown, heartShown;

        private void Start()
        {
            AudioLibrary clips = GameAudio.Clips;
            if (clips == null)
            {
                enabled = false;
                return;
            }
            if (threat == null) threat = FindAnyObjectByType<ThreatLevelSystem>();
            if (ceilingLight == null) ceilingLight = FindLight(ceilingLightName);
            if (ceilingLight != null) ceilingBase = Mathf.Max(0.0001f, ceilingLight.intensity);

            roomTone = Start2D(clips.officeRoomTone, roomToneLevel);
            if (ceilingLight != null)
            {
                buzz = GameAudio.CreateLoop(clips.fluorescentBuzz, AudioChannel.Ambience, ceilingLight.transform, buzzLevel);
                if (buzz != null) buzz.Play();
            }

            if (threat == null && droneWithoutThreat > 0f)
                drone = Start2D(clips.horrorDrone, droneWithoutThreat);

            if (threat != null)
            {
                droneShown = DroneTarget();
                drone = Start2D(clips.horrorDrone, droneShown);
                heartbeat = Start2D(clips.heartbeatLoop, 0f);
            }
        }

        private AudioSource Start2D(AudioClip clip, float level)
        {
            AudioSource s = GameAudio.CreateLoop(clip, AudioChannel.Ambience, null, level);
            if (s != null)
            {
                // Start loops at a random point so repeated plays never sound identical.
                if (clip != null && clip.length > 1f) s.time = Random.Range(0f, clip.length * 0.9f);
                s.Play();
            }
            return s;
        }

        private void Update()
        {
            // Buzz follows the ceiling light (flickers, death).
            if (buzz != null && ceilingLight != null)
            {
                float k = ceilingLight.enabled && ceilingLight.gameObject.activeInHierarchy
                    ? Mathf.Clamp01(ceilingLight.intensity / ceilingBase) : 0f;
                GameAudio.SetLevel(buzz, buzzLevel * k);
            }

            if (threat == null)
                return;

            droneShown = Mathf.MoveTowards(droneShown, DroneTarget(), followSpeed * Time.deltaTime);
            GameAudio.SetLevel(drone, droneShown);

            float heartTarget = Mathf.SmoothStep(0f, 1f,
                Mathf.InverseLerp(ThreatLevelSystem.CriticalAbove, ThreatLevelSystem.Max, threat.Value));
            heartShown = Mathf.MoveTowards(heartShown, heartTarget, followSpeed * Time.deltaTime);
            GameAudio.SetLevel(heartbeat, heartbeatLevel * heartShown);
            if (heartbeat != null) heartbeat.pitch = Mathf.Lerp(1f, heartbeatFastest, heartShown);
        }

        private float DroneTarget() =>
            Mathf.Lerp(droneCalm, droneCritical, Mathf.SmoothStep(0f, 1f, threat.Value / ThreatLevelSystem.Max));

        private static Light FindLight(string lightName)
        {
            foreach (Light l in FindObjectsByType<Light>(FindObjectsInactive.Include))
                if (l.name == lightName) return l;
            return null;
        }
    }
}
