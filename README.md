# Mashhad Runner

Vertical-slice prototype for the mobile runner described in the project brief. The current prototype is an original, self-contained Unity implementation centered on a Mashhad-inspired BRT route.

## What is included

- Three-lane infinite runner movement
- Procedurally generated BRT, underpass, metro, and ring-road route zones with cleanup behind the runner
- An Imam Reza shrine landmark, procedural sky dome, soft clouds, and layered mountain horizon
- Low-poly road props, a distant city skyline, and character models from CC0 Kenney asset kits
- Automatic forward movement
- Lane switching, jump, and slide
- Reusable primitive-based BRT environment
- Coins and collectible letters for the `BRT` word
- Obstacles and simple vehicles
- A player character and a pursuing agent with basic animation
- Collision, lives, records, and safe-point continue
- One rewarded-ad continue placeholder per run
- On-screen runtime HUD and controls

No external assets, brands, copied game content, server, in-app purchase, or real-world rewards are included.

## Open in Unity

1. Install Unity Hub and Unity `2022.1.24f1` (the editor currently used to validate this project; the technical appendix does not mandate a version).
2. Open this repository as a Unity project.
3. Open `Assets/Scenes/RunnerPrototype.unity`.
4. Press **Play** in the Unity Editor.

The scene can be generated at runtime by `Assets/Scripts/RunnerBootstrap.cs`, so no binary scene asset is required. On first open, wait for Unity to import the project and scripts.

## Controls

| Action | Keyboard |
| --- | --- |
| Move left | Left Arrow / A |
| Move right | Right Arrow / D |
| Jump | Up Arrow / W / Space |
| Slide | Down Arrow / S / Left Ctrl |

Touch controls are represented by on-screen buttons.

## Android

Use **File > Build Settings**, select **Android**, switch the platform, and build the project. The contract defines Android as the base platform. iOS packaging and store publication are outside this prototype.

Android settings in `ProjectSettings/ProjectSettings.asset` use portrait orientation and 1080x1920 window defaults. The technical appendix did not finalize test devices, frame-rate, or size targets.

## Project structure

- `Assets/Scripts/` - gameplay, world generation, UI, and bootstrap code
- `Assets/Resources/KenneyCityRoads/`, `Assets/Resources/KenneyCityCommercial/`, and `Assets/Resources/KenneyMiniCharacters/` - selected CC0 models and licenses
- `Assets/Scenes/` - scene note and target scene
- `ProjectSettings/` - Unity project configuration
- `Packages/` - Unity package manifest

## Placeholder decision boundaries

The technical appendix leaves the final environments, visual style, exact Unity version, test devices, frame-rate/size target, and advertising details blank. The current route zones, skyline, shrine landmark, and sky are original, replaceable prototype visuals rather than a finalized art direction. No advanced advertising services are included.
