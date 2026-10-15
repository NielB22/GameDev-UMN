using System.Collections.Generic;
using System.Linq;
using CyberWatch.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace CyberWatch.Cinematic.EditorTools
{
    /// <summary>
    /// Membangun Cinematic_Intro_Scene otomatis: kantor + dunia luar, titik-titik kamera tiap shot,
    /// sosok pengintai, seluruh UI (judul, subtitle, panel cara bermain), dan menghubungkan semuanya
    /// ke CinematicIntro. Menu: CyberWatch > Build Cinematic Scene.
    /// Aman dijalankan ulang (scene ditimpa) — tapi jangan jalankan kalau sudah mengubah scene-nya manual.
    /// Letakkan file ini di folder bernama "Editor" (Assets/Scenes/CinematicIntro/Editor/).
    /// </summary>
    public static class CinematicSceneBuilder
    {
        // ---------- path aset ----------
        private const string SceneFolder = "Assets/Scenes/CinematicIntro";
        private const string ScenePath = SceneFolder + "/Cinematic_Intro_Scene.unity";
        private const string LightingPrefab = "Assets/_Game/Prefabs/Environment/Lighting_Night.prefab";
        private const string OfficePrefab = "Assets/_Game/Prefabs/Environment/Office_Environment.prefab";
        private const string VolumeProfilePath = "Assets/Settings/SampleSceneProfile.asset";
        private const string SlenderPrefab = "Assets/Asset_3D/#NVJOB Slender/Slender/Prefab/Slender.prefab";
        private const string SlenderController = "Assets/Scenes/GameplayDesktop/Animation/Slender_Silhouette.controller";
        private const string FontTitle = "Assets/Asset_2D/Fonts/VT323/VT323-Regular SDF.asset";
        private const string FontBody = "Assets/Asset_2D/Fonts/IBM_Plex_Mono/IBMPlexMono-Regular SDF.asset";
        private const string FontBold = "Assets/Asset_2D/Fonts/IBM_Plex_Mono/IBMPlexMono-Bold SDF.asset";

        // ---------- data cerita ----------

        private class P
        {
            public Vector3 pos, target;
            public P(float x, float y, float z, float tx, float ty, float tz)
            {
                pos = new Vector3(x, y, z);
                target = new Vector3(tx, ty, tz);
            }
        }

        private class C
        {
            public float start, duration;
            public string text;
            public C(float start, float duration, string text)
            {
                this.start = start;
                this.duration = duration;
                this.text = text;
            }
        }

        private class S
        {
            public string name;
            public float duration, fov, ease, handheld;
            public bool reveal;
            public P[] points;
            public C[] captions;
        }

        // Koordinat dunia (kantor di sekitar 0,0,0; jendela menghadap +Z; jalan di z ~ 17; gedung di z ~ 28).
        private static readonly S[] ShotDefs =
        {
            // 1. Kota yang tertidur: meluncur pelan menyusuri jalan.
            new S
            {
                name = "01_Kota_Tertidur", duration = 11f, fov = 55f, ease = 0.35f, handheld = 0.015f,
                points = new[]
                {
                    new P(-27f, 2.2f, 14.8f, -10f, 5.0f, 28f),
                    new P(-18f, 2.35f, 14.6f, -1f, 4.5f, 28f),
                    new P(-9f, 2.3f, 14.2f, 8f, 3.2f, 27f),
                },
                captions = new[]
                {
                    new C(0.8f, 4.2f, "00:00. Seluruh kota sudah tertidur."),
                    new C(5.6f, 4.8f, "Di sudut jalan ini, satu kantor kecil masih terjaga."),
                }
            },
            // 2. Kantor dari luar: mendekati satu-satunya jendela yang menyala.
            new S
            {
                name = "02_Kantor", duration = 11f, fov = 48f, ease = 0.5f, handheld = 0.01f,
                points = new[]
                {
                    new P(-10f, 0.9f, 11.5f, 0f, 1.7f, 3.1f),
                    new P(-5.5f, 1.3f, 9.6f, 0f, 1.75f, 3.1f),
                    new P(-2.2f, 1.9f, 7.4f, 0f, 1.8f, 3.1f),
                },
                captions = new[]
                {
                    new C(0.8f, 4.6f, "Kantor IT Support. Satu jendela, satu komputer."),
                    new C(6.0f, 4.2f, "Satu penjaga malam. Kamu."),
                }
            },
            // 3. Sosok di bawah lampu jalan, jauh, diam.
            new S
            {
                name = "03_Pengintai", duration = 8.5f, fov = 38f, ease = 0.2f, handheld = 0.008f, reveal = true,
                points = new[]
                {
                    new P(4.5f, 1.7f, 12.8f, 15.5f, 2.0f, 20.4f),
                    new P(5.4f, 1.7f, 13.3f, 15.5f, 1.9f, 20.2f),
                },
                captions = new[]
                {
                    new C(0.6f, 3.6f, "Tapi bukan hanya kamu yang terjaga malam ini."),
                    new C(4.4f, 3.8f, "Ia hanya berdiri dan menatap."),
                }
            },
            // 4. Terbang masuk lewat jendela, berhenti di meja kerja (sudut pandang yang sama dengan mode duduk).
            new S
            {
                name = "04_Masuk", duration = 15f, fov = 60f, ease = 0.55f, handheld = 0.012f,
                points = new[]
                {
                    new P(1.8f, 2.2f, 5.2f, 1.0f, 1.6f, 0.5f),
                    new P(1.9f, 2.0f, 2.7f, 0.0f, 1.4f, -0.8f),
                    new P(1.0f, 1.75f, 0.6f, -0.2f, 1.3f, 2.7f),
                    new P(0.0f, 1.25f, 1.55f, 0.0f, 0.41f, 7.55f),
                },
                captions = new[]
                {
                    new C(0.6f, 4.2f, "Setiap pesan yang masuk bisa jadi jebakan."),
                    new C(5.4f, 4.4f, "Satu keputusan salah, dan ia melangkah lebih dekat."),
                    new C(10.6f, 4.0f, "Bertahan sampai pukul 06:00."),
                }
            },
        };

        // Sosok pengintai: berdiri di bawah lampu jalan sisi kanan (tampil hanya pada shot 3).
        private static readonly Vector3 RevealPosition = new Vector3(16f, 0.08f, 20.2f);
        private const float RevealYaw = -123f;

        private static readonly string[][] TutorialPages =
        {
            new[]
            {
                "MISI MALAM INI",
                "Kamu staf IT shift malam. Dari pukul <b>00:00 sampai 06:00</b>, pantau inbox dan jaga akun perusahaan.\n\n" +
                "Setiap pesan yang masuk: pilih <b>TERIMA</b> kalau asli, atau <b>BLOKIR</b> kalau penipuan.\n\n" +
                "Periksa 4 hal sebelum memutuskan:\n\n" +
                "- <b>Pengirim</b>: ejaan domain yang janggal (paypaI.com bukan paypal.com)\n" +
                "- <b>Bahasa</b>: nada mendesak atau mengancam ('akun diblokir dalam 5 menit!')\n" +
                "- <b>Kode</b>: bank asli tidak pernah meminta OTP atau PIN\n" +
                "- <b>Tautan</b>: arahkan kursor ke link untuk melihat tujuan sebenarnya"
            },
            new[]
            {
                "BERTAHAN SAMPAI PAGI",
                "<b>Threat Level</b> naik setiap kali kamu salah memutuskan. Makin tinggi, kantor makin gelap dan sosok di luar jendela makin dekat. Keputusan yang benar menurunkannya.\n\n" +
                "<b>Menang</b>: bertahan sampai 06:00 dengan Threat di bawah 100%.\n" +
                "<b>Kalah</b>: Threat mencapai 100% (System Compromised).\n\n" +
                "<b>KONTROL</b>\n" +
                "WASD jalan   |   Shift lari   |   Mouse menoleh\n" +
                "E duduk di meja IT (E atau Esc untuk berdiri)\n" +
                "V ganti kamera FPP/TPP   |   F senter\n" +
                "Senter mati? Goyangkan mouse untuk menyalakannya lagi."
            },
        };

        // ---------- menu ----------

        [MenuItem("CyberWatch/Build Cinematic Scene")]
        public static void Build()
        {
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog("Cinematic", "Hentikan Play dulu, lalu jalankan lagi.", "OK");
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null &&
                !EditorUtility.DisplayDialog("Cinematic",
                    "Cinematic_Intro_Scene sudah ada. Timpa dengan versi baru?\n(Perubahan manual di scene itu akan hilang.)",
                    "Timpa", "Batal"))
                return;

            if (!AssetDatabase.IsValidFolder(SceneFolder))
                AssetDatabase.CreateFolder("Assets/Scenes", "CinematicIntro");

            TMP_FontAsset fontTitle = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontTitle);
            TMP_FontAsset fontBody = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontBody);
            TMP_FontAsset fontBold = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontBold);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // ----- dunia -----
            Spawn(LightingPrefab);
            Spawn(OfficePrefab);

            var volumeGo = new GameObject("Global Volume");
            Volume volume = volumeGo.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.sharedProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(VolumeProfilePath);
            if (volume.sharedProfile == null)
                Debug.LogWarning("[CinematicBuilder] Profile post-processing tidak ditemukan: " + VolumeProfilePath);

            // ----- kamera -----
            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            Camera cam = camGo.AddComponent<Camera>();
            cam.fieldOfView = 55f;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 1000f;
            camGo.AddComponent<AudioListener>();
            camGo.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing = true;

            // ----- titik kamera tiap shot -----
            var shotsRoot = new GameObject("CinematicShots");
            var shotPoints = new List<Transform[]>();
            foreach (S def in ShotDefs)
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

            // ----- sosok pengintai -----
            GameObject reveal = SpawnWatcher();

            // ----- UI -----
            var canvasGo = new GameObject("Canvas_Cinematic", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<EnsureEventSystem>();
            Transform canvasTf = canvasGo.transform;

            // letterbox (bar sinema)
            RectTransform barTop = NewRect("LetterboxTop", canvasTf);
            barTop.anchorMin = new Vector2(0f, 1f);
            barTop.anchorMax = new Vector2(1f, 1f);
            barTop.pivot = new Vector2(0.5f, 1f);
            barTop.anchoredPosition = Vector2.zero;
            barTop.sizeDelta = Vector2.zero;
            AddImage(barTop, Color.black, false);

            RectTransform barBottom = NewRect("LetterboxBottom", canvasTf);
            barBottom.anchorMin = new Vector2(0f, 0f);
            barBottom.anchorMax = new Vector2(1f, 0f);
            barBottom.pivot = new Vector2(0.5f, 0f);
            barBottom.anchoredPosition = Vector2.zero;
            barBottom.sizeDelta = Vector2.zero;
            AddImage(barBottom, Color.black, false);

            // subtitle (di dalam bar bawah)
            RectTransform captionRt = NewRect("CaptionText", canvasTf);
            captionRt.anchorMin = captionRt.anchorMax = new Vector2(0.5f, 0f);
            captionRt.anchoredPosition = new Vector2(0f, 65f);
            captionRt.sizeDelta = new Vector2(1500f, 110f);
            TextMeshProUGUI caption = AddText(captionRt, "", fontBody, 38f, new Color(0.92f, 0.94f, 0.96f), TextAlignmentOptions.Center);
            caption.alpha = 0f;

            // panel cara bermain
            RectTransform panel = NewRect("TutorialPanel", canvasTf);
            panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 0.5f);
            panel.anchoredPosition = Vector2.zero;
            panel.sizeDelta = new Vector2(1300f, 840f);
            AddImage(panel, new Color(0.03f, 0.04f, 0.06f, 0.93f), true);

            RectTransform tTitleRt = NewRect("TutorialTitle", panel);
            tTitleRt.anchorMin = tTitleRt.anchorMax = new Vector2(0.5f, 1f);
            tTitleRt.pivot = new Vector2(0.5f, 1f);
            tTitleRt.anchoredPosition = new Vector2(0f, -40f);
            tTitleRt.sizeDelta = new Vector2(1100f, 110f);
            TextMeshProUGUI tTitle = AddText(tTitleRt, "MISI MALAM INI", fontTitle, 90f, new Color(0.93f, 0.95f, 0.97f), TextAlignmentOptions.Center);

            RectTransform tBodyRt = NewRect("TutorialBody", panel);
            tBodyRt.anchorMin = Vector2.zero;
            tBodyRt.anchorMax = Vector2.one;
            tBodyRt.offsetMin = new Vector2(90f, 170f);
            tBodyRt.offsetMax = new Vector2(-90f, -170f);
            TextMeshProUGUI tBody = AddText(tBodyRt, "", fontBody, 30f, new Color(0.85f, 0.88f, 0.92f), TextAlignmentOptions.TopLeft);

            RectTransform buttonRt = NewRect("NextButton", panel);
            buttonRt.anchorMin = buttonRt.anchorMax = new Vector2(0.5f, 0f);
            buttonRt.pivot = new Vector2(0.5f, 0f);
            buttonRt.anchoredPosition = new Vector2(0f, 50f);
            buttonRt.sizeDelta = new Vector2(380f, 84f);
            Image buttonImage = AddImage(buttonRt, new Color(0.55f, 0.1f, 0.1f, 1f), true);
            Button button = buttonRt.gameObject.AddComponent<Button>();
            button.targetGraphic = buttonImage;
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.25f, 1.1f, 1.1f, 1f);
            colors.pressedColor = new Color(0.75f, 0.75f, 0.75f, 1f);
            button.colors = colors;

            RectTransform buttonLabelRt = NewRect("Label", buttonRt);
            Stretch(buttonLabelRt);
            TextMeshProUGUI buttonLabel = AddText(buttonLabelRt, "LANJUT", fontBold, 34f, Color.white, TextAlignmentOptions.Center);

            // petunjuk lewati
            RectTransform skipRt = NewRect("SkipHint", canvasTf);
            skipRt.anchorMin = skipRt.anchorMax = new Vector2(1f, 0f);
            skipRt.pivot = new Vector2(1f, 0f);
            skipRt.anchoredPosition = new Vector2(-50f, 30f);
            skipRt.sizeDelta = new Vector2(700f, 44f);
            AddText(skipRt, "[SPASI] Lewati sinematik", fontBody, 26f, new Color(0.65f, 0.68f, 0.72f), TextAlignmentOptions.BottomRight);

            // layar hitam (fade) - di atas semuanya kecuali judul
            RectTransform fadeRt = NewRect("FadePanel", canvasTf);
            Stretch(fadeRt);
            AddImage(fadeRt, Color.black, false);
            CanvasGroup fadeGroup = fadeRt.gameObject.AddComponent<CanvasGroup>();
            fadeGroup.alpha = 1f;
            fadeGroup.blocksRaycasts = false;
            fadeGroup.interactable = false;

            // kartu judul
            RectTransform titleGroupRt = NewRect("TitleGroup", canvasTf);
            Stretch(titleGroupRt);
            CanvasGroup titleGroup = titleGroupRt.gameObject.AddComponent<CanvasGroup>();
            titleGroup.alpha = 0f;
            titleGroup.blocksRaycasts = false;
            titleGroup.interactable = false;

            RectTransform titleRt = NewRect("TitleLabel", titleGroupRt);
            titleRt.anchorMin = titleRt.anchorMax = new Vector2(0.5f, 0.5f);
            titleRt.anchoredPosition = new Vector2(0f, 50f);
            titleRt.sizeDelta = new Vector2(1600f, 220f);
            AddText(titleRt, "CYBER WATCH", fontTitle, 190f, new Color(0.93f, 0.95f, 0.97f), TextAlignmentOptions.Center);

            RectTransform subtitleRt = NewRect("SubtitleLabel", titleGroupRt);
            subtitleRt.anchorMin = subtitleRt.anchorMax = new Vector2(0.5f, 0.5f);
            subtitleRt.anchoredPosition = new Vector2(0f, -90f);
            subtitleRt.sizeDelta = new Vector2(1200f, 80f);
            TextMeshProUGUI subtitle = AddText(subtitleRt, "NIGHT SHIFT", fontBold, 52f, new Color(0.88f, 0.18f, 0.18f), TextAlignmentOptions.Center);
            subtitle.characterSpacing = 24f;

            // ----- direktur sinematik -----
            var directorGo = new GameObject("CinematicDirector");
            CinematicIntro intro = directorGo.AddComponent<CinematicIntro>();
            var so = new SerializedObject(intro);

            so.FindProperty("cam").objectReferenceValue = cam;
            so.FindProperty("volume").objectReferenceValue = volume;
            so.FindProperty("fadeGroup").objectReferenceValue = fadeGroup;
            so.FindProperty("titleGroup").objectReferenceValue = titleGroup;
            so.FindProperty("captionText").objectReferenceValue = caption;
            so.FindProperty("letterboxTop").objectReferenceValue = barTop;
            so.FindProperty("letterboxBottom").objectReferenceValue = barBottom;
            so.FindProperty("skipHint").objectReferenceValue = skipRt.gameObject;
            so.FindProperty("tutorialPanel").objectReferenceValue = panel.gameObject;
            so.FindProperty("tutorialTitle").objectReferenceValue = tTitle;
            so.FindProperty("tutorialBody").objectReferenceValue = tBody;
            so.FindProperty("nextButton").objectReferenceValue = button;
            so.FindProperty("nextButtonLabel").objectReferenceValue = buttonLabel;

            SerializedProperty shotsProp = so.FindProperty("shots");
            shotsProp.arraySize = ShotDefs.Length;
            for (int i = 0; i < ShotDefs.Length; i++)
            {
                S def = ShotDefs[i];
                SerializedProperty el = shotsProp.GetArrayElementAtIndex(i);
                el.FindPropertyRelative("name").stringValue = def.name;
                el.FindPropertyRelative("duration").floatValue = def.duration;
                el.FindPropertyRelative("fov").floatValue = def.fov;
                el.FindPropertyRelative("ease").floatValue = def.ease;
                el.FindPropertyRelative("handheld").floatValue = def.handheld;
                el.FindPropertyRelative("reveal").objectReferenceValue = def.reveal ? reveal : null;

                SerializedProperty pts = el.FindPropertyRelative("points");
                pts.arraySize = shotPoints[i].Length;
                for (int j = 0; j < shotPoints[i].Length; j++)
                    pts.GetArrayElementAtIndex(j).objectReferenceValue = shotPoints[i][j];

                SerializedProperty caps = el.FindPropertyRelative("captions");
                caps.arraySize = def.captions.Length;
                for (int j = 0; j < def.captions.Length; j++)
                {
                    SerializedProperty cp = caps.GetArrayElementAtIndex(j);
                    cp.FindPropertyRelative("text").stringValue = def.captions[j].text;
                    cp.FindPropertyRelative("start").floatValue = def.captions[j].start;
                    cp.FindPropertyRelative("duration").floatValue = def.captions[j].duration;
                }
            }

            SerializedProperty pagesProp = so.FindProperty("pages");
            pagesProp.arraySize = TutorialPages.Length;
            for (int i = 0; i < TutorialPages.Length; i++)
            {
                SerializedProperty pg = pagesProp.GetArrayElementAtIndex(i);
                pg.FindPropertyRelative("title").stringValue = TutorialPages[i][0];
                pg.FindPropertyRelative("body").stringValue = TutorialPages[i][1];
            }

            so.ApplyModifiedPropertiesWithoutUndo();

            // ----- simpan + daftarkan ke Build Settings (setelah MainMenu) -----
            EditorSceneManager.SaveScene(scene, ScenePath);
            RegisterInBuildSettings();
            AssetDatabase.SaveAssets();

            Selection.activeGameObject = directorGo;
            Debug.Log("[CinematicBuilder] Selesai: " + ScenePath +
                      "\nLangkah berikut: tambahkan SceneNames.CinematicIntro dan GameManager.StartIntro(), lalu Play scene ini.");
        }

        // ---------- bantuan ----------

        private static GameObject Spawn(string path)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                Debug.LogWarning("[CinematicBuilder] Prefab tidak ditemukan: " + path);
                return null;
            }
            return (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        }

        private static GameObject SpawnWatcher()
        {
            GameObject watcher = Spawn(SlenderPrefab);
            if (watcher == null)
                return null;

            watcher.name = "Reveal_Watcher";
            watcher.transform.SetPositionAndRotation(RevealPosition, Quaternion.Euler(0f, RevealYaw, 0f));
            watcher.transform.localScale = Vector3.one * 0.8f;

            Animator animator = watcher.GetComponentInChildren<Animator>(true);
            if (animator != null)
            {
                var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(SlenderController);
                if (controller != null)
                    animator.runtimeAnimatorController = controller;
                animator.applyRootMotion = false;
                animator.speed = 0.4f;
            }

            watcher.SetActive(false);
            return watcher;
        }

        private static void RegisterInBuildSettings()
        {
            List<EditorBuildSettingsScene> list = EditorBuildSettings.scenes.ToList();
            if (list.Any(s => s.path == ScenePath))
                return;

            int menuIndex = list.FindIndex(s => s.path.EndsWith("MainMenu_Scene.unity"));
            list.Insert(menuIndex >= 0 ? menuIndex + 1 : 0, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = list.ToArray();
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

        private static Image AddImage(RectTransform rt, Color color, bool raycast)
        {
            Image image = rt.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = raycast;
            return image;
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