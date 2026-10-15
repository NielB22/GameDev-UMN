# 04 – Lighting

Prinsip lighting game ini: **hanya objek yang terkena cahaya yang terlihat — sisanya gelap.**
Itulah yang membuat suasana malam terasa nyata, bukan "seperti game".

[← Kembali ke README](../README.md)

---

## Peta: lighting diatur di mana?

| Yang ingin diubah | Tempat mengubah | Berlaku untuk |
|---|---|---|
| **Bulan, ambient, fog, langit, pantulan** | Aset **`Assets/_Game/Data/Lighting/LightingProfile_Night`** | Semua scene (Menu, Gameplay, Demo) |
| Lighting kerja (terang) | `Lighting_Night` → komponen *Scene Lighting* → **Dev Mode** | Scene yang sedang dibuka, hanya di editor |
| Lampu dalam kantor (plafon, meja) | `Office_Environment.prefab` → grup `Lighting` | Semua scene yang memakai kantor |
| Lampu luar (parkir, jalan) | `Office_Exterior.prefab` → grup `Lighting` | Semua scene yang memakai kantor |
| Reaksi lampu terhadap Threat | `ThreatLightFlicker` (Gameplay) | Gameplay |
| Lampu berkedip di menu | `MenuEffects` → `MenuLightFlicker` (MainMenu) | MainMenu |
| Efek layar (vignette, tint merah) | Global Volume + `ThreatPostProcess` | Gameplay |

Setiap scene 3D punya **satu** prefab `Lighting_Night` (berisi `SceneLighting`, `Moonlight`, `DEV_Sun`).
Jangan menambah Directional Light lain.

## Lighting Profile (pengaturan global)

Klik aset `LightingProfile_Night` di jendela Project → ubah di Inspector.
Perubahan **langsung terlihat**, juga saat Play.

| Bagian | Nilai sekarang | Fungsi / tips |
|---|---|---|
| **Moon Intensity** | 0.15 | Seberapa terang gedung & jalan di luar. Naikkan (0.25–0.4) bila gedung tidak terbaca. |
| Moon Color | biru pucat | Kontras dingin di luar vs hangat di dalam kantor. |
| Moon Elevation / Direction | 35° / 20° | Arah bulan. 20° = dari belakang kantor sehingga muka gedung seberang terkena cahaya. |
| **Moon Shadow Strength** | 1 | **Biarkan 1.** Di bawah 1, cahaya bulan bocor menembus dinding ke dalam ruangan. |
| **Ambient Sky / Equator / Ground** | hampir hitam | Cahaya rata di **semua** permukaan, termasuk dalam ruangan tertutup. Dinaikkan = area gelap jadi abu-abu. |
| Ambient Multiplier | 1 | Pengali cepat ambient (mis. 2 untuk sedikit lebih terang). |
| **Fog Color / Density** | hampir hitam / 0.035 | Objek jauh tenggelam ke gelap. Warna fog **harus mirip warna langit**. |
| Sky Color | hampir hitam | Latar kamera (langit malam). |
| Reflection Intensity | 0 | Pantulan langit di permukaan mengkilap. Tidak ada langit terang di malam hari → 0. |

**Variasi:** klik kanan aset → *Duplicate*, ubah nilainya (mis. `LightingProfile_Readable`), lalu
pasang di `Lighting_Night` → *Scene Lighting* → **Profile** untuk membandingkan.

## Dev Mode

Untuk bekerja di scene yang gelap: pilih `Lighting_Night` → centang **Dev Mode**
(matahari terang, ambient terang, tanpa fog, pantulan normal).

- Hanya di editor — **build selalu memakai lighting game**.
- File scene **selalu menyimpan lighting game**, walau disimpan saat Dev Mode aktif.
- Biasakan mematikan Dev Mode sebelum commit agar tim membuka scene dalam mode horor.

## Yang perlu dihindari

| Jangan | Kenapa |
|---|---|
| Menaikkan ambient untuk "membuat terang" | Semua bayangan ikut terangkat → kesan abu-abu dan datar. Tambah/naikkan **lampu** di tempat yang perlu. |
| Moon Shadow Strength < 1 | Cahaya bulan bocor ke dalam kantor. |
| Fog berwarna terang/abu | Objek jauh memudar ke abu-abu, bukan ke gelap. |
| Menambah Directional Light | Bertabrakan dengan bulan; satu-satunya directional adalah `Moonlight` (+ `DEV_Sun` di editor). |
| Mengubah `Assets/Settings/*` (URP asset, renderer) | Setting global untuk semua scene — diskusikan dengan tim dulu. |
