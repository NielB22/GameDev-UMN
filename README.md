# Cyber Watch: Night Shift

Game 3D horor atmosferik (tanpa *jumpscare*) bertema **kesadaran penipuan digital**.
Pemain adalah staf IT shift malam (00:00–06:00) yang memeriksa email/SMS dan memutuskan
**TERIMA** atau **BLOKIR**. Setiap keputusan salah menaikkan **Threat Level**, dan lingkungan
kantor ikut berubah semakin mencekam.

Proyek UTS **IF581 Game Development (Lab)** — Universitas Multimedia Nusantara.

---

## Spesifikasi proyek

| | |
|---|---|
| Engine | **Unity 6.6** (`6000.6.0f1`) — wajib versi yang sama |
| Render pipeline | **URP** 17.6 (profil PC: `Assets/Settings/PC_RPAsset`) |
| Input | **Input System** baru saja (Input Manager lama tidak aktif) |
| Kamera | Cinemachine 3.1.7 |
| Teks UI | TextMeshPro — font VT323 & IBM Plex Mono |
| Target build | Windows `.exe` |

## Mulai dari sini

1. Clone repo, buka folder proyek dengan **Unity Hub** memakai **Unity 6000.6.0f1**.
2. Buka `Assets/Scenes/GameplayDesktop/Gameplay_Desktop_Scene` lalu tekan **Play**.
3. Scene gelap? Itu memang lighting game. Untuk bekerja, pilih objek **`Lighting_Night`**
   di Hierarchy → centang **Dev Mode** (lihat [Lighting](docs/04-lighting.md)).
4. Baca dokumen di bawah **sebelum** menambah file atau scene baru.

## Dokumentasi tim

| Dokumen | Isi |
|---|---|
| [01 – Struktur folder](docs/01-struktur-folder.md) | Arti setiap folder dan di mana menaruh file baru |
| [02 – Membuat scene baru](docs/02-membuat-scene-baru.md) | Checklist langkah demi langkah |
| 03 – Aturan aset | *(menyusul)* |
| 04 – Lighting | *(menyusul)* |
| 05 – Sistem gameplay | *(menyusul)* |
| 06 – Alur kerja Git | *(menyusul)* |

## Alur game & status scene

```
MainMenu_Scene → Office_TPP_Scene → Gameplay_Desktop_Scene → GameOver_Report_Scene
   (menu)         (jalan di kantor)    (inbox, threat, jam)      (rapor + Retry/Menu)
```

| Scene | Status |
|---|---|
| `Gameplay_Desktop_Scene` | ✅ Bisa dimainkan: inbox, hover link, HUD, menang/kalah |
| `GameOver_Report_Scene` | ⏳ Berikutnya |
| `MainMenu_Scene` | ⏳ |
| `Office_TPP_Scene` | ⏳ |
| `DemoScene` | Scene uji ruangan (tidak ikut build) |

## 5 aturan utama

1. **Aset pihak ketiga (`Asset_2D`, `Asset_3D`) read-only.** Pakai **prefab**-nya, bukan `.fbx`.
2. **Ruangan kantor hanya diedit di `Office_Environment.prefab`**, bukan di scene.
3. **Satu `Lighting_Night` di setiap scene** — jangan menambah directional light lain.
4. **Save di Unity (Ctrl+S) sebelum commit** — perubahan yang belum di-save tidak ikut.
5. Pindah scene **lewat `GameManager` / `SceneLoader`**, bukan `SceneManager` langsung.
