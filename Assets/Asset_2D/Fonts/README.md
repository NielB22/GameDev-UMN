# Fonts — Cyber Watch: Night Shift

Font eksternal (pihak ketiga, **read-only**). Semua dari Google Fonts, lisensi **SIL Open Font
License (OFL)** — gratis untuk game, termasuk komersial. Simpan file `OFL.txt` di folder font.

| Font | Dipakai untuk | Folder |
|---|---|---|
| **VT323** | Judul, jam shift, label Threat Level, angka besar | `VT323/` |
| **IBM Plex Mono** | Isi email/SMS, panel, tombol, teks kecil | `IBM_Plex_Mono/` |

## Struktur akhir

```
Asset_2D/Fonts/
├── README.md
├── VT323/
│   ├── VT323-Regular.ttf
│   ├── OFL.txt
│   └── VT323-Regular SDF.asset          ← dibuat di Unity (langkah 3)
└── IBM_Plex_Mono/
    ├── IBMPlexMono-Regular.ttf
    ├── IBMPlexMono-Bold.ttf
    ├── OFL.txt
    ├── IBMPlexMono-Regular SDF.asset    ← dibuat di Unity (langkah 3)
    └── IBMPlexMono-Bold SDF.asset       ← dibuat di Unity (langkah 3)
```

Cukup **Regular** (+ **Bold** untuk Plex Mono). Jangan masukkan semua ketebalan
(Thin, Light, Italic, ...) — tidak dipakai dan menambah ukuran build.

## Cara impor

### 1. Unduh
- VT323: https://fonts.google.com/specimen/VT323 → **Get font** → **Download all**
- IBM Plex Mono: https://fonts.google.com/specimen/IBM+Plex+Mono → **Get font** → **Download all**

Ekstrak file `.zip`-nya (di luar folder proyek, mis. di Downloads).

### 2. Salin ke proyek
Salin **hanya** file berikut ke folder masing-masing (lewat Explorer atau drag ke jendela Project Unity):
- `VT323-Regular.ttf` + `OFL.txt` → `Assets/Asset_2D/Fonts/VT323/`
- `IBMPlexMono-Regular.ttf`, `IBMPlexMono-Bold.ttf` + `OFL.txt` → `Assets/Asset_2D/Fonts/IBM_Plex_Mono/`

### 3. Buat TMP Font Asset (di Unity)
UI memakai **TextMeshPro**, yang butuh *Font Asset* (bukan `.ttf` langsung).
1. Jika diminta, klik **Import TMP Essentials** (atau menu *Window > TextMeshPro >
   Import TMP Essential Resources*). Cukup sekali per proyek.
2. Di jendela Project, **klik kanan** file `.ttf` → **Create > TextMeshPro > Font Asset > SDF**.
   File `... SDF.asset` muncul di folder yang sama. Ulangi untuk setiap `.ttf`.
3. Biarkan *Atlas Population Mode* = **Dynamic** (default): karakter dibuat otomatis saat
   dipakai, ukuran file tetap kecil.

Selesai — font siap dipilih di komponen *TextMeshPro - Text (UI)*.

## Aturan
- Jangan ubah/ganti nama file `.ttf` setelah diimpor (Font Asset merujuk ke file ini).
- Font baru: buat subfolder sendiri dengan pola yang sama + sertakan lisensinya.
