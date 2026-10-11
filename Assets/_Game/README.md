# _Game — shared project content

Everything used by **more than one scene** lives here. Scene-only content stays in
`Assets/Scenes/<Scene>/` (Scripts, Prefabs, Audio, UI, ...).

```
_Game/
├── Materials/Environment/   Room materials (floor, wall, glass, ceiling lights)
├── Materials/Props/         M_CCTV_LED
├── Prefabs/Environment/     Office_Environment.prefab — the full office room (incl. Security/ CCTVs)
├── Prefabs/Props/           CCTV_Camera.prefab — wall CCTV that follows the player at high threat
└── Scripts/                 Assembly: CyberWatch.Runtime (namespace CyberWatch.*)
    ├── Core/                GameManager (flow + shift state), SceneLoader, SceneNames
    ├── Environment/         CctvCamera — idle sweep / tracking driven by ThreatLevelSystem
    ├── Messages/            MessageData ScriptableObject + enums
    ├── Session/             ShiftSession — decisions, accuracy, mistakes for the report
    └── Threat/              ThreatLevelSystem — value, stages, events
```

## Rules
- **Third-party assets live in `Assets/Asset_3D/` (models) and `Assets/Asset_2D/` (fonts)
  and are read-only** (Retro Office_V2, Security Camera, VT323, IBM Plex Mono). Place their *prefabs*
  (never the raw `.fbx`) and change things through prefab overrides in the scene.
- Do not use Retro Office prefabs whose FBX is empty (0 bytes): `RetroPC_pack`, `Paper`,
  `Box_Filled_Grey`, `Mug&Pencil_Black`. Use `Doc_Tray_Array` for document trays.
- The office room is edited **only** in `Office_Environment.prefab`; scenes use instances of it.
  Fog/ambient/skybox are per-scene (Lighting window) — copy them from `DemoScene`.
- New scene changes go through `SceneLoader` / `GameManager`, not `SceneManager` directly.
- Environment effects (lights, CCTV, silhouette, audio) subscribe to `ThreatLevelSystem`
  events instead of being called by it.
- Messages are content: create them with *Assets > Create > Cyber Watch > Message*.
