# _Game — konten bersama

Folder ini berisi semua yang **dipakai lebih dari satu scene**: script, prefab, material, data.
Isi khusus satu scene ada di `Assets/Scenes/<Scene>/`.

Dokumentasi lengkap ada di folder `docs/` (root repo, di luar `Assets`):
- `docs/01-struktur-folder.md` — arti setiap folder, aturan aset, konvensi nama
- `docs/02-membuat-scene-baru.md` — checklist membuat scene (lighting, UI, pindah scene)

Ringkas:

```
_Game/
├── Data/Messages/        MessageData (pesan email/SMS)
├── Data/Lighting/        LightingProfile_Night — pengaturan lighting global (bulan, ambient, fog, langit)
├── Materials/            Environment/, Props/
├── Prefabs/Environment/  Office_Environment (+ Office_Exterior, CCTV), Lighting_Night
├── Prefabs/Props/        CCTV_Camera
└── Scripts/              CyberWatch.Runtime: Core, Environment, Messages, Player, Session, Threat, UI
```
