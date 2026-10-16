using System.Collections.Generic;
using UnityEngine;

namespace CyberWatch.Audio
{
    public enum AudioChannel
    {
        Ambience,
        Sfx,
        Jumpscare,
        UI
    }

    /// <summary>
    /// Central audio for a scene: one volume per channel (Ambience, SFX, Jumpscare, UI) plus master,
    /// and simple helpers other scripts use instead of managing AudioSources themselves:
    /// - Play2D: a one-shot "in the player's head" (stereo kept), e.g. UI, jumpscare layers;
    /// - PlayAt: a one-shot coming from a point in the world (3D), e.g. a desk lamp sputtering;
    /// - PlayOutside: like PlayAt but muffled and quieter, for sounds outside the office;
    /// - CreateLoop: a looping source (2D or attached to an object in 3D) whose volume follows
    ///   its channel; change its level with SetLevel.
    /// Without a GameAudio in the scene every call simply does nothing.
    /// </summary>
    public class GameAudio : MonoBehaviour
    {
        [SerializeField] private AudioLibrary library;

        [Header("Volumes")]
        [SerializeField, Range(0f, 1f)] private float master = 1f;
        [SerializeField, Range(0f, 1f)] private float ambience = 0.8f;
        [SerializeField, Range(0f, 1f)] private float sfx = 0.9f;
        [SerializeField, Range(0f, 1f)] private float jumpscare = 1f;
        [SerializeField, Range(0f, 1f)] private float ui = 0.7f;

        [Header("Responsiveness")]
        [Tooltip("Samples quieter than this at the start of a short sound are skipped (no delay before the sound).")]
        [SerializeField, Range(0.001f, 0.1f)] private float silenceThreshold = 0.01f;

        [Header("3D sounds")]
        [Tooltip("Full volume within this distance (meters).")]
        [SerializeField, Min(0.1f)] private float minDistance = 1.5f;
        [Tooltip("Inaudible beyond this distance (meters).")]
        [SerializeField, Min(1f)] private float maxDistance = 30f;

        [Header("Sounds from outside (through glass and walls)")]
        [Tooltip("Extra volume factor for outdoor sounds.")]
        [SerializeField, Range(0f, 1f)] private float outdoorVolume = 0.07f;
        [Tooltip("Low-pass cutoff (Hz): lower = more muffled.")]
        [SerializeField, Range(200f, 5000f)] private float outdoorCutoff = 450f;
        [Tooltip("Amount of open-air reverb (0 = none).")]
        [SerializeField, Range(0f, 1f)] private float outdoorReverb = 0.55f;

        private static GameAudio instance;
        private readonly List<AudioSource> oneShots = new List<AudioSource>();
        private readonly List<Loop> loops = new List<Loop>();
        private readonly Dictionary<AudioClip, int> startOffsets = new Dictionary<AudioClip, int>();

        private class Loop
        {
            public AudioSource source;
            public AudioChannel channel;
            public float level = 1f;
        }

        /// <summary>All clips of the game (null when no GameAudio is in the scene).</summary>
        public static AudioLibrary Clips => instance != null ? instance.library : null;

        public static bool Available => instance != null && instance.library != null;

        private void Awake()
        {
            instance = this;
        }

        private void OnDestroy()
        {
            if (instance == this) instance = null;
        }

        private void Update()
        {
            // Volume sliders apply live to the loops.
            foreach (Loop l in loops)
                if (l.source != null) l.source.volume = Volume(l.channel) * l.level;
        }

        /// <summary>Final volume of a channel (channel x master).</summary>
        public static float Volume(AudioChannel channel)
        {
            if (instance == null) return 0f;
            float v = channel switch
            {
                AudioChannel.Ambience => instance.ambience,
                AudioChannel.Sfx => instance.sfx,
                AudioChannel.Jumpscare => instance.jumpscare,
                _ => instance.ui
            };
            return v * instance.master;
        }

        /// <summary>One-shot heard "in the head" (2D, stereo kept).</summary>
        public static AudioSource Play2D(AudioClip clip, AudioChannel channel, float volume = 1f, float pitch = 1f)
        {
            if (instance == null || clip == null) return null;
            AudioSource s = instance.FreeOneShot();
            s.transform.localPosition = Vector3.zero;
            s.spatialBlend = 0f;
            instance.SetOutdoor(s, false);
            return instance.Fire(s, clip, channel, volume, pitch);
        }

        /// <summary>One-shot coming from a point in the world (3D).</summary>
        public static AudioSource PlayAt(AudioClip clip, Vector3 position, AudioChannel channel, float volume = 1f, float pitch = 1f)
        {
            if (instance == null || clip == null) return null;
            AudioSource s = instance.FreeOneShot();
            s.transform.position = position;
            s.spatialBlend = 1f;
            instance.SetOutdoor(s, false);
            return instance.Fire(s, clip, channel, volume, pitch);
        }

        /// <summary>
        /// One-shot from OUTSIDE the office (3D), heard through the glass and walls:
        /// muffled (low-pass), quieter (Outdoor Volume) and with a little open-air reverb.
        /// </summary>
        public static AudioSource PlayOutside(AudioClip clip, Vector3 position, AudioChannel channel, float volume = 1f, float pitch = 1f)
        {
            if (instance == null || clip == null) return null;
            AudioSource s = instance.FreeOneShot();
            s.transform.position = position;
            s.spatialBlend = 1f;
            instance.SetOutdoor(s, true);
            return instance.Fire(s, clip, channel, volume * instance.outdoorVolume, pitch);
        }

        private void SetOutdoor(AudioSource s, bool outdoor)
        {
            // Outdoor sounds are spread out a little so they feel distant rather than pinpoint.
            s.spread = outdoor ? 90f : 0f;
            SetOutdoorFilters(s, outdoor);
        }

        private void SetOutdoorFilters(AudioSource s, bool outdoor)
        {
            var lowPass = s.GetComponent<AudioLowPassFilter>();
            var reverb = s.GetComponent<AudioReverbFilter>();
            if (outdoor && lowPass == null)
            {
                lowPass = s.gameObject.AddComponent<AudioLowPassFilter>();
                reverb = s.gameObject.AddComponent<AudioReverbFilter>();
                reverb.reverbPreset = AudioReverbPreset.Plain;
            }
            if (lowPass != null)
            {
                lowPass.enabled = outdoor;
                lowPass.cutoffFrequency = outdoorCutoff;
            }
            if (reverb != null)
            {
                reverb.enabled = outdoor;
                reverb.dryLevel = 0f;
                reverb.room = Mathf.Lerp(-10000f, 0f, outdoorReverb);
            }
        }

        /// <summary>
        /// Looping source. With a parent it is 3D and follows that object; without one it is 2D.
        /// Start it with source.Play(); set its level (0..1, on top of the channel volume) with SetLevel.
        /// </summary>
        public static AudioSource CreateLoop(AudioClip clip, AudioChannel channel, Transform parent = null, float level = 1f)
        {
            if (instance == null || clip == null) return null;
            var go = new GameObject("Loop_" + clip.name);
            go.transform.SetParent(parent != null ? parent : instance.transform, false);
            AudioSource s = go.AddComponent<AudioSource>();
            instance.Setup3D(s);
            s.clip = clip;
            s.loop = true;
            s.playOnAwake = false;
            s.spatialBlend = parent != null ? 1f : 0f;
            instance.loops.Add(new Loop { source = s, channel = channel, level = level });
            s.volume = Volume(channel) * level;
            return s;
        }

        /// <summary>Change a loop's own level (0..1); the channel volume still applies on top.</summary>
        public static void SetLevel(AudioSource loop, float level)
        {
            if (instance == null || loop == null) return;
            foreach (Loop l in instance.loops)
                if (l.source == loop) { l.level = Mathf.Max(0f, level); return; }
        }

        /// <summary>Stop every loop and one-shot (e.g. the silence after the blackout).</summary>
        public static void StopAll()
        {
            if (instance == null) return;
            foreach (Loop l in instance.loops) if (l.source != null) l.source.Stop();
            foreach (AudioSource s in instance.oneShots) if (s != null) s.Stop();
        }

        private AudioSource Fire(AudioSource s, AudioClip clip, AudioChannel channel, float volume, float pitch)
        {
            s.clip = clip;
            s.volume = Volume(channel) * volume;
            s.pitch = pitch;
            s.loop = false;
            s.timeSamples = AudibleStart(clip); // skip the silent start (MP3 padding) -> instant
            s.Play();
            return s;
        }

        // First sample that is actually audible, measured once per clip. Only for clips that are
        // decompressed on load (short SFX); streamed / compressed clips start at 0.
        private int AudibleStart(AudioClip clip)
        {
            if (startOffsets.TryGetValue(clip, out int cached))
                return cached;

            int start = 0;
            if (clip.loadType == AudioClipLoadType.DecompressOnLoad && clip.samples > 0)
            {
                int frames = Mathf.Min(clip.samples, clip.frequency / 2); // look at the first 0.5 s
                var data = new float[frames * clip.channels];
                if (clip.GetData(data, 0))
                {
                    for (int i = 0; i < data.Length; i++)
                    {
                        if (Mathf.Abs(data[i]) > silenceThreshold)
                        {
                            start = i / clip.channels;
                            break;
                        }
                    }
                }
                // Keep a few ms before the first sound so the attack is not cut.
                start = Mathf.Max(0, start - clip.frequency / 500);
            }
            startOffsets[clip] = start;
            return start;
        }

        private AudioSource FreeOneShot()
        {
            foreach (AudioSource s in oneShots)
                if (s != null && !s.isPlaying) return s;
            var go = new GameObject("OneShot");
            go.transform.SetParent(transform, false);
            AudioSource created = go.AddComponent<AudioSource>();
            created.playOnAwake = false;
            Setup3D(created);
            oneShots.Add(created);
            return created;
        }

        private void Setup3D(AudioSource s)
        {
            s.rolloffMode = AudioRolloffMode.Logarithmic;
            s.minDistance = minDistance;
            s.maxDistance = maxDistance;
            s.dopplerLevel = 0f;
        }
    }
}
