# Mashhad Runner

Vertical-slice prototype for the mobile runner described in the project brief. The current prototype is an original, self-contained Unity implementation for the agreed BRT environment scope.

## What is included

- Three-lane infinite runner movement
- Procedurally extended BRT road and obstacle sections with cleanup behind the runner
- Low-poly road props, a distant city skyline, and character models from CC0 Kenney asset kits
- Original procedural BRT station modules with layered canopy, glass walls, route signage, seating, tactile platform details, and turquoise/warm-light accents
- Original roadside BRT bus model with custom body, livery, glazing, doors, mirrors, wheels, grille, and lights
- Original Mashhad-inspired limestone mid-rises with turquoise tile bands, recessed windows, balconies, parapets, and arched entrances
- Original cypress street trees and a distant shrine-inspired gold dome/minaret skyline landmark
- Expanded shrine landmark with layered turquoise iwan, segmented gilded arch, tiled facade panels, stepped gold dome drum and finial, twin detailed minarets, and forecourt arcade columns
- Original cantilever LED streetlights and custom BRT route wayfinding signs with route numbers and high-contrast labels
- Refined BRT bus hero prop with front destination board, route number, side identity labels, turquoise lower livery, roof AC housing, wheel hubs, and door framing
- Warm late-afternoon lighting, atmospheric distance fog, and tuned camera field of view
- Decorative building, station, bus, tree, streetlight, and landmark meshes are combined by material to reduce mobile draw calls
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

1. Install Unity Hub and Unity `2022.1.24f1`.
2. Open this repository as a Unity project and wait for the first asset import.
3. Open `Assets/Scenes/RunnerPrototype.unity` for the playable runner prototype, then press **Play**.
4. Open `Assets/Scenes/MashhadRunner_BRT_ArtShowcase.unity` to review the complete static BRT Mashhad environment from its dedicated showcase camera. This is an art-review scene and does not replace the playable prototype.

The playable scene is generated at runtime by `Assets/Scripts/RunnerBootstrap.cs`. The art showcase is a saved scene built from the original procedural assets in `Assets/Scripts/MashhadRunnerArtFactory.cs`. To regenerate it in the Editor, use **Mashhad Runner > Build BRT Mashhad Art Showcase**. The builder creates a separate scene and preserves a currently open saved scene.

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

The technical appendix leaves the final environments, visual style, exact Unity version, test devices, frame-rate/size target, and advertising details blank. This prototype therefore uses only the confirmed BRT environment, a generic replaceable city skyline, and clearly replaceable primitives. It does not invent additional environments or advanced advertising services.
