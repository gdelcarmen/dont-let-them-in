# Don't Let Them In

`Don't Let Them In` is a Unity-based iOS game project: a darkly comedic roguelike tower defense set inside a multi-story suburban house under alien attack. Players defend rooms and hallways with improvised household defenses, route control, hazards, and fast tactical repositioning.

## Project Snapshot

- Genre: Roguelike tower defense
- Platform target: iOS
- Engine: Unity `2022.3.62f1`
- Rendering: Universal Render Pipeline
- Primary language: C#
- Secondary tooling: Python asset pipeline in `dlti_asset_pipeline/`

## Core Design Pillars

- Readability at phone scale: enemies, defenses, and hazards need to parse instantly.
- Domestic improvisation: traps and weapons are built from familiar household objects.
- Route manipulation: pathing and floor layout are central to play, not just presentation.
- Strong tone contrast: cozy suburban interiors versus cold alien lighting and pressure.
- Run variety: waves, layouts, picks, and upgrades should produce different tactical problems.

## Repository Layout

```text
.
├── Assets/
│   ├── _Project/
│   │   ├── Prefabs/
│   │   ├── Scenes/
│   │   ├── ScriptableObjects/
│   │   └── Scripts/
│   └── _Tests/
├── Packages/
├── ProjectSettings/
├── docs/
├── dlti_asset_pipeline/
├── scripts/
└── tools/
```

## Main Runtime Areas

- `Assets/_Project/Scripts/Core`: game orchestration and runtime bootstrap
- `Assets/_Project/Scripts/Grid`: floor layout, graph logic, and pathing support
- `Assets/_Project/Scripts/Aliens`: alien data and behavior
- `Assets/_Project/Scripts/Defenses`: placement, combat, and defense behavior
- `Assets/_Project/Scripts/Economy`: resource and progression systems
- `Assets/_Project/Scripts/Waves`: wave sequencing and spawns
- `Assets/_Project/Scripts/Hazards`: environmental hazards and special events
- `Assets/_Project/Scripts/UI`: HUD and interface flow
- `Assets/_Project/Scripts/Utils/Editor`: editor automation and project setup tools
- `Assets/_Project/Scripts/Visuals`: visual theme and presentation helpers

## Current Scope

This repository contains two related codebases:

1. The Unity game project.
2. A standalone Python asset pipeline in `dlti_asset_pipeline/` for future image-to-3D and content production workflows.

The pipeline is structured around stable interfaces so mock implementations can be used now and real backends can be added later without changing the game-side integration points.

## Getting Started

### Requirements

- Unity Hub
- Unity `2022.3.62f1`
- iOS build support in Unity if targeting device builds
- Xcode for signing and packaging iOS builds

### Open The Project

```bash
git clone https://github.com/gdelcarmen/dont-let-them-in.git
cd dont-let-them-in
```

Open the folder in Unity Hub and allow the initial import to finish.

Primary scenes:

- `Assets/_Project/Scenes/GameScene.unity`
- `Assets/_Project/Scenes/MainMenu.unity`
- `Assets/_Project/Scenes/MetaUpgrades.unity`

## Gameplay / Feature Status

The project already includes substantial scaffolding and implementation work, including:

- multi-floor layout and pathing systems
- alien spawning and wave progression
- defense placement and combat scaffolding
- hazard systems
- economy and progression groundwork
- UI and HUD structure
- editor tooling for setup and visual-theme generation

## Testing

### Unity tests

Unity tests live under `Assets/_Tests/`.

- Edit Mode: `Assets/_Tests/EditMode`
- Play Mode: `Assets/_Tests/PlayMode`

### Python asset pipeline tests

```bash
cd dlti_asset_pipeline
python3 -m venv .venv
source .venv/bin/activate
pip install -e .[dev]
pytest tests -q
```

## Build And CI

GitHub Actions workflows are already present under `.github/workflows/` for:

- C# linting
- Unity tests
- Unity builds

Unity CI generally expects a `UNITY_LICENSE` secret when running licensed build or test steps in GitHub Actions.

Typical iOS build flow:

1. Switch the Unity build target to `iOS`.
2. Confirm `IL2CPP` is selected in Player Settings.
3. Build the Xcode project from Unity.
4. Open the generated project in Xcode.
5. Sign, archive, validate, and distribute.

## Python Asset Pipeline

The `dlti_asset_pipeline/` project is a separate, backend-agnostic pipeline for asset generation and processing. It currently includes:

- typed configuration and asset definitions
- style resolution and catalog support
- mock image generation
- mock mesh reconstruction
- post-processing and quality-gate scaffolding
- registry and experiment-log support
- preview/export helpers

Useful references:

- `dlti_asset_pipeline/README.md`
- `dlti_asset_pipeline/docs/adding_a_new_backend.md`
- `dlti_asset_pipeline/docs/gpu_environment_setup.md`
- `dlti_asset_pipeline/docs/unity_integration.md`

## Additional Docs

Project notes and runbooks live in `docs/`, including:

- `docs/app_store_metadata.md`
- `docs/balance_log.md`
- `docs/stage1_initialization_status.md`
- `docs/stage1_unity_verification_checklist.md`
- `docs/stage1_verification_runbook.md`
- `docs/stage2_wave_system_status.md`

## Development Notes

- Default branch: `main`
- Working branch prefix for focused changes: `codex/`
- Preferred commit prefixes: `feat:`, `fix:`, `chore:`, `test:`, `docs:`

## License

No license file is currently included. Unless and until one is added, treat the project code and game content as proprietary.
