using System;
using System.Collections;
using System.Collections.Generic;
using CyberWatch.Audio;
using CyberWatch.Threat;
using UnityEngine;

namespace CyberWatch.Environment
{
    /// <summary>
    /// "That wasn't like that before": office objects change while the player is NOT looking at
    /// them (outside the camera view), from a given Threat stage. A chair has turned, the clock
    /// fell over... No sound, no movement on screen, no jumpscare. Colliders can follow along.
    /// When the Threat drops below the stage again, the object quietly goes back (also unseen).
    /// Objects are found by name at runtime, so the Office_Environment prefab is not modified.
    /// </summary>
    public class UnseenChanges : MonoBehaviour
    {
        [Serializable]
        private class Change
        {
            [Tooltip("Exact GameObject name inside the scene (e.g. Chair_B).")]
            public string objectName;
            [Tooltip("Stage from which the change happens.")]
            public ThreatStage fromStage = ThreatStage.Danger;
            [Tooltip("Rotation in world axes (degrees). Y = swivel (a chair turned), X or Z = tip over.")]
            public Vector3 worldEuler;
            [Tooltip("Extra rotation in the object's own axes (degrees), e.g. a crooked clock.")]
            public Vector3 localEuler;
            [Tooltip("Move in world space (meters), e.g. a chair pushed back.")]
            public Vector3 worldOffset;
            [Tooltip("After rotating, put the object back on the floor (its lowest point stays where it was). " +
                     "Use for things that fall over.")]
            public bool restOnFloor;
            [Tooltip("Collider object that must move with it (e.g. Col_Chair_A). Empty = none.")]
            public string colliderName;
            [Tooltip("Played together (layered) from the object when it changes. Empty = silent.")]
            public AudioClip[] sounds;
            [Range(0f, 1f)] public float soundVolume = 0.8f;
            [Tooltip("Each next layer starts this much later (seconds, random in range) for a natural mix.")]
            public Vector2 layerDelay = new Vector2(0.05f, 0.25f);

            [NonSerialized] public Transform target;
            [NonSerialized] public Transform collider;
            [NonSerialized] public Renderer[] renderers;
            [NonSerialized] public Vector3 homePosition;
            [NonSerialized] public Quaternion homeRotation;
            [NonSerialized] public Vector3 colliderHomePosition;
            [NonSerialized] public Quaternion colliderHomeRotation;
            [NonSerialized] public bool changed;
            [NonSerialized] public float stageReachedAt = -1f;
        }

        [Tooltip("Found automatically in the scene when empty.")]
        [SerializeField] private ThreatLevelSystem threat;
        [SerializeField] private List<Change> changes = new List<Change>();
        [Tooltip("How often visibility is checked (seconds).")]
        [SerializeField, Min(0.05f)] private float checkInterval = 0.25f;
        [Tooltip("Never change something closer than this to the camera, even when out of view.")]
        [SerializeField, Min(0f)] private float minDistance = 1.2f;
        [Tooltip("Wait this long after the stage is reached before changing (seconds), so the change and its " +
                 "sound do not get lost in the stage-change glitch and light sounds.")]
        [SerializeField, Min(0f)] private float delayAfterStage = 3f;

        private readonly Plane[] planes = new Plane[6];
        private float nextCheck;

        private void Awake()
        {
            if (threat == null) threat = FindAnyObjectByType<ThreatLevelSystem>();

            var byName = new Dictionary<string, Transform>();
            foreach (Transform t in FindObjectsByType<Transform>(FindObjectsInactive.Include))
                if (!byName.ContainsKey(t.name)) byName.Add(t.name, t);

            foreach (Change c in changes)
            {
                if (string.IsNullOrEmpty(c.objectName) || !byName.TryGetValue(c.objectName, out c.target))
                {
                    Debug.LogWarning($"[UnseenChanges] Objek '{c.objectName}' tidak ditemukan.", this);
                    continue;
                }
                c.renderers = c.target.GetComponentsInChildren<Renderer>();
                c.homePosition = c.target.position;
                c.homeRotation = c.target.rotation;

                if (!string.IsNullOrEmpty(c.colliderName))
                {
                    if (byName.TryGetValue(c.colliderName, out c.collider))
                    {
                        c.colliderHomePosition = c.collider.position;
                        c.colliderHomeRotation = c.collider.rotation;
                    }
                    else
                    {
                        Debug.LogWarning($"[UnseenChanges] Collider '{c.colliderName}' tidak ditemukan.", this);
                    }
                }
            }
        }

        private void Update()
        {
            if (threat == null || Time.time < nextCheck)
                return;
            nextCheck = Time.time + checkInterval;

            Camera cam = Camera.main;
            if (cam == null)
                return;
            GeometryUtility.CalculateFrustumPlanes(cam, planes);
            ThreatStage stage = threat.Stage;

            foreach (Change c in changes)
            {
                if (c.target == null)
                    continue;
                bool want = stage >= c.fromStage && stage < ThreatStage.Compromised;
                if (!want) c.stageReachedAt = -1f;
                else if (c.stageReachedAt < 0f) c.stageReachedAt = Time.time;
                if (want && !c.changed && Time.time - c.stageReachedAt < delayAfterStage)
                    continue; // let the stage change settle first

                if (want == c.changed || IsSeen(c, cam))
                    continue;

                c.changed = want;
                if (want) Apply(c);
                else Restore(c);
            }
        }

        private void Apply(Change c)
        {
            if (c.sounds != null && c.sounds.Length > 0)
                StartCoroutine(PlaySounds(c));

            float floorY = c.restOnFloor ? LowestPoint(c) : 0f;

            Quaternion rotation = Quaternion.Euler(c.worldEuler) * c.homeRotation * Quaternion.Euler(c.localEuler);
            c.target.SetPositionAndRotation(c.homePosition + c.worldOffset, rotation);

            // Fallen over: lift/lower so it lies on the floor instead of sinking into it.
            if (c.restOnFloor)
                c.target.position += Vector3.up * (floorY - LowestPoint(c));

            // The collider follows exactly the same move (rotated around the object's pivot).
            if (c.collider != null)
            {
                Quaternion delta = c.target.rotation * Quaternion.Inverse(c.homeRotation);
                c.collider.SetPositionAndRotation(
                    c.target.position + delta * (c.colliderHomePosition - c.homePosition),
                    delta * c.colliderHomeRotation);
            }
        }

        // All layers come from the object (3D), each with its own small delay and pitch.
        private IEnumerator PlaySounds(Change c)
        {
            Vector3 at = c.target.position;
            for (int i = 0; i < c.sounds.Length; i++)
            {
                if (i > 0)
                    yield return new WaitForSeconds(UnityEngine.Random.Range(c.layerDelay.x, c.layerDelay.y));
                GameAudio.PlayAt(c.sounds[i], at, AudioChannel.Sfx, c.soundVolume, UnityEngine.Random.Range(0.92f, 1.06f));
            }
        }

        private static void Restore(Change c)
        {
            c.target.SetPositionAndRotation(c.homePosition, c.homeRotation);
            if (c.collider != null)
                c.collider.SetPositionAndRotation(c.colliderHomePosition, c.colliderHomeRotation);
        }

        private static float LowestPoint(Change c)
        {
            float min = float.MaxValue;
            foreach (Renderer r in c.renderers)
                if (r != null) min = Mathf.Min(min, r.bounds.min.y);
            return min == float.MaxValue ? c.target.position.y : min;
        }

        // Seen = any of its renderers is inside the camera view, or it is right next to the camera.
        private bool IsSeen(Change c, Camera cam)
        {
            if (Vector3.Distance(cam.transform.position, c.target.position) < minDistance)
                return true;
            foreach (Renderer r in c.renderers)
                if (r != null && r.enabled && GeometryUtility.TestPlanesAABB(planes, r.bounds))
                    return true;
            return false;
        }
    }
}
