using UnityEngine;

namespace CyberWatch.Audio
{
    /// <summary>
    /// Every sound of the game in one asset (_Game/Data/Audio/AudioLibrary), so scripts ask
    /// GameAudio.Clips for a sound instead of each holding their own clip references.
    /// </summary>
    [CreateAssetMenu(fileName = "AudioLibrary", menuName = "Cyber Watch/Audio Library")]
    public class AudioLibrary : ScriptableObject
    {
        [Header("Ambience (loops)")]
        public AudioClip officeRoomTone;
        public AudioClip fluorescentBuzz;
        public AudioClip horrorDrone;
        public AudioClip heartbeatLoop;

        [Header("Events")]
        public AudioClip lightFlicker;
        public AudioClip staticShort;
        public AudioClip crtOff;
        public AudioClip powerDown;
        public AudioClip blackoutRiser;

        [Header("Interaction")]
        public AudioClip flashlightClick;
        public AudioClip flashlightShake;
        public AudioClip mailNotify;
        public AudioClip uiClick;
        public AudioClip footstepWalk;
        public AudioClip footstepRun;

        [Header("Jumpscare layers")]
        public AudioClip jumpscareRiser;
        public AudioClip slenderVoice;
        public AudioClip jumpscareScream;
        public AudioClip jumpscareStatic;
        public AudioClip jumpscareImpact;
    }
}
