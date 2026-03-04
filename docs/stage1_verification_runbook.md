# Stage 1 Verification Runbook

Use this runbook to close all deferred Stage 1 checks when Unity tooling is available.

## Scope

- Pipeline stage: `stage1_floor_layout_path_system`
- Baseline repository: `https://github.com/gdelcarmen/dont-let-them-in`
- Baseline commit before this runbook: `832a7532b36118206d0841525008baf9d76ee241`

## Inputs Required

- Unity `2022.3.62f1` (or compatible 2022.3 LTS) with iOS module
- Xcode with signing access
- GitHub repo admin access for Actions secrets
- Existing checklist/log files:
  - `docs/stage1_unity_verification_checklist.md`
  - `docs/stage1_deferred_verification_log.json`
  - `docs/stage1_verification_evidence.template.json`

## Execution Steps

1. Create a verification branch from latest `main`.
2. Open project in Unity Hub and wait for import to complete.
3. Resolve compile/import issues (if any) before running tests.
4. Confirm stage setup auto-generation:
   - ScriptableObject assets generated
   - Scenes generated and added to Build Settings
   - iOS IL2CPP settings applied
5. Run Play mode validation in `GameScene`:
   - Camera orientation and framing
   - Node visualization and state color coding
   - Defense placement and path rerouting
   - Grey spawn/combat/death behavior
   - Scrap, wave, integrity HUD updates
   - Game over/victory/restart behavior
6. Run Unity Test Runner:
   - EditMode tests
   - PlayMode tests
7. Configure or verify GitHub secrets:
   - `UNITY_LICENSE` or `UNITY_SERIAL` + `UNITY_EMAIL` + `UNITY_PASSWORD`
8. Push verification branch and confirm Actions:
   - `Unity Tests` runs real test path and passes
   - `Unity Build Check` runs real build path and passes
   - `CSharp Lint` still passes
9. Build iOS target from Unity and archive in Xcode.
10. Capture all evidence in `docs/stage1_verification_evidence.json` using the template.
11. Update:
   - `docs/stage1_deferred_verification_log.json` statuses from `deferred` to `passed` (or `failed`)
   - `experiment_log.json` outcome to `success` if all checks pass
   - `docs/stage1_initialization_status.md` deferred section to reflect closure

## Exit Criteria

- Every item in `docs/stage1_unity_verification_checklist.md` checked.
- Every check in `docs/stage1_deferred_verification_log.json` is no longer `deferred`.
- Evidence file completed and committed.
- CI green on `main` with real Unity execution path.

