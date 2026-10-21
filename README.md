# Cyber Watch: Night Shift

Game 3D horor atmosferik bertema **kesadaran penipuan digital**.
Pemain adalah staf IT shift malam (00:00–06:00) yang memeriksa email/SMS dan memutuskan
**TERIMA** atau **BLOKIR**. Setiap keputusan salah menaikkan **Threat Level**: kantor makin gelap,
lampu berkedip, dan sosok di luar jendela makin dekat. Kalau Threat mencapai 100%, sosok itu masuk.

Proyek UTS **IF581 Game Development (Lab)** — Universitas Multimedia Nusantara.

---

## Spesifikasi proyek

| | |
|---|---|
| Engine | **Unity 6.6** (`6000.6.0f1`) — wajib versi yang sama |
| Render pipeline | **URP** 17.6 (profil PC: `Assets/Settings/PC_RPAsset`, post-process `_Game/Settings/PP_Night`) |
| Input | **Input System** baru saja (Input Manager lama tidak aktif) |
| Kamera | Cinemachine 3.1.7 |
| Teks UI | TextMeshPro — font VT323 & IBM Plex Mono |
| Target build | Windows 10/11 64-bit (`.exe`), nama produk **Cyber Watch Night Shift** |

## Cara main (build)

1. Ekstrak zip, buka foldernya, **double-click `Cyber Watch Night Shift.exe`**. Tidak perlu Unity.
2. Semua isi folder harus tetap bersama (`*_Data`, DLL, dst.).
3. Muncul "Windows protected your PC"? Klik **More info → Run anyway** (game belum di-*sign*).

### Kontrol

| Tombol | Fungsi |
|---|---|
| WASD / Shift | Jalan / lari |
| Mouse | Menoleh |
| E | Duduk di meja IT · E / Esc untuk berdiri |
| V | Ganti kamera FPP ⇄ TPP (mulai di FPP) |
| F | Senter (baterai habis → goyangkan mouse untuk menyalakan lagi) |
| Klik | TERIMA / BLOKIR · arahkan kursor ke link untuk melihat URL aslinya |

### Aturan

- **Menang**: bertahan sampai **06:00** dengan Threat di bawah 100%.
- **Kalah**: Threat mencapai **100%** → lampu padam satu per satu, lalu sosok itu masuk.
- **Target**: jawab minimal sejumlah pesan **setiap jam** (lihat panel TARGET di bawah jam).
  Setiap pesan yang tertinggal saat jam berganti menaikkan Threat — mendiamkan inbox bukan jalan keluar.
- **Mode** (dipilih setelah MULAI SHIFT): **NORMAL** atau **LEMBUR TERAKHIR**.

## Mulai dari sini (developer)

1. Clone repo, buka folder proyek dengan **Unity Hub** memakai **Unity 6000.6.0f1**.
2. Buka `Assets/Scenes/MainMenu/MainMenu_Scene` lalu tekan **Play** (alur lengkap), atau langsung
   `Assets/Scenes/GameplayDesktop/Gameplay_Desktop_Scene`.
3. Scene gelap? Itu memang lighting game. Untuk bekerja, pilih objek **`Lighting_Night`**
   di Hierarchy → centang **Dev Mode** (lihat [Lighting](docs/04-lighting.md)). **Matikan lagi sebelum commit.**
4. Uji cepat Threat: komponen **Threat Level System** (objek `GameSystems`) → klik kanan header saat Play
   → Set 35 / 55 / 80 / 95. Uji cepat menang: perkecil `Real Duration Seconds` di **Shift Clock** saat Play.
5. Baca dokumen di bawah **sebelum** menambah file atau scene baru.

### Build

1. Pastikan **Dev Mode `Lighting_Night` mati** di semua scene.
2. Kosongkan folder `Build/`, lalu **File → Build Profiles → Windows → Build** ke `Build/`
   (tanpa *Development Build*).
3. Untuk dikumpulkan: zip **isi** `Build/` **tanpa** folder `*_BackUpThisFolder_ButDontShipItWithYourGame`
   (sekitar 50 MB).

## Dokumentasi tim

| Dokumen | Isi |
|---|---|
| [01 – Struktur folder](docs/01-struktur-folder.md) | Arti setiap folder, di mana menaruh file baru, audio |
| [02 – Membuat scene baru](docs/02-membuat-scene-baru.md) | Checklist langkah demi langkah |
| [04 – Lighting](docs/04-lighting.md) | Prinsip "hanya yang terkena cahaya terlihat", Lighting Profile global, Dev Mode |
| [07 – Mode Jalan ↔ Meja](docs/07-mode-jalan-meja.md) | Berkeliling kantor saat shift + checklist uji alur penuh |
| [Brief perubahan](docs/brief/) | Ringkasan perubahan untuk tim (`01-…`, `02-…`) |

## Alur game & status scene

```
MainMenu ──► Cinematic_Intro ──► Gameplay_Desktop ─────────────────────► GameOver_Report
 (pilih       (pembuka +          mode Jalan ⇄ mode Meja                   (rapor + Retry/Menu)
  mode)        Cara Bermain)      menang: 06:00 · kalah: Threat 100% → jumpscare
```

| Scene | Status |
|---|---|
| `MainMenu_Scene` | ✅ Latar kantor, lampu redup-terang, MULAI SHIFT → NORMAL / LEMBUR TERAKHIR, Keluar |
| `Cinematic_Intro_Scene` | ✅ Sinematik pembuka + panel Cara Bermain (aturan sesuai mode) |
| `Gameplay_Desktop_Scene` | ✅ Mode Jalan (FPP/TPP, senter) ⇄ mode Meja (inbox 36 pesan, hover link), HUD (Threat, skor, jam, target), efek per Threat, hantu, audio, sekuens kalah + jumpscare |
| `GameOver_Report_Scene` | ✅ Rapor: akurasi, peringkat, daftar kesalahan + penjelasan, Retry / Main Menu |
| `DemoScene` | Scene uji ruangan (tidak ikut build) |

## 5 aturan utama

1. **Aset pihak ketiga (`Asset_2D`, `Asset_3D`) read-only.** Pakai **prefab**-nya, bukan `.fbx`.
2. **Ruangan kantor hanya diedit di `Office_Environment.prefab`**, bukan di scene.
3. **Satu `Lighting_Night` di setiap scene 3D** — jangan menambah directional light lain.
4. **Save di Unity (Ctrl+S / File → Save Project) sebelum commit**, dan atur nilai di luar Play Mode.
5. Pindah scene **lewat `GameManager` / `SceneLoader`**, bukan `SceneManager` langsung.
