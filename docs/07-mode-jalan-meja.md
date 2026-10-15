# 07 – Mode Jalan ↔ Meja (rencana fitur)

Pemain bisa **kapan saja berdiri dari meja IT dan berkeliling kantor** (third person), lalu
kembali duduk untuk bekerja. Tujuannya: pemain **merasakan langsung** perubahan lingkungan
akibat Threat Level (lampu berkedip/mati, CCTV mengikuti, sosok di luar jendela).

Status: **persiapan selesai** (folder, alur, dokumentasi). Implementasi menyusul per step.

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
| Prefab pemain & rig kamera | `Assets/Scenes/GameplayDesktop/Prefabs/Player/` | Karakter LowPolyPeople + CharacterController + Animator, kamera TPP & kamera Meja (Cinemachine) |
| Animator pemain | `Assets/Scenes/GameplayDesktop/Animation/` | Idle / Walk / Run dari pack LowPolyPeople |
| Collider furnitur | Grup `Colliders` di `Office_Environment.prefab` | Prefab Retro Office **tidak punya collider** → kotak collider tak terlihat agar pemain tidak menembus meja/loker. Pack tidak diubah. |
| Petunjuk & notifikasi | `UI_HUD` | "[E] Duduk" / "[E] Berdiri" dan "Pesan baru" saat di mode Jalan |
| Input | `Assets/InputSystem_Actions` (map **Player**) | Sudah ada: Move, Look, Interact, Sprint |

Sistem yang **tidak perlu diubah**: CCTV dan sosok sudah mengikuti `Camera.main`, jadi otomatis
mengikuti pemain saat berjalan; HUD sudah terpisah dari inbox.

## Urutan implementasi (1 step = 1 commit, dicek di Unity)

1. Collider furnitur di `Office_Environment`.
2. Prefab pemain: karakter + gerak + animasi idle/walk.
3. Rig kamera Cinemachine (TPP + Meja) + blend.
4. `PlayerModeController` + zona duduk di meja (sembunyikan inbox, kunci kursor).
5. Petunjuk "[E]" dan notifikasi pesan baru di HUD.
6. Uji alur penuh: Menu → Jalan → Duduk → Berdiri → … → Rapor → Retry.

## Risiko yang perlu diuji (rubrik Q4)

- Kamera TPP menembus dinding di ruangan sempit (8 × 6 m).
- Kursor tetap terkunci/terlepas di saat yang salah (setelah Rapor sudah di-reset oleh `ReportView`).
- Pemain tersangkut di antara furnitur.
- Tombol **E** bentrok dengan input lain.
