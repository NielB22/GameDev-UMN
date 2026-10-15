# 01 – Struktur Folder

Tujuan: setiap orang tahu **di mana sebuah file berada** dan **di mana menaruh file baru**,
supaya proyek tetap rapi walau dikerjakan bersamaan.

[← Kembali ke README](../README.md)

---

## Gambaran besar

```
Assets/
├── _Game/          ← MILIK KITA, dipakai lebih dari satu scene (script, prefab, material, data)
├── Scenes/         ← MILIK KITA, satu folder per scene (file scene + isi khusus scene itu)
├── Asset_2D/       ← PIHAK KETIGA, 2D (font)            — read-only
├── Asset_3D/       ← PIHAK KETIGA, model 3D              — read-only
├── Settings/       ← Setting URP & build (jarang diubah)
├── TextMesh Pro/   ← Bawaan TextMeshPro (jangan diubah)
└── TutorialInfo/, Readme.asset   ← Bawaan template Unity (abaikan)
```

> Garis besarnya: **`_Game` = bersama**, **`Scenes/<Nama>` = khusus satu scene**,
> **`Asset_*` = pihak ketiga, hanya dipakai, tidak diedit**.

---

## `Assets/_Game/` — konten bersama

```
_Game/
├── Data/
│   ├── Lighting/          LightingProfile_Night — pengaturan lighting global (lihat docs/04)
│   └── Messages/          Pesan email/SMS (MessageData) — asli atau penipuan + penjelasan rapor
├── Materials/
│   ├── Environment/       Material ruangan & luar (lantai, dinding, kaca, lampu, garis parkir, ...)
│   └── Props/             Material properti kecil (mis. LED CCTV)
├── Prefabs/
│   ├── Environment/       Office_Environment  — ruangan kantor lengkap (+ CCTV)
│   │                      Office_Exterior     — dunia luar jendela (parkiran, jalan, gedung)
│   │                      Lighting_Night      — lighting malam + Dev Mode (WAJIB 1 per scene)
│   └── Props/             CCTV_Camera
├── Scripts/               Assembly "CyberWatch.Runtime", namespace CyberWatch.*
│   ├── Core/              GameManager (alur game + data shift), SceneLoader, SceneNames
│   ├── Environment/       CctvCamera, SceneLighting + LightingProfile, ThreatSilhouette (sosok Slender), ThreatLightFlicker
│   ├── Messages/          MessageData, InboxController
│   ├── Player/            PlayerMovement, ThirdPersonCameraRig, PlayerModeController
│   ├── Session/           ShiftSession, ShiftClock, ShiftController (menang/kalah)
│   ├── Threat/            ThreatLevelSystem
│   └── UI/                View UI (ThreatMeter, Score, Inbox, LinkTooltip, ReportView, ...)
└── README.md              Ringkasan singkat (mengarah ke dokumen ini)
```

Catatan nesting prefab: `Office_Environment` **berisi** `Office_Exterior`. Jadi memasang
`Office_Environment` di scene otomatis membawa ruangan + dunia luar.

## `Assets/Scenes/<NamaScene>/` — khusus satu scene

Setiap scene punya folder sendiri dengan pola yang sama:

```
Scenes/GameplayDesktop/
├── Gameplay_Desktop_Scene.unity
├── Prefabs/      UI atau objek yang HANYA dipakai scene ini (mis. UI_HUD, UI_Desktop)
├── Scripts/      Script yang HANYA dipakai scene ini
├── Audio/        Suara khusus scene ini
├── Materials/    Material khusus scene ini
└── Shaders/      (opsional)
```

| Folder scene | Scene | Status |
|---|---|---|
| `DemoScene/` | Scene uji ruangan | Tidak ikut build |
| `MainMenu/` | Menu utama | ⏳ |
| `GameplayDesktop/` | Inti game: mode Jalan (`Prefabs/Player/`) dan mode Meja (inbox, threat, jam, sosok, lampu kedip). `Scripts/ThreatPostProcess` = efek layar per Threat | ✅ |
| `GameOverReport/` | Rapor menang/kalah (UI langsung di scene, `ReportView`) | ✅ |

Folder yang masih kosong boleh dibiarkan; isi saat dibutuhkan.

## `Assets/Asset_2D/` dan `Assets/Asset_3D/` — pihak ketiga (read-only)

| Folder | Isi | Dipakai untuk |
|---|---|---|
| `Asset_2D/Fonts/VT323` | Font + TMP Font Asset | Judul, jam, Threat Level |
| `Asset_2D/Fonts/IBM_Plex_Mono` | Font Regular/Bold + TMP Font Asset | Isi pesan, panel, tombol |
| `Asset_3D/Retro Office_V2` | Furnitur & properti kantor | Isi ruangan |
| `Asset_3D/Security Camera by J-Toastie…` | Model CCTV | Di dalam `CCTV_Camera.prefab` |
| `Asset_3D/Pandazole_Ultimate_Pack` | Jalan, gedung, pohon, properti kota | Dunia luar (`Office_Exterior`) |
| `Asset_3D/#NVJOB Slender` | Model + animasi sosok Slender (folder `Example` sudah dihapus) | Sosok di luar jendela (`ThreatSilhouette`) |
| `Asset_3D/DavidJalbert/LowPolyPeople` | Karakter low-poly + animasi idle/walk/run | Karakter pemain mode Jalan di `Gameplay_Desktop_Scene` (rencana) |

**Jangan** mengedit, memindah, atau mengganti nama file di dalam folder ini.
Kalau butuh versi berbeda, buat **prefab baru di `_Game/Prefabs/`** yang memakai prefab pack
tersebut, lalu ubah lewat *override*.

⚠️ Prefab Retro Office berikut **rusak** (FBX-nya kosong) — jangan dipakai:
`RetroPC_pack`, `Paper`, `Box_Filled_Grey`, `Mug&Pencil_Black`.
Untuk baki dokumen pakai `Doc_Tray_Array` (bukan `DocumentTray`).

## Folder lain

| Folder | Keterangan |
|---|---|
| `Assets/Settings/` | URP asset, renderer, volume profile, build profile. Ubah hanya kalau disepakati tim. Catatan: **GPU Resident Drawer sengaja dimatikan** di `PC_RPAsset` (proyek kecil: build lebih cepat, tanpa efek visual). |
| `Assets/TextMesh Pro/` | Hasil *Import TMP Essentials*. Jangan diubah. |
| `docs/` (di luar `Assets`) | Dokumentasi ini. Tidak diimpor Unity. |
| `Library/`, `Temp/`, `Logs/`, `UserSettings/`, `BuildOutput/`, `Assets/_Recovery/` | Dibuat otomatis — **tidak masuk git** (sudah di `.gitignore`). |

---

## "File baru saya taruh di mana?"

Ikuti pertanyaan ini dari atas:

| Pertanyaan | Ya → taruh di |
|---|---|
| Aset hasil download (model, font, suara pack)? | `Asset_3D/<NamaPack>/` atau `Asset_2D/<Jenis>/` |
| Dipakai **lebih dari satu scene**? | `_Game/<Scripts \| Prefabs \| Materials \| Data>/...` |
| Hanya dipakai **satu scene**? | `Scenes/<NamaScene>/<Scripts \| Prefabs \| Audio \| ...>/` |
| Pesan email/SMS baru? | `_Game/Data/Messages/` (*Create → Cyber Watch → Message*) |

Ragu? Mulai di folder scene. Kalau nanti dipakai scene lain, pindahkan ke `_Game`
**lewat jendela Project Unity** (bukan Explorer) agar referensi tidak putus.

## Konvensi penamaan

| Jenis | Pola | Contoh |
|---|---|---|
| Scene | `Nama_Scene` | `GameOver_Report_Scene` |
| Prefab UI | `UI_Nama` | `UI_HUD`, `UI_Desktop` |
| Material | `M_Nama` | `M_SodiumLamp`, `M_Wall` |
| Pesan | `MSG_NN_Deskripsi` | `MSG_02_PayPal_Blokir` |
| Script | `PascalCase`, satu class per file, namespace `CyberWatch.<Folder>` | `ThreatMeterView` |
| Objek di Hierarchy | Grup dengan nama jelas | `Structure`, `Furniture`, `Props`, `Lighting` |
