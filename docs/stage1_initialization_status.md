# Stage 1 Initialization Status (2026-03-04)

Repository: https://github.com/gdelcarmen/dont-let-them-in

This matrix maps the requested initialization checklist to the repository state, and separates completed work from deferred Unity verification.

## Checklist Status

| Item | Status | Evidence |
| --- | --- | --- |
| 1. Repository initialization (`dont-let-them-in`, Unity `.gitignore`, LFS patterns) | done | `.gitignore`, `.gitattributes`, remote repo exists |
| 2. Unity project setup (2D URP structure, iOS IL2CPP configuration path) | done (scaffolded), deferred (runtime verify) | `Assets/_Project/...` tree, `Stage1ProjectSetupEditor.ConfigureIosIl2Cpp()` |
| 3. Floor layout + node graph + pathing + debug visualization | done (implemented), deferred (Unity runtime verify) | `Assets/_Project/Scripts/Grid/*`, `GameManager`, `FloorRenderer`, `GridDebugDrawer` |
| 4. Minimal playable prototype loop | done (implemented), deferred (Unity runtime verify) | `GameManager`, `WaveSpawner`, `DefensePlacementController`, `HUDController`, `GreyAlien` |
| 5. ScriptableObject data architecture | done (types + generation) | `WaveConfig`, `DefenseData`, `AlienData`, `FloorLayout`, `Stage1DataFactory`, editor setup |
| 6. Test framework (EditMode + PlayMode tests) | done (tests authored), deferred (Unity runtime verify) | `Assets/_Tests/EditMode/*`, `Assets/_Tests/PlayMode/*`, asmdefs |
| 7. CI/CD (GameCI build/tests + C# lint) | done | `.github/workflows/*.yml`; latest main runs green |
| 8. Experiment log initialized and maintained | done | `experiment_log.json` |
| 9. README coverage (overview, stack, structure, setup, build, pipeline, conventions) | done | `README.md` |
| 10. Commit/push/verify/tag/release | done | initial commit `e65fdea`, tag/release `v0.0.1`, latest branch sync |

## Current CI Evidence (main @ `71c21a3`)

- Unity Build Check: https://github.com/gdelcarmen/dont-let-them-in/actions/runs/22654704941 (`success`)
- Unity Tests: https://github.com/gdelcarmen/dont-let-them-in/actions/runs/22654704944 (`success`)
- CSharp Lint: https://github.com/gdelcarmen/dont-let-them-in/actions/runs/22654704945 (`success`)

## Deferred Unity Verification

Unity-dependent checks are intentionally deferred until Unity Editor and/or licensed GameCI execution is available. Full checklist:

- `docs/stage1_unity_verification_checklist.md`

## Exit Condition for Stage 1 = Fully Verified

Mark Stage 1 as fully verified only after every item in `docs/stage1_unity_verification_checklist.md` is checked and `experiment_log.json` is updated from `partial` to `success`.
