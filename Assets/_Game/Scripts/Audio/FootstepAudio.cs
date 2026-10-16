using CyberWatch.Player;
using UnityEngine;

namespace CyberWatch.Audio
{
    /// <summary>
    /// The player's own footsteps: the walk loop while walking, the run loop while sprinting,
    /// silent when standing still or sitting at the desk. Short fades between them so steps never
    /// cut off abruptly. Playback speed follows the actual walking speed a little.
    /// </summary>
    public class FootstepAudio : MonoBehaviour
    {
        [Tooltip("Found automatically in the scene when empty.")]
        [SerializeField] private PlayerMovement player;
        [SerializeField, Range(0f, 1f)] private float walkLevel = 1f;
        [SerializeField, Range(0f, 1f)] private float runLevel = 0.65f;
        [Tooltip("Fade time when starting/stopping or switching walk <-> run (seconds).")]
        [SerializeField, Min(0.01f)] private float fadeSeconds = 0.12f;
        [Tooltip("Playback speed range from slow to full walking speed.")]
        [SerializeField] private Vector2 walkPitch = new Vector2(0.9f, 1.05f);

        private AudioSource walk, run;
        private float walkShown, runShown;

        private void Start()
        {
            AudioLibrary clips = GameAudio.Clips;
            if (player == null) player = FindAnyObjectByType<PlayerMovement>();
            if (clips == null || player == null)
            {
                enabled = false;
                return;
            }
            walk = GameAudio.CreateLoop(clips.footstepWalk, AudioChannel.Sfx, null, 0f);
            run = GameAudio.CreateLoop(clips.footstepRun, AudioChannel.Sfx, null, 0f);
        }

        private void Update()
        {
            float speed = player.ControlEnabled ? player.HorizontalSpeed : 0f;
            bool moving = speed > 0.1f;
            bool running = moving && player.IsRunning;

            float step = Time.deltaTime / fadeSeconds;
            walkShown = Mathf.MoveTowards(walkShown, moving && !running ? 1f : 0f, step);
            runShown = Mathf.MoveTowards(runShown, running ? 1f : 0f, step);

            Drive(walk, walkShown * walkLevel);
            Drive(run, runShown * runLevel);

            if (walk != null && player.WalkSpeed > 0f)
                walk.pitch = Mathf.Lerp(walkPitch.x, walkPitch.y, Mathf.Clamp01(speed / player.WalkSpeed));
        }

        // Start the loop when it becomes audible, pause it when silent (resumes mid-stride).
        private static void Drive(AudioSource source, float level)
        {
            if (source == null)
                return;
            GameAudio.SetLevel(source, level);
            if (level > 0.001f && !source.isPlaying)
                source.Play();
            else if (level <= 0.001f && source.isPlaying)
                source.Pause();
        }
    }
}
