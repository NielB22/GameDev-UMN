# 01 – Struktur Folder

Tujuan: setiap orang tahu **di mana sebuah file berada** dan **di mana menaruh file baru**,
supaya proyek tetap rapi walau dikerjakan bersamaan.

[← Kembali ke README](../README.md)

---

## Gambaran besar

```
Assets/
├── _Game/          ← MILIK KITA, dipakai lebih dari satu scene (script, prefab, material, audio, data)
├── Scenes/         ← MILIK KITA, satu folder per scene (file scene + isi khusus scene itu)
├── Asset_2D/       ← PIHAK KETIGA, 2D (font)            — read-only
├── Asset_3D/       ← PIHAK KETIGA, model 3D              — read-only
├── Settings/       ← Setting URP & build profile (jarang diubah)
├── TextMesh Pro/   ← Bawaan TextMeshPro (jangan diubah)
└── InputSystem_Actions.inputactions   ← Aksi input (Move, Sprint, Look, Interact)
```

> Garis besarnya: **`_Game` = bersama**, **`Scenes/<Nama>` = khusus satu scene**,
> **`Asset_*` = pihak ketiga, hanya dipakai, tidak diedit**.

---

## `Assets/_Game/` — konten bersama

```
_Game/
├── Audio/                 Semua suara game (lihat "Audio" di bawah)
│   ├── Ambience/          room tone, buzz neon, drone, detak jantung, riser blackout
│   ├── SFX/               lampu, CRT, power down, statik, senter, langkah, kursi, tempat sampah
│   ├── UI/                klik tombol, notifikasi email
│   └── Jumpscare/         riser, scream, statik keras, impact
├── Data/
│   ├── Audio/             AudioLibrary — daftar semua clip (dipanggil script lewat GameAudio)
│   ├── Lighting/          LightingProfile_Night — lighting global (lihat docs/04)
│   └── Messages/          36 pesan email/SMS (MessageData): asli/penipuan, penjelasan rapor,
│                          tingkat kesulitan tersembunyi
├── Materials/
│   ├── Effects/           M_LightCone (kerucut cahaya lampu jalan), M_VHS (efek layar)
│   ├── Environment/       Ruangan & luar (lantai, dinding, kaca, lampu, monitor, parkir, ...)
│   └── Props/             Properti kecil (LED CCTV)
├── Prefabs/
│   ├── Environment/       Office_Environment  — ruangan kantor lengkap (+ CCTV)
│   │                      Office_Exterior     — dunia luar jendela (parkiran, jalan, lampu jalan)
│   │                      Lighting_Night      — lighting malam + Dev Mode + VHS (WAJIB 1 per scene 3D)
│   └── Props/             CCTV_Camera
├── Settings/              PP_Night — profile post-process (bloom, grain, warna malam)
├── Shaders/               VHS.shader (dipakai renderer feature "VHS" di PC_Renderer)
├── Scripts/               Assembly "CyberWatch.Runtime", namespace CyberWatch.*
│   ├── Audio/             GameAudio (volume per kategori), AmbienceAudio, FootstepAudio, UiSounds
│   ├── Core/              GameManager, SceneLoader, SceneNames, GameDifficulty, MessageQuotaRules
│   ├── Effects/           ScreenGlitch, VhsEffect, LightCone, GazeReaction
│   ├── Environment/       Lampu (ThreatLightFlicker, StreetLampFlicker, MonitorGlow), CctvCamera,
│   │                      ThreatSilhouette (hantu), UnseenChanges, SceneLighting + LightingProfile
│   ├── Messages/          MessageData, InboxController
│   ├── Player/            PlayerMovement, PlayerModeController, First/ThirdPersonCameraRig, Flashlight
│   ├── Session/           ShiftClock, ShiftController, ShiftSession, MessageQuota, LoseSequence
│   ├── Threat/            ThreatLevelSystem
│   └── UI/                HUD & menu (ThreatMeter, Score, Inbox, Objective, QuotaWarning, Report, ...)
└── README.md              Ringkasan singkat (mengarah ke dokumen ini)
```

Catatan nesting prefab: `Office_Environment` **berisi** `Office_Exterior`. Jadi memasang
`Office_Environment` di scene otomatis membawa ruangan + dunia luar.

## `Assets/Scenes/<NamaScene>/` — khusus satu scene

Setiap scene punya folder sendiri. Subfolder dibuat **hanya bila ada isinya**
(`Prefabs/`, `Scripts/`, `Animation/`, `Editor/`, ...).

| Folder scene | Isi | Status |
|---|---|---|
| `MainMenu/` | Scene menu, `Prefabs/UI_MainMenu`, `Scripts/` (kamera & lampu menu) | ✅ Ikut build |
| `CinematicIntro/` | Sinematik pembuka + panel Cara Bermain, `Editor/CinematicSceneBuilder` | ✅ Ikut build |
| `GameplayDesktop/` | Inti game: `Prefabs/` (Player, UI_HUD, UI_Desktop), `Animation/` (controller hantu), `Scripts/ThreatPostProcess` | ✅ Ikut build |
| `GameOverReport/` | Rapor menang/kalah (UI langsung di scene) | ✅ Ikut build |
| `DemoScene/` | Scene uji ruangan | Tidak ikut build |

## Audio

- Semua clip didaftarkan di **`_Game/Data/Audio/AudioLibrary`**. Ganti suara = tukar clip di aset itu.
- Volume per kategori (Master, Ambience, SFX, Jumpscare, UI, suara luar) di komponen
  **Game Audio** (`GameSystems` di Gameplay, objek `Audio` di Menu/Cinematic).
- **Mengganti file audio mereset import setting-nya.** Atur lagi: suara pendek = *Decompress On
  Load* + *Preload*; suara dari satu titik (lampu, CRT, kursi) = *Force To Mono*.

## `Assets/Asset_2D/` dan `Assets/Asset_3D/` — pihak ketiga (read-only)

| Folder | Isi | Dipakai untuk |
|---|---|---|
| `Asset_2D/Fonts/VT323` | Font + TMP Font Asset | Judul, jam, Threat Level |
| `Asset_2D/Fonts/IBM_Plex_Mono` | Font Regular/Bold + TMP Font Asset | Isi pesan, panel, tombol |
| `Asset_3D/Retro Office_V2` | Furnitur & properti kantor | Isi ruangan (tekstur dikecilkan lewat import setting) |
| `Asset_3D/Security Camera by J-Toastie…` | Model CCTV | Di dalam `CCTV_Camera.prefab` |
| `Asset_3D/Pandazole_Ultimate_Pack` | Jalan, gedung, properti kota | Dunia luar (`Office_Exterior`) |
| `Asset_3D/#NVJOB Slender` | Model + animasi Slender | Hantu (`ThreatSilhouette`, jumpscare) |
| `Asset_3D/DavidJalbert/LowPolyPeople` | Karakter low-poly + animasi idle/walk/run | Pemain ("stout man a") |

**Jangan** mengedit, memindah, atau mengganti nama file di dalam folder ini.
Kalau butuh versi berbeda, buat **prefab baru di `_Game/Prefabs/`** yang memakai prefab pack
tersebut, lalu ubah lewat *override*. Mengubah *import setting* (mis. Max Size tekstur) boleh.

⚠️ Prefab Retro Office berikut **rusak** (FBX-nya kosong) — jangan dipakai:
`RetroPC_pack`, `Paper`, `Box_Filled_Grey`, `Mug&Pencil_Black`.
Untuk baki dokumen pakai `Doc_Tray_Array` (bukan `DocumentTray`).

## Folder lain

| Folder | Keterangan |
|---|---|
| `Assets/Settings/` | URP asset, renderer (berisi feature **VHS**), build profile. Ubah hanya kalau disepakati tim. **GPU Resident Drawer sengaja dimatikan** di `PC_RPAsset`. |
| `Assets/TextMesh Pro/` | Hasil *Import TMP Essentials*. Jangan diubah. |
| `docs/` (di luar `Assets`) | Dokumentasi ini + brief perubahan untuk tim (`docs/brief/`). Tidak diimpor Unity. |
| `Build/` | Hasil build `.exe`. **Tidak masuk git.** |
| `Library/`, `Temp/`, `Logs/`, `UserSettings/`, `*.csproj`, `*.slnx` | Dibuat otomatis oleh Unity/IDE — **tidak masuk git**. |

---

## "File baru saya taruh di mana?"

Ikuti pertanyaan ini dari atas:

| Pertanyaan | Ya → taruh di |
|---|---|
| Aset hasil download (model, font, pack)? | `Asset_3D/<NamaPack>/` atau `Asset_2D/<Jenis>/` |
| File suara? | `_Game/Audio/<Ambience \| SFX \| UI \| Jumpscare>/` + daftarkan di `AudioLibrary` |
| Dipakai **lebih dari satu scene**? | `_Game/<Scripts \| Prefabs \| Materials \| Data>/...` |
| Hanya dipakai **satu scene**? | `Scenes/<NamaScene>/<Scripts \| Prefabs \| ...>/` |
| Pesan email/SMS baru? | `_Game/Data/Messages/` (*Create → Cyber Watch → Message*), lalu tambahkan ke **Inbox Controller** |

Ragu? Mulai di folder scene. Kalau nanti dipakai scene lain, pindahkan ke `_Game`
**lewat jendela Project Unity** (bukan Explorer) agar referensi tidak putus.

## Konvensi penamaan

| Jenis | Pola | Contoh |
|---|---|---|
| Scene | `Nama_Scene` | `GameOver_Report_Scene` |
| Prefab UI | `UI_Nama` | `UI_HUD`, `UI_Desktop` |
| Material | `M_Nama` | `M_SodiumLamp`, `M_Wall` |
| Pesan | `MSG_NN_Deskripsi` | `MSG_02_PayPal_Blokir` |
| Audio | `amb_`, `sfx_`, `js_` + deskripsi | `amb_horror_drone`, `sfx_ui_click`, `js_scream` |
| Script | `PascalCase`, satu class per file, namespace `CyberWatch.<Folder>` | `ThreatMeterView` |
| Objek di Hierarchy | Grup dengan nama jelas | `Structure`, `Furniture`, `Props`, `Lighting` |
