# 07 – Mode Jalan ↔ Meja (rencana fitur)

Pemain bisa **kapan saja berdiri dari meja IT dan berkeliling kantor** (third person), lalu
kembali duduk untuk bekerja. Tujuannya: pemain **merasakan langsung** perubahan lingkungan
akibat Threat Level (lampu berkedip/mati, CCTV mengikuti, sosok di luar jendela).

Status: **step 1–5 selesai**, step 6 (uji alur penuh) memakai checklist di bawah.

[← Kembali ke README](../README.md)

---

## Keputusan desain (disepakati tim, 15 Okt)

| # | Pertanyaan | Keputusan |
|---|---|---|
| 1 | Jam shift tetap berjalan saat pemain berjalan-jalan? | **Ya** — menambah ketegangan: periksa ruangan vs urus inbox. |
| 2 | Pesan menumpuk saat pemain tidak di meja? | **Dibiarkan**, tanpa penalti. HUD menampilkan notifikasi "pesan baru". |
| 3 | Game dimulai dalam mode apa? | **Mode Jalan** — pemain masuk kantor, lalu duduk di meja untuk bekerja. |

Konsekuensi: **`Office_TPP_Scene` dihapus**. Semuanya terjadi di `Gameplay_Desktop_Scene`,
sehingga jam, Threat, inbox, sosok, dan lampu tidak perlu dipindah antar scene.

```
MainMenu_Scene ──► Gameplay_Desktop_Scene ─────────────────────────► GameOver_Report_Scene
                   ┌──────────────┐  [E] Duduk   ┌──────────────┐   06:00 / Threat 100%
                   │  MODE JALAN  │ ───────────► │  MODE MEJA   │
                   │ (third person)│ ◄─────────── │ (FPV monitor)│
                   └──────────────┘ [E]/[Esc]    └──────────────┘
                                     Berdiri
```

## Perilaku per mode

| | Mode Jalan | Mode Meja |
|---|---|---|
| Kamera | Third person mengikuti karakter | Diam menatap monitor (posisi FPV sekarang) |
| Kontrol | WASD jalan, mouse lihat, Shift lari, **E** duduk (di dekat meja) | Mouse untuk UI, **E / Esc** berdiri |
| Kursor | Terkunci & tersembunyi | Terlihat |
| `UI_HUD` (Threat, skor, jam) | **Tampil** | Tampil |
| `UI_Desktop` (inbox) | **Disembunyikan** | Tampil |
| Jam, Threat, inbox, sosok, lampu, CCTV | **Tetap berjalan** | Tetap berjalan |

Perpindahan kamera memakai **Cinemachine blend** (halus, bukan potong mendadak).

## Rencana komponen

| Bagian | Lokasi | Keterangan |
|---|---|---|
| Script gerak & mode | `Assets/_Game/Scripts/Player/` | `PlayerMovement` (CharacterController + Input System), `PlayerModeController` (Jalan/Meja), `DeskInteraction` (zona "[E] Duduk") — namespace `CyberWatch.Player` |
| Prefab pemain | Prefab pemain & rig kamera | `Assets/Scenes/GameplayDesktop/Prefabs/Player/` | Karakter LowPolyPeople + CharacterController + Animator, kamera TPP & kamera Meja (Cinemachine) | rig kamera | `Assets/Scenes/GameplayDesktop/Prefabs/Player/` | Karakter **`stout man a`** (LowPolyPeople, dipilih tim) + CharacterController + Animator, kamera TPP | Prefab pemain & rig kamera | `Assets/Scenes/GameplayDesktop/Prefabs/Player/` | Karakter LowPolyPeople + CharacterController + Animator, kamera TPP & kamera Meja (Cinemachine) | kamera Meja (Cinemachine). Tidak ada animasi duduk: di mode Meja karakter disembunyikan karena kamera FPV |
| Animator pemain | `Assets/Scenes/GameplayDesktop/Animation/` | Idle / Walk / Run dari pack LowPolyPeople |
| Collider furnitur | Grup `Colliders` di `Office_Environment.prefab` | Prefab Retro Office **tidak punya collider** → kotak collider tak terlihat agar pemain tidak menembus meja/loker. Pack tidak diubah. |
| Petunjuk & notifikasi | `UI_HUD` | "[E] Duduk" / "[E] Berdiri" dan "Pesan baru" saat di mode Jalan |
| Input | `Assets/InputSystem_Actions` (map **Player**) | Sudah ada: Move, Look, Interact, Sprint |

Sistem yang **tidak perlu diubah**: CCTV dan sosok sudah mengikuti `Camera.main`, jadi otomatis
mengikuti pemain saat berjalan; HUD sudah terpisah dari inbox.

## Urutan implementasi (1 step = 1 commit, dicek di Unity)

| # | Step | Commit | Hasil |
|---|---|---|---|
| 1 | Collider furnitur | `433857f` | Grup `Office_Environment/Colliders`: 14 BoxCollider tak terlihat |
| 2 | Prefab pemain | `6e6c9c4` | `Player` (stout man a, skala 0.85) + `PlayerMovement` (WASD, Shift lari, animasi idle/walk/run) |
| 3 | Kamera | `6c7f970` | `CinemachineBrain` + `CM_DeskCam` + `CM_PlayerCam` (`ThirdPersonCameraRig`: orbit mouse, tidak menembus dinding) |
| 4 | Duduk / berdiri | `36ca17c` | `PlayerModeController` di `GameSystems`: [E] duduk di zona kursi, [E]/[Esc] berdiri; inbox & kursor diatur; karakter hilang setelah kamera tiba di meja |
| 5 | Petunjuk HUD | `70c75e3` | `WalkHintsView`: "[E] DUDUK DI MEJA" / "[E]/[ESC] BERDIRI" + "PESAN BARU – KEMBALI KE MEJA" |
| 6 | Uji alur penuh | — | Checklist di bawah |

### Pengaturan yang bisa disesuaikan (Inspector)

| Yang diatur | Di mana |
|---|---|
| Kecepatan jalan/lari, kecepatan berputar | `Player` → *Player Movement* |
| Ukuran karakter | `Player.prefab` → `Model` → Scale (sekarang 0.85) |
| Jarak kamera, sensitivitas mouse, batas atas-bawah | `CM_PlayerCam` → *Third Person Camera Rig* |
| Lama blend kamera Jalan ↔ Meja | `Main Camera` → *Cinemachine Brain* → Default Blend |
| Zona duduk (posisi & radius) | `GameSystems` → *Player Mode Controller* (terlihat sebagai lingkaran hijau saat dipilih) |
| Teks petunjuk | `UI_HUD/WalkHints` → *Walk Hints View* |

## Step 6 – Checklist uji alur penuh

Mainkan dari **MainMenu_Scene** (bukan langsung Gameplay) dan centang:

**Menu → Gameplay**
- [ ] MULAI SHIFT → Gameplay dimuat, mulai di **mode Jalan, kamera FPP**, inbox tidak terlihat, HUD terlihat, kursor terkunci.

**Mode Jalan**
- [ ] WASD/Shift/mouse responsif; karakter tidak menembus dinding, meja, loker, dispenser.
- [ ] Kamera tidak menembus dinding di keempat pojok ruangan.
- [ ] **[V]** ganti FPP ⇄ TPP (blend singkat, arah pandang tetap). Di FPP badan tak terlihat tapi bayangannya ada. Pojok kanan bawah menampilkan "[V] KAMERA: FPP/TPP". V tidak berfungsi saat duduk; berdiri kembali ke view terakhir.
- [ ] **[F]** senter putih kekuningan (FPP dari dekat mata, TPP dari dada). Pojok kanan bawah: "[F] SENTER" + bar baterai (oranye saat lemah). Baterai habis → berkedip lalu mati, HUD merah "SENTER MATI - GOYANGKAN MOUSE!" dan bar = progres goyangan; goyang mouse cepat → berkedip lalu nyala, baterai penuh. Duduk atau berdiri = senter otomatis mati (default tanpa senter), nyalakan lagi dengan [F].
- [ ] Notifikasi "PESAN BARU" muncul saat ada pesan menunggu.
- [ ] Jam tetap berjalan saat berkeliling.

**Duduk ↔ Berdiri**
- [ ] "[E] DUDUK DI MEJA" hanya muncul di dekat kursi IT; E di tempat lain tidak berpengaruh.
- [ ] Duduk: kamera meluncur ke monitor, karakter hilang di akhir transisi, inbox & kursor muncul, TERIMA/BLOKIR berfungsi, hover link berfungsi.
- [ ] Berdiri ([E] dan [Esc]): kembali ke belakang karakter, inbox hilang, kursor terkunci.
- [ ] Ulangi duduk/berdiri berkali-kali dengan cepat — tidak ada yang tersangkut.

**Threat (Debug di *Threat Level System*) saat mode Jalan & Meja**
- [ ] Set 35: CCTV mulai menoleh ke pemain, lampu plafon sesekali meredup, sosok sesekali lewat menyeberangi cahaya lampu jalan jauh.
- [ ] Set 55: lampu plafon mati, sosok berdiri diam di bawah lampu jalan jauh (kiri/kanan), lampu jalan sesekali berkedip.
- [ ] Set 80/95: lampu meja mati, sosok pindah ke lampu parkir lalu mendekati kaca, lampu jalan lebih sering berkedip & kadang padam sebentar, cahaya monitor berdenyut redup-terang, grain/chromatic/distorsi halus naik, vignette berdenyut.

**Akhir shift**
- [ ] **Target per jam**: panel TARGET di bawah jam; mendiamkan pesan → banner oranye + Threat naik di setiap pergantian jam (01:00–05:00), sampai kalah.
- [ ] **Mode**: NORMAL (pesan mudah → sulit) dan LEMBUR TERAKHIR (acak, lebih cepat, penalti lebih besar); angka di Cara Bermain sesuai mode.
- [ ] Kalah (Threat 100%) **saat mode Jalan** → Rapor tampil, kursor terlihat & bisa klik.
- [ ] Menang (Real Duration Seconds = 20 untuk tes) → Rapor "SHIFT SELESAI".
- [ ] Retry → Gameplay baru di mode Jalan, skor & Threat ter-reset.
- [ ] Main Menu → kembali ke menu, kursor terlihat.
- [ ] Console bersih dari error merah sepanjang alur.

## Risiko yang perlu diuji (rubrik Q4)

- Kamera TPP menembus dinding di ruangan sempit (8 × 6 m).
- Kursor tetap terkunci/terlepas di saat yang salah (Rapor & Menu me-reset kursor).
- Pemain tersangkut di antara furnitur.
- Tombol **E** bentrok dengan input lain.
- Di **editor**, Esc juga melepas kursor dari jendela Game — klik tab Game lagi. Di build tidak terjadi.
