# 02 – Membuat Scene Baru

Checklist supaya setiap scene **konsisten** (lighting, UI, alur) dan tidak menimbulkan bug.
Contoh di bawah memakai scene `GameOver_Report_Scene`; ganti sesuai scene yang dikerjakan.

[← Kembali ke README](../README.md)

---

## Sebelum mulai

- [ ] `git pull` dulu agar mendapat versi terbaru.
- [ ] Beri tahu tim scene mana yang kamu kerjakan — **dua orang mengedit scene yang sama
      hampir pasti bentrok** (file `.unity` sulit di-merge).

## 1. File & folder

Keempat scene game **sudah ada** beserta foldernya. Buka saja dari
`Assets/Scenes/<Folder>/`. Langkah ini hanya untuk scene yang benar-benar baru:

- [ ] Buat folder `Assets/Scenes/<NamaFolder>/` (pola sama dengan folder scene lain).
- [ ] *Right-click → Create → Scene*, beri nama `Nama_Scene`.
- [ ] Buat subfolder hanya saat dibutuhkan: `Prefabs/`, `Scripts/`, `Audio/`, `UI/`.

## 2. Isi wajib setiap scene

Pasang dari jendela **Project** (drag ke Hierarchy):

| Objek | Dari | Wajib? | Catatan |
|---|---|---|---|
| **`Lighting_Night`** | `_Game/Prefabs/Environment/` | ✅ Scene 3D | Mengatur bulan, fog, ambient, warna langit. **Hapus "Directional Light" bawaan scene.** Scene yang hanya UI (mis. Report) tidak perlu. |
| **Main Camera** | (bawaan scene) | ✅ Selalu | Tag harus `MainCamera`. Warna latar diatur otomatis oleh `Lighting_Night`. |
| **Global Volume** | Salin dari `Gameplay_Desktop_Scene` | Disarankan | Post-processing yang sama dengan scene lain. |
| **`Office_Environment`** | `_Game/Prefabs/Environment/` | Jika scene di dalam kantor | Sudah termasuk dunia luar & CCTV. Jangan diedit di scene — edit prefab-nya. |

- [ ] Hierarchy rapi: objek yang sejenis dikelompokkan (mis. `GameSystems`, `UI_...`).

## 3. UI (jika scene punya UI)

- [ ] Buat UI sebagai **prefab** di `Scenes/<Folder>/Prefabs/UI_Nama.prefab`.
- [ ] **Canvas**: *Render Mode* `Screen Space - Overlay`.
- [ ] **Canvas Scaler** — wajib untuk nilai UTS "responsif":
  - *UI Scale Mode*: **Scale With Screen Size**
  - *Reference Resolution*: **1920 × 1080**
  - *Match*: **0.5**
- [ ] Tambahkan komponen **`EnsureEventSystem`** ke Canvas jika ada tombol
      (proyek memakai Input System baru; komponen ini membuat EventSystem yang benar).
- [ ] Teks memakai **TextMeshPro** dengan font:
      **VT323** (judul/angka besar) atau **IBM Plex Mono** (teks biasa).
- [ ] Anchor setiap panel ke tepi/sudut yang sesuai (bukan di tengah semua) agar tetap pas
      di resolusi lain. Uji di tab Game dengan beberapa rasio (16:9, 16:10, 4:3).

## 4. Logika & perpindahan scene

- [ ] Script khusus scene → `Scenes/<Folder>/Scripts/`. Script yang dipakai ulang → `_Game/Scripts/`.
- [ ] **Pindah scene selalu lewat `GameManager`**, contoh:

  ```csharp
  using CyberWatch.Core;

  GameManager.Instance.GoToMainMenu();      // ke menu
  GameManager.Instance.StartNewShift();     // mulai shift baru (ke Gameplay_Desktop_Scene, mode Jalan)
  ```

  Data shift (keputusan, akurasi, menang/kalah) ada di `GameManager.Instance.CurrentShift`.
- [ ] Jangan memanggil `SceneManager.LoadScene` langsung.
- [ ] Butuh Threat Level / jam / inbox? Cari otomatis atau sambungkan di Inspector;
      **berlangganan event** (`StageChanged`, `TimeChanged`, ...) — jangan cek tiap frame.

## 5. Build Settings

- [ ] Scene **baru** harus didaftarkan: *File → Build Profiles → Scene List → Add Open Scenes*,
      lalu tambahkan namanya ke `_Game/Scripts/Core/SceneNames.cs`.
- [ ] Urutan scene game: `MainMenu` → `Gameplay_Desktop` → `GameOver_Report`.
      `DemoScene` dibiarkan **tidak dicentang**.

## 6. Cek sebelum commit

- [ ] **Play** scene ini langsung — tidak ada error merah di Console.
- [ ] Mainkan alur dari scene sebelumnya ke scene ini dan ke scene berikutnya.
- [ ] `Lighting_Night` → **Dev Mode dimatikan**.
- [ ] **Ctrl+S** (scene) — dan *Save* di Prefab Mode bila mengedit prefab.
- [ ] Jika mengubah objek prefab di scene dan ingin berlaku di semua scene:
      *Overrides → Apply to Prefab '<prefab kita>'* — **jangan** apply ke prefab pack pihak ketiga.
- [ ] Commit dengan pesan jelas, mis. `feat(report): layar rapor + tombol Retry/Menu`.

---

## Ringkasan 1 menit

```
Folder sendiri → Lighting_Night (hapus Directional Light) → Camera (+ Global Volume)
→ Office_Environment bila di kantor → UI prefab (Canvas Scaler 1920x1080 + EnsureEventSystem)
→ pindah scene via GameManager → Build Settings → Play tanpa error → Dev Mode off → Save → commit
```
