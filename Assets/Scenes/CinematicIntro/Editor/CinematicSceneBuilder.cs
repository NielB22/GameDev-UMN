using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CyberWatch.Cinematic.EditorTools
{
    /// <summary>
    /// Mengubah Cinematic_Intro_Scene yang SUDAH ADA menjadi sinematik bergaya film:
    /// shot baru (cold open, judul menghantam, masuk ke kantor, close-up monitor), fokus (depth of field),
    /// warna tiap shot, kabut, cue suara dan kedip lampu, overlay REC + jam + lokasi, kartu studio.
    ///
    /// TIDAK membuat scene dari nol: objek Audio, Global Volume (PP_Night), UI responsif, dan panel
    /// Cara Bermain buatan tim dipertahankan. Yang diganti: CinematicShots dan sosok pengintai.
    /// Menu: CyberWatch > Upgrade Cinematic to Film (v2).
    /// Letakkan file ini di Assets/Scenes/CinematicIntro/Editor/.
    /// </summary>
    public static class CinematicUpgradeV2
    {
        private const string ScenePath = "Assets/Scenes/CinematicIntro/Cinematic_Intro_Scene.unity";
        private const string SlenderPrefab = "Assets/Asset_3D/#NVJOB Slender/Slender/Prefab/Slender.prefab";
        private const string SlenderController = "Assets/Scenes/GameplayDesktop/Animation/Slender_Silhouette.controller";
        private const string FontTitle = "Assets/Asset_2D/Fonts/VT323/VT323-Regular SDF.asset";
        private const string FontBody = "Assets/Asset_2D/Fonts/IBM_Plex_Mono/IBMPlexMono-Regular SDF.asset";
        private const string FontBold = "Assets/Asset_2D/Fonts/IBM_Plex_Mono/IBMPlexMono-Bold SDF.asset";

        private const string Ghost = "@Ghost";
        private static readonly Vector3 GhostPosition = new Vector3(16f, 0.08f, 20.2f);
        private const float GhostYaw = -123f;

        // ---------- data cerita ----------

        private class PointDef
        {
            public Vector3 pos, target;
            public PointDef(float x, float y, float z, float tx, float ty, float tz)
            {
                pos = new Vector3(x, y, z);
                target = new Vector3(tx, ty, tz);
            }
        }

        private class CapDef
        {
            public float start, duration;
            public string text;
            public CapDef(float start, float duration, string text)
            {
                this.start = start;
                this.duration = duration;
                this.text = text;
            }
        }

        private class CueDef
        {
            public CueKind kind;
            public float time;
            public SoundId sound = SoundId.None;
            public float volume = 0.8f;
            public string soundAt;
            public bool outside, fromEnd;
            public string light;
            public float seconds = 1f;
            public string target;
            public float amount = 0.5f;
        }

        private class ShotDef
        {
            public string name, stamp;
            public float duration = 8f, fov = 60f, fovEnd, ease = 0.5f, handheld = 0.01f, fog;
            public float aperture = 2.4f, focal = 75f, rackAt = 0.5f, rackLength = 0.3f;
            public string focus, focusEnd;
            // exposure, contrast, saturation, temperature, bloom, vignette, chromatic, grain
            public float[] look = new float[8];
            public PointDef[] points;
            public CapDef[] captions;
            public CueDef[] cues;
        }

        // Efek pindah shot: statik pendek + desis kecil.
        private static CueDef Cut() => new CueDef
        {
            kind = CueKind.StaticHit, time = 0.02f, sound = SoundId.StaticShort,
            volume = 0.3f, amount = 0.35f, seconds = 0.18f
        };

        // Koordinat dunia: kantor di sekitar (0,0,0), jendela menghadap +Z, jalan di z ~ 17, gedung di z ~ 28.
        // Posisi prop (meja IT, monitor, lampu meja) diambil dari prefab Office_Environment.
        private static readonly ShotDef[] Shots =
        {
            // ===== COLD OPEN =====
            new ShotDef
            {
                name = "01_Kota", stamp = "EXT. JALAN RAYA - MALAM",
                duration = 9f, fov = 55f, fovEnd = 50f, ease = 0.35f, handheld = 0.015f, fog = 0.02f,
                look = new[] { 0f, 8f, -10f, -12f, 0.3f, 0.10f, 0.08f, 0.05f },
                points = new[]
                {
                    new PointDef(-26f, 2.2f, 14.8f, -10f, 5.0f, 28f),
                    new PointDef(-17f, 2.35f, 14.6f, -1f, 4.5f, 28f),
                    new PointDef(-8f, 2.3f, 14.2f, 8f, 3.2f, 27f),
                },
                captions = new[]
                {
                    new CapDef(0.8f, 4.0f, "Tengah malam. Seluruh kota sudah tertidur."),
                    new CapDef(5.2f, 3.4f, "Hampir semua lampu sudah padam."),
                },
                cues = new CueDef[0],
            },
            new ShotDef
            {
                name = "02_Kantor", stamp = "EXT. KANTOR IT SUPPORT",
                duration = 9f, fov = 50f, ease = 0.5f, handheld = 0.01f, fog = 0.02f,
                focus = "Window_Glass", aperture = 2.8f, focal = 60f,
                look = new[] { 0.1f, 10f, -8f, -8f, 0.5f, 0.12f, 0.10f, 0.05f },
                points = new[]
                {
                    new PointDef(11f, 8.5f, 17f, 0f, 1.2f, 4f),
                    new PointDef(7.5f, 5.0f, 13.5f, 0f, 1.6f, 3.6f),
                    new PointDef(3.5f, 2.4f, 9.5f, 0f, 1.8f, 3.1f),
                },
                captions = new[]
                {
                    new CapDef(0.6f, 4.2f, "Hanya satu jendela yang masih menyala."),
                    new CapDef(5.3f, 3.2f, "Kantor IT Support."),
                },
                cues = new[] { Cut() },
            },
            new ShotDef
            {
                name = "03_Pengintai", stamp = "EXT. LAMPU JALAN",
                duration = 9.5f, fov = 32f, fovEnd = 27f, ease = 0.2f, handheld = 0.008f, fog = 0.022f,
                focus = Ghost, aperture = 2.0f, focal = 85f,
                look = new[] { -0.1f, 14f, -22f, -18f, 0.3f, 0.20f, 0.15f, 0.08f },
                points = new[]
                {
                    new PointDef(3.0f, 1.3f, 13.2f, 15.8f, 1.3f, 20.2f),
                    new PointDef(3.9f, 1.3f, 13.7f, 15.8f, 1.3f, 20.2f),
                },
                captions = new[]
                {
                    new CapDef(0.8f, 3.8f, "Tapi bukan hanya kamu yang terjaga malam ini."),
                    new CapDef(5.2f, 3.6f, "Ia hanya berdiri dan menatap."),
                },
                cues = new[]
                {
                    Cut(),
                    // Lampu jalan berkedip, dan saat menyala lagi, sosok itu sudah berdiri di sana.
                    new CueDef { kind = CueKind.FlickerLight, time = 1.0f, light = "StreetLamp_FarRight_Light", seconds = 1.3f,
                                 sound = SoundId.LightFlicker, soundAt = "StreetLamp_FarRight_Light", outside = true, volume = 0.8f },
                    new CueDef { kind = CueKind.ShowObject, time = 2.5f, target = Ghost },
                    new CueDef { kind = CueKind.Sound, time = 2.8f, sound = SoundId.Whisper, soundAt = Ghost, outside = true, volume = 0.5f },
                    new CueDef { kind = CueKind.HideObject, time = 9.2f, target = Ghost },
                    // Riser yang selesai tepat saat layar menjadi hitam, lalu judul menghantam.
                    new CueDef { kind = CueKind.Sound, sound = SoundId.Riser, fromEnd = true, volume = 0.8f, seconds = 4f },
                },
            },

            // ===== (JUDUL menghantam di sini) =====

            new ShotDef
            {
                name = "04_Masuk", stamp = "EXT./INT. JENDELA",
                duration = 10f, fov = 58f, ease = 0.55f, handheld = 0.012f, fog = 0.012f,
                focus = "Window_Glass", focusEnd = "PC_Monitor_Main", rackAt = 0.45f, rackLength = 0.3f, aperture = 2.4f, focal = 60f,
                look = new[] { 0f, 8f, -6f, 4f, 0.4f, 0.12f, 0.08f, 0.05f },
                points = new[]
                {
                    new PointDef(1.9f, 2.1f, 6.8f, 0.6f, 1.5f, 2.6f),
                    new PointDef(1.7f, 1.95f, 3.9f, 0.3f, 1.4f, 2.5f),
                    new PointDef(1.2f, 1.85f, 2.3f, 0.0f, 1.25f, 2.58f),
                    new PointDef(0.5f, 1.55f, 1.5f, 0.0f, 1.2f, 2.58f),
                },
                captions = new[]
                {
                    new CapDef(0.8f, 4.4f, "Tugasmu sederhana: pantau setiap pesan yang masuk."),
                },
                cues = new[]
                {
                    Cut(),
                    new CueDef { kind = CueKind.FlickerLight, time = 6.2f, light = "CeilingLight_Working", seconds = 1.1f,
                                 sound = SoundId.LightFlicker, soundAt = "CeilingLight_Working", volume = 0.6f },
                },
            },
            new ShotDef
            {
                name = "05_Monitor", stamp = "INT. MEJA IT",
                duration = 7f, fov = 40f, fovEnd = 34f, ease = 0.5f, handheld = 0.008f, fog = 0f,
                focus = "PC_Monitor_Main", aperture = 1.6f, focal = 70f,
                look = new[] { 0.1f, 12f, -4f, 10f, 0.6f, 0.20f, 0.12f, 0.05f },
                points = new[]
                {
                    new PointDef(0.55f, 1.35f, 1.4f, 0.0f, 1.15f, 2.58f),
                    new PointDef(0.2f, 1.22f, 1.85f, 0.0f, 1.15f, 2.58f),
                },
                captions = new[]
                {
                    new CapDef(1.6f, 3.0f, "Satu pesan masuk."),
                    new CapDef(4.4f, 2.4f, "Asli, atau jebakan?"),
                },
                cues = new[]
                {
                    Cut(),
                    new CueDef { kind = CueKind.Sound, time = 1.7f, sound = SoundId.MailNotify, volume = 0.8f },
                    new CueDef { kind = CueKind.StaticHit, time = 1.72f, seconds = 0.12f, amount = 0.3f },
                    new CueDef { kind = CueKind.FlickerLight, time = 4.6f, light = "DeskLamp_Main_Light", seconds = 0.9f,
                                 sound = SoundId.LightFlicker, soundAt = "DeskLamp_Main_Light", volume = 0.6f },
                },
            },
            new ShotDef
            {
                name = "06_Jendela", stamp = "INT. KANTOR - 00:00",
                duration = 8.5f, fov = 60f, ease = 0.55f, handheld = 0.012f, fog = 0f,
                focus = "PC_Monitor_Main", focusEnd = Ghost, rackAt = 0.35f, rackLength = 0.3f, aperture = 2.6f, focal = 60f,
                look = new[] { 0f, 12f, -14f, -6f, 0.3f, 0.25f, 0.14f, 0.08f },
                points = new[]
                {
                    new PointDef(0.0f, 1.45f, 0.3f, 0.0f, 1.15f, 2.58f),
                    new PointDef(0.0f, 1.33f, 1.0f, 0.0f, 0.9f, 4.0f),
                    new PointDef(0.0f, 1.25f, 1.55f, 0.0f, 0.41f, 7.55f),
                },
                captions = new[]
                {
                    new CapDef(1.0f, 3.6f, "Satu keputusan salah, dan ia melangkah lebih dekat."),
                    new CapDef(5.0f, 3.4f, "Bertahan sampai pukul 06:00."),
                },
                cues = new[]
                {
                    Cut(),
                    new CueDef { kind = CueKind.ShowObject, time = 1.0f, target = Ghost },
                    new CueDef { kind = CueKind.Sound, time = 1.2f, sound = SoundId.Whisper, soundAt = Ghost, outside = true, volume = 0.35f },
                },
            },
        };

        // Judul muncul setelah shot ke-3 (indeks 2).
        private const int TitleAfterShot = 2;

        // ---------- menu ----------

        [MenuItem("CyberWatch/Upgrade Cinematic to Film (v2)")]
        public static void Upgrade()
        {
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog("Cinematic v2", "Hentikan Play dulu, lalu jalankan lagi.", "OK");
                return;
            }

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
            {
                EditorUtility.DisplayDialog("Cinematic v2", "Scene tidak ditemukan:\n" + ScenePath, "OK");
                return;
            }

            if (!EditorUtility.DisplayDialog("Cinematic v2",
                    "Scene sinematik akan diperbarui (shot, cue, overlay, kartu studio).\n" +
                    "Objek Audio, Global Volume, dan panel Cara Bermain dipertahankan.\n\n" +
                    "Pastikan perubahan terakhirmu sudah di-commit dulu (Git = cadangan). Lanjut?",
                    "Lanjut", "Batal"))
                return;

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            CinematicIntro director = Object.FindAnyObjectByType<CinematicIntro>();
            if (director == null)
            {
                EditorUtility.DisplayDialog("Cinematic v2", "CinematicIntro tidak ditemukan di scene.", "OK");
                return;
            }

            Transform canvas = FindIn(scene, "Canvas_Cinematic");
            if (canvas == null)
            {
                EditorUtility.DisplayDialog("Cinematic v2", "Canvas_Cinematic tidak ditemukan di scene.", "OK");
                return;
            }

            // ----- bersihkan bagian lama -----
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == "CinematicShots" || root.name == "Reveal_Watcher" || root.name.StartsWith("Slender"))
                    Object.DestroyImmediate(root);
            }

            GameObject ghost = SpawnGhost();

            Dictionary<string, Transform> map = MapScene(scene);
            if (ghost != null)
                map[Ghost] = ghost.transform;

            // ----- titik kamera -----
            var shotsRoot = new GameObject("CinematicShots");
            var shotPoints = new List<Transform[]>();
            foreach (ShotDef def in Shots)
            {
                var shotGo = new GameObject("Shot_" + def.name);
                shotGo.transform.SetParent(shotsRoot.transform, false);

                var arr = new Transform[def.points.Length];
                for (int i = 0; i < def.points.Length; i++)
                {
                    var pg = new GameObject("P" + i);
                    pg.transform.SetParent(shotGo.transform, false);
                    pg.transform.position = def.points[i].pos;
                    pg.transform.rotation = Quaternion.LookRotation(def.points[i].target - def.points[i].pos);
                    arr[i] = pg.transform;
                }
                shotPoints.Add(arr);
            }

            // ----- UI baru -----
            TMP_FontAsset fontTitle = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontTitle);
            TMP_FontAsset fontBody = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontBody);
            TMP_FontAsset fontBold = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontBold);

            foreach (string n in new[] { "StaticOverlay", "StudioGroup", "OverlayGroup" })
            {
                Transform old = canvas.Find(n);
                if (old != null)
                    Object.DestroyImmediate(old.gameObject);
            }

            // statik (efek pindah shot) - paling belakang di antara UI
            RectTransform staticRt = NewRect("StaticOverlay", canvas);
            Stretch(staticRt);
            RawImage staticImage = staticRt.gameObject.AddComponent<RawImage>();
            staticImage.color = new Color(1f, 1f, 1f, 0f);
            staticImage.raycastTarget = false;
            staticRt.SetSiblingIndex(0);

            // overlay gaya rekaman (di dalam bar hitam atas)
            RectTransform overlayRt = NewRect("OverlayGroup", canvas);
            Stretch(overlayRt);
            CanvasGroup overlayGroup = overlayRt.gameObject.AddComponent<CanvasGroup>();
            overlayGroup.alpha = 0f;
            overlayGroup.blocksRaycasts = false;
            overlayGroup.interactable = false;

            RectTransform recRt = NewRect("RecLabel", overlayRt);
            recRt.anchorMin = recRt.anchorMax = new Vector2(0f, 1f);
            recRt.pivot = new Vector2(0f, 1f);
            recRt.anchoredPosition = new Vector2(60f, -26f);
            recRt.sizeDelta = new Vector2(400f, 44f);
            TextMeshProUGUI rec = AddText(recRt, "[REC]", fontBold, 32f, new Color(0.9f, 0.15f, 0.15f), TextAlignmentOptions.Left);

            RectTransform stampRt = NewRect("StampText", overlayRt);
            stampRt.anchorMin = new Vector2(0f, 1f);
            stampRt.anchorMax = new Vector2(0.7f, 1f);
            stampRt.pivot = new Vector2(0f, 1f);
            stampRt.anchoredPosition = new Vector2(60f, -72f);
            stampRt.sizeDelta = new Vector2(-60f, 40f);
            TextMeshProUGUI stamp = AddText(stampRt, "", fontBody, 28f, new Color(0.78f, 0.82f, 0.86f), TextAlignmentOptions.Left);
            AutoSize(stamp, 16f);

            RectTransform timeRt = NewRect("TimecodeText", overlayRt);
            timeRt.anchorMin = timeRt.anchorMax = new Vector2(1f, 1f);
            timeRt.pivot = new Vector2(1f, 1f);
            timeRt.anchoredPosition = new Vector2(-60f, -26f);
            timeRt.sizeDelta = new Vector2(420f, 44f);
            TextMeshProUGUI timecode = AddText(timeRt, "23:58:00", fontBold, 34f, new Color(0.92f, 0.94f, 0.96f), TextAlignmentOptions.Right);

            overlayRt.SetSiblingIndex(3); // di atas bar letterbox (indeks 1 dan 2)

            // kartu studio - paling depan
            RectTransform studioRt = NewRect("StudioGroup", canvas);
            Stretch(studioRt);
            CanvasGroup studioGroup = studioRt.gameObject.AddComponent<CanvasGroup>();
            studioGroup.alpha = 0f;
            studioGroup.blocksRaycasts = false;
            studioGroup.interactable = false;

            RectTransform studioNameRt = NewRect("StudioLabel", studioRt);
            studioNameRt.anchorMin = new Vector2(0.1f, 0.5f);
            studioNameRt.anchorMax = new Vector2(0.9f, 0.5f);
            studioNameRt.anchoredPosition = new Vector2(0f, 20f);
            studioNameRt.sizeDelta = new Vector2(0f, 160f);
            AutoSize(AddText(studioNameRt, "DaNaBaRe", fontTitle, 130f, new Color(0.93f, 0.95f, 0.97f), TextAlignmentOptions.Center), 60f);

            RectTransform studioSubRt = NewRect("StudioSub", studioRt);
            studioSubRt.anchorMin = new Vector2(0.1f, 0.5f);
            studioSubRt.anchorMax = new Vector2(0.9f, 0.5f);
            studioSubRt.anchoredPosition = new Vector2(0f, -70f);
            studioSubRt.sizeDelta = new Vector2(0f, 60f);
            TextMeshProUGUI studioSub = AddText(studioSubRt, "presents", fontBody, 34f, new Color(0.6f, 0.64f, 0.7f), TextAlignmentOptions.Center);
            studioSub.characterSpacing = 14f;
            AutoSize(studioSub, 18f);

            studioRt.SetAsLastSibling();

            // ----- hubungkan ke CinematicIntro -----
            var so = new SerializedObject(director);

            so.FindProperty("overlayGroup").objectReferenceValue = overlayGroup;
            so.FindProperty("recLabel").objectReferenceValue = rec;
            so.FindProperty("timecodeText").objectReferenceValue = timecode;
            so.FindProperty("stampText").objectReferenceValue = stamp;
            so.FindProperty("studioGroup").objectReferenceValue = studioGroup;
            so.FindProperty("staticOverlay").objectReferenceValue = staticImage;

            Transform titleLabel = canvas.Find("TitleGroup/TitleLabel");
            Transform subtitleLabel = canvas.Find("TitleGroup/SubtitleLabel");
            so.FindProperty("titleLabel").objectReferenceValue = titleLabel != null ? titleLabel.GetComponent<TMP_Text>() : null;
            so.FindProperty("subtitleLabel").objectReferenceValue = subtitleLabel != null ? subtitleLabel.GetComponent<TMP_Text>() : null;

            so.FindProperty("titleAfterShot").intValue = TitleAfterShot;
            so.FindProperty("titleSeconds").floatValue = 2.2f;
            so.FindProperty("studioSeconds").floatValue = 2.2f;
            so.FindProperty("clockRate").floatValue = 4f;
            so.FindProperty("cutFadeSeconds").floatValue = 0.45f;
            so.FindProperty("titleSound").enumValueIndex = (int)SoundId.PowerDown;
            so.FindProperty("titleSoundVolume").floatValue = 0.9f;
            so.FindProperty("vignetteIntensity").floatValue = 0.3f;
            so.FindProperty("grainIntensity").floatValue = 0.28f;
            so.FindProperty("depthOfField").boolValue = true;
            so.FindProperty("lookBlendSpeed").floatValue = 3f;
            so.FindProperty("fogColor").colorValue = new Color(0.02f, 0.03f, 0.05f);

            SerializedProperty shotsProp = so.FindProperty("shots");
            shotsProp.arraySize = Shots.Length;
            for (int i = 0; i < Shots.Length; i++)
                WriteShot(shotsProp.GetArrayElementAtIndex(i), Shots[i], shotPoints[i], map);

            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            Selection.activeGameObject = director.gameObject;
            Debug.Log("[CinematicUpgradeV2] Selesai. Tekan Play di scene ini untuk melihat hasilnya.");
        }

        // ---------- menulis data shot ----------

        private static void WriteShot(SerializedProperty el, ShotDef d, Transform[] points, Dictionary<string, Transform> map)
        {
            el.FindPropertyRelative("name").stringValue = d.name;
            el.FindPropertyRelative("stamp").stringValue = d.stamp;
            el.FindPropertyRelative("duration").floatValue = d.duration;
            el.FindPropertyRelative("fov").floatValue = d.fov;
            el.FindPropertyRelative("fovEnd").floatValue = d.fovEnd;
            el.FindPropertyRelative("ease").floatValue = d.ease;
            el.FindPropertyRelative("handheld").floatValue = d.handheld;
            el.FindPropertyRelative("fog").floatValue = d.fog;
            el.FindPropertyRelative("rackAt").floatValue = d.rackAt;
            el.FindPropertyRelative("rackLength").floatValue = d.rackLength;
            el.FindPropertyRelative("dofAperture").floatValue = d.aperture;
            el.FindPropertyRelative("dofFocalLength").floatValue = d.focal;
            el.FindPropertyRelative("lookAt").objectReferenceValue = null;
            el.FindPropertyRelative("reveal").objectReferenceValue = null;
            el.FindPropertyRelative("focusTarget").objectReferenceValue = Get(map, d.focus);
            el.FindPropertyRelative("focusTargetEnd").objectReferenceValue = Get(map, d.focusEnd);

            SerializedProperty pts = el.FindPropertyRelative("points");
            pts.arraySize = points.Length;
            for (int j = 0; j < points.Length; j++)
                pts.GetArrayElementAtIndex(j).objectReferenceValue = points[j];

            SerializedProperty lk = el.FindPropertyRelative("look");
            string[] lookNames = { "exposure", "contrast", "saturation", "temperature", "bloom", "vignette", "chromatic", "grain" };
            for (int j = 0; j < lookNames.Length; j++)
                lk.FindPropertyRelative(lookNames[j]).floatValue = d.look[j];

            SerializedProperty caps = el.FindPropertyRelative("captions");
            caps.arraySize = d.captions.Length;
            for (int j = 0; j < d.captions.Length; j++)
            {
                SerializedProperty cp = caps.GetArrayElementAtIndex(j);
                cp.FindPropertyRelative("text").stringValue = d.captions[j].text;
                cp.FindPropertyRelative("start").floatValue = d.captions[j].start;
                cp.FindPropertyRelative("duration").floatValue = d.captions[j].duration;
            }

            SerializedProperty cues = el.FindPropertyRelative("cues");
            cues.arraySize = d.cues.Length;
            for (int j = 0; j < d.cues.Length; j++)
            {
                CueDef c = d.cues[j];
                SerializedProperty cp = cues.GetArrayElementAtIndex(j);
                cp.FindPropertyRelative("kind").enumValueIndex = (int)c.kind;
                cp.FindPropertyRelative("time").floatValue = c.time;
                cp.FindPropertyRelative("sound").enumValueIndex = (int)c.sound;
                cp.FindPropertyRelative("volume").floatValue = c.volume;
                cp.FindPropertyRelative("soundAt").objectReferenceValue = Get(map, c.soundAt);
                cp.FindPropertyRelative("outside").boolValue = c.outside;
                cp.FindPropertyRelative("fromEnd").boolValue = c.fromEnd;
                cp.FindPropertyRelative("seconds").floatValue = c.seconds;
                cp.FindPropertyRelative("amount").floatValue = c.amount;

                Transform lightTf = Get(map, c.light);
                Light light = null;
                if (lightTf != null)
                {
                    light = lightTf.GetComponent<Light>();
                    if (light == null)
                        light = lightTf.GetComponentInChildren<Light>(true);
                }
                cp.FindPropertyRelative("light").objectReferenceValue = light;

                Transform targetTf = Get(map, c.target);
                cp.FindPropertyRelative("target").objectReferenceValue = targetTf != null ? targetTf.gameObject : null;
            }
        }

        // ---------- bantuan ----------

        private static Transform Get(Dictionary<string, Transform> map, string name)
        {
            if (string.IsNullOrEmpty(name))
                return null;
            if (map.TryGetValue(name, out Transform t))
                return t;

            Debug.LogWarning("[CinematicUpgradeV2] Objek tidak ditemukan di scene: " + name);
            return null;
        }

        private static Dictionary<string, Transform> MapScene(Scene scene)
        {
            var map = new Dictionary<string, Transform>();
            foreach (Transform t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))
            {
                if (t.gameObject.scene == scene && !map.ContainsKey(t.name))
                    map[t.name] = t;
            }
            return map;
        }

        private static Transform FindIn(Scene scene, string name)
        {
            foreach (Transform t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))
                if (t.gameObject.scene == scene && t.name == name)
                    return t;
            return null;
        }

        private static GameObject SpawnGhost()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SlenderPrefab);
            if (prefab == null)
            {
                Debug.LogWarning("[CinematicUpgradeV2] Prefab Slender tidak ditemukan: " + SlenderPrefab);
                return null;
            }

            var ghost = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            ghost.name = "Reveal_Watcher";
            ghost.transform.SetPositionAndRotation(GhostPosition, Quaternion.Euler(0f, GhostYaw, 0f));
            ghost.transform.localScale = Vector3.one * 0.8f;

            Animator animator = ghost.GetComponentInChildren<Animator>(true);
            if (animator != null)
            {
                var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(SlenderController);
                if (controller != null)
                    animator.runtimeAnimatorController = controller;
                animator.applyRootMotion = false;
                animator.speed = 0.4f;
            }

            ghost.SetActive(false);
            return ghost;
        }

        private static RectTransform NewRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go.GetComponent<RectTransform>();
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static TextMeshProUGUI AutoSize(TextMeshProUGUI text, float min)
        {
            text.enableAutoSizing = true;
            text.fontSizeMax = text.fontSize;
            text.fontSizeMin = min;
            return text;
        }

        private static TextMeshProUGUI AddText(RectTransform rt, string text, TMP_FontAsset font, float size, Color color, TextAlignmentOptions align)
        {
            TextMeshProUGUI t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            t.text = text;
            if (font != null)
                t.font = font;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.raycastTarget = false;
            return t;
        }
    }
}