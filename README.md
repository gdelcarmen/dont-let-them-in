# Don't Let Them In

Home Alone. In space. Sort of.

## Game Overview

**Don't Let Them In** is a darkly comedic roguelike tower defense game for iOS.
You defend a multi-story suburban home from alien intruders using improvised defenses,
resource routing, and lane control under escalating wave pressure.

- Genre: Darkly Comedic Roguelike Tower Defense
- Platform: iOS (App Store)
- Monetization: Free-to-play, interstitial ads between runs, optional rewarded ads
- Engine: Unity LTS (URP 2D template), C#, IL2CPP for iOS

## Elevator Pitch

Aliens are outside and they want in. Draft defenses between floors, place traps and gadgets,
reroute invaders through kill zones, and protect the Safe Room. Every run changes floor layout,
wave composition, and tactical priorities.

## Technical Stack

- Unity LTS: `2022.3.62f1` (project metadata target)
- Rendering: URP + 2D-friendly top-down orthographic setup
- Language: C#
- Scripting backend target: IL2CPP for iOS (set by editor setup automation)
- CI: GitHub Actions + GameCI + dotnet-format

## Project Structure

```text
Assets/
├── _Project/
│   ├── Scripts/
│   │   ├── Core/
│   │   ├── Grid/
│   │   ├── Aliens/
│   │   ├── Defenses/
│   │   ├── Economy/
│   │   ├── Waves/
│   │   ├── Hazards/
│   │   ├── UI/
│   │   ├── Audio/
│   │   └── Utils/
│   ├── ScriptableObjects/
│   │   ├── WaveConfigs/
│   │   ├── DefenseData/
│   │   ├── AlienData/
│   │   └── FloorLayouts/
│   ├── Prefabs/
│   │   ├── Aliens/
│   │   ├── Defenses/
│   │   ├── Environment/
│   │   └── UI/
│   ├── Art/
│   │   ├── Sprites/
│   │   ├── Materials/
│   │   ├── VFX/
│   │   └── Fonts/
│   ├── Audio/
│   │   ├── Music/
│   │   └── SFX/
│   └── Scenes/
├── _Tests/
│   ├── EditMode/
│   └── PlayMode/
└── Plugins/
```

### Where Each System Lives

- Grid + pathing + debug: `Assets/_Project/Scripts/Grid`
- Alien logic and types: `Assets/_Project/Scripts/Aliens`
- Defense placement and combat hooks: `Assets/_Project/Scripts/Defenses`
- Scrap economy: `Assets/_Project/Scripts/Economy`
- Wave loop + spawn timing: `Assets/_Project/Scripts/Waves`
- Runtime orchestration: `Assets/_Project/Scripts/Core/GameManager.cs`
- HUD and restart flow: `Assets/_Project/Scripts/UI`
- Unity editor auto-setup (assets/scenes/IL2CPP): `Assets/_Project/Scripts/Utils/Editor`

## Setup Instructions

1. Clone:
   - `git clone <repo-url>`
   - `cd dont-let-them-in`
2. Install Unity LTS `2022.3.62f1` (or nearest compatible 2022.3 LTS) with iOS build support.
3. Open the project in Unity Hub.
4. On first load, the editor setup script auto-generates:
   - Ground Floor ScriptableObject assets
   - `MainMenu`, `GameScene`, `MetaUpgrades` scenes
   - iOS IL2CPP player settings
5. Open `Assets/_Project/Scenes/GameScene.unity` and press Play.

## Stage 1 Prototype Behavior

- Ground Floor grid renders with room/hallway coloring.
- Entry points are blue-highlighted; Safe Room is green.
- Node graph supports states: `Open`, `Blocked`, `HazardActive`, `Destroyed`.
- Pathfinding is waypoint-based (A*), not NavMesh.
- Clicking/tapping places a defense node (scrap cost deducted).
- Alien `Grey` spawns and follows path toward Safe Room.
- Node changes trigger alien rerouting.
- Defenses damage aliens when they enter attack range (default entry-node trap).
- HUD includes Scrap (top-left), Wave (top-center), Integrity (top-right), restart button.
- Game over at zero integrity; victory after all waves clear.

## Build Instructions (iOS)

1. In Unity: `File -> Build Settings -> iOS`.
2. Confirm `IL2CPP` in `Project Settings -> Player -> Other Settings -> Scripting Backend`.
3. Build to an Xcode project directory.
4. Open generated `.xcodeproj`/`.xcworkspace` in Xcode.
5. Select signing team, archive, and submit via Organizer.

## CI/CD

GitHub Actions workflows:

- `unity-tests.yml`
  - Uses `game-ci/unity-test-runner` for EditMode + PlayMode.
- `unity-build.yml`
  - Uses `game-ci/unity-builder` to perform `StandaloneOSX` proxy build with custom build method.
- `csharp-lint.yml`
  - Uses `dotnet-format` checks for C# style/whitespace.

Required repo secret for GameCI workflows:

- `UNITY_LICENSE`

If Unity license secrets are not configured yet, Unity build/test jobs auto-skip with a clear log message so baseline CI remains green while repository bootstrap and non-Unity checks continue.

## Agent Pipeline (10 Stages)

1. Floor Layout & Path System - **Complete (current scaffold)**
2. Alien Spawning & Wave System - Planned
3. Defense Placement & Combat - Planned
4. Hazard & Special Systems - Planned
5. Floor Progression & Multi-Floor System - Planned
6. Economy, Draft Picks & UI - Planned
7. Meta Progression - Planned
8. Asset Integration - Planned
9. Playtesting Loop - Planned
10. Store Prep - Planned

## Design Document Reference

This repository is initialized from the Stage 1 seed direction for **Don't Let Them In**.
Core loop target:

- 3-floor run progression
- Draft picks between floors
- Safe Room defense objective
- 4 defense categories (A/B/C/D)
- 3+ alien classes (Grey, Stalker, TechUnit, Overlord)

## Contribution Conventions

- Branch naming:
  - `main` for stable baseline
  - `codex/<topic-or-date>` for implementation sweeps
- Commit style:
  - Conventional commits (`feat:`, `fix:`, `chore:`, `test:`)
- PR format:
  - Summary
  - What changed
  - Test evidence (EditMode/PlayMode/CI)
  - Risks and follow-up tasks

## Experiment Log

`experiment_log.json` is a structured research artifact capturing pipeline stage actions,
outcomes, retries, and intervention notes. Update it at the end of each stage so technical
and process decisions remain auditable.

Deferred Unity validations are tracked in `docs/stage1_unity_verification_checklist.md` and should be completed before promoting Stage 1 from partial to fully verified.
Structured deferred-check state is also tracked in `docs/stage1_deferred_verification_log.json`.
Execution instructions and evidence template are available in `docs/stage1_verification_runbook.md` and `docs/stage1_verification_evidence.template.json`.

Initialization completion/deferred status for the 10-point setup checklist is tracked in `docs/stage1_initialization_status.md`.
