# Stage 2 Status: Alien Spawning & Wave System

Date: 2026-03-04
Scope: Stage 2 of the 10-stage pipeline

## Implemented

- Wave model extensions:
  - `WaveConfig.PreWaveDelay`
  - `WaveConfig.PostWaveDelay`
  - `WaveSpawnDirective.EntryPointSelection`
- Entry-point selection strategies:
  - `Fixed`
  - `RoundRobin`
  - `Random`
- WaveSpawner runtime upgrades:
  - Multi-wave start/completion flow with per-wave delays
  - Alien subtype spawning via `AlienFactory` (no longer hard-coded to `GreyAlien`)
  - Additional events: `WaveStarted`, `WaveCompleted`, `AlienSpawned`
- Alien subtype placeholders:
  - `StalkerAlien`
  - `TechUnitAlien`
  - `OverlordAlien`
- Stage runtime data expanded:
  - Added Stalker + TechUnit `AlienData` runtime factories
  - Added multi-wave mixed-alien runtime set
- Editor scaffold generation expanded:
  - `Stalker.asset`, `TechUnit.asset`
  - `Wave_01.asset`, `Wave_02.asset`, `Wave_03.asset`
- PlayMode tests expanded:
  - Spawn count + delay
  - Spawned subtype verification
  - Round-robin entry selection verification

## Deferred Verification

Runtime/Unity-dependent checks are deferred and tracked in:

- `docs/stage2_wave_system_deferred_log.json`

## Current Outcome

- Status: `partial`
- Reason: Unity editor/runtime validation still required for full Stage 2 closure.
