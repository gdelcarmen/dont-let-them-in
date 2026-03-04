# Stage 1 Unity Deferred Verification Checklist

This checklist captures every Unity-dependent validation that is deferred when Unity Editor/CLI or Unity license secrets are unavailable.

## Preconditions

- Unity Editor `2022.3.62f1` (or compatible 2022.3 LTS) installed with iOS build support
- GitHub Actions secrets configured for GameCI:
  - `UNITY_LICENSE` (or `UNITY_SERIAL` + `UNITY_EMAIL` + `UNITY_PASSWORD`)
- Xcode installed for iOS archive validation

## Local Unity Validation

- [ ] Open project in Unity Hub and verify no compile errors on import.
- [ ] Confirm `Stage1ProjectSetupEditor` auto-runs on load.
- [ ] Verify generated assets exist:
  - `Assets/_Project/ScriptableObjects/FloorLayouts/GroundFloor.asset`
  - `Assets/_Project/ScriptableObjects/AlienData/Grey.asset`
  - `Assets/_Project/ScriptableObjects/DefenseData/Tripwire.asset`
  - `Assets/_Project/ScriptableObjects/WaveConfigs/Wave_01.asset`
- [ ] Verify generated scenes exist:
  - `Assets/_Project/Scenes/MainMenu.unity`
  - `Assets/_Project/Scenes/GameScene.unity`
  - `Assets/_Project/Scenes/MetaUpgrades.unity`
- [ ] Verify Build Settings includes the three scenes above in this order.
- [ ] Verify iOS Player setting uses `IL2CPP` backend.
- [ ] Verify top-down orthographic camera is centered and unrotated in `GameScene`.
- [ ] Verify floor rendering and debug visuals:
  - Node grid visible
  - Entry points highlighted blue-white
  - Safe Room highlighted green
  - Node states color-coded
- [ ] Verify interaction and simulation:
  - Click/tap placement creates defense marker and deducts Scrap
  - Node state updates trigger alien path recalculation
  - Grey aliens spawn from entry points and route to Safe Room
  - Grey visuals render as placeholder big-head silhouette
  - Defenses damage/destroy aliens on node entry/range checks
  - Kills award Scrap
  - Safe Room integrity decreases on alien breach
  - Game over at zero integrity
  - Victory after wave clear
  - Restart button reloads run

## Unity Test Framework Validation

- [ ] Run EditMode tests in Unity Test Runner and confirm pass:
  - Pathfinding route correctness
  - Reroute behavior when blocked
  - No-path behavior (alien queue case)
  - Economy spend/add/insufficient-scrap checks
- [ ] Run PlayMode tests in Unity Test Runner and confirm pass:
  - Wave spawn count
  - Spawn delay behavior

## CI Validation With Unity License Enabled

- [ ] Trigger GitHub Actions on `main` after adding Unity secrets.
- [ ] Confirm `Unity Tests` workflow executes real tests (not skip path) and passes.
- [ ] Confirm `Unity Build Check` executes real GameCI build (not skip path) and passes.
- [ ] Confirm `CSharp Lint` continues passing.

## iOS Build Validation

- [ ] Build iOS player from Unity and export Xcode project.
- [ ] Open Xcode project, set signing team, and produce Archive successfully.

## Artifact/Repo Hygiene Checks

- [ ] Confirm new binary assets are tracked by Git LFS (`git lfs ls-files`) once added.
- [ ] Confirm Unity-generated `.meta` files are committed for all created assets/scenes.
- [ ] Update `experiment_log.json` outcome to `success` once all checks above are complete.
