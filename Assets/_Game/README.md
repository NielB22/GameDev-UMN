# _Game — konten bersama

Folder ini berisi semua yang **dipakai lebih dari satu scene**: script, prefab, material, audio, data.
Isi khusus satu scene ada di `Assets/Scenes/<Scene>/`.

Dokumentasi lengkap ada di folder `docs/` (root repo, di luar `Assets`):
- `docs/01-struktur-folder.md` — arti setiap folder, aturan aset, konvensi nama
- `docs/02-membuat-scene-baru.md` — checklist membuat scene (lighting, UI, pindah scene)

Ringkas:

```
_Game/
├── Audio/                Ambience/, SFX/, UI/, Jumpscare/ (didaftarkan di Data/Audio/AudioLibrary)
├── Data/                 Audio/AudioLibrary, Lighting/LightingProfile_Night, Messages/ (36 pesan)
├── Materials/            Effects/, Environment/, Props/
├── Prefabs/Environment/  Office_Environment (+ Office_Exterior, CCTV), Lighting_Night
├── Prefabs/Props/        CCTV_Camera
├── Settings/             PP_Night (post-process)
├── Shaders/              VHS
└── Scripts/              CyberWatch.Runtime: Audio, Core, Effects, Environment, Messages,
                          Player, Session, Threat, UI
```
