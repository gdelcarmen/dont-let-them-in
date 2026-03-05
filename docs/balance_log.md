## Balance Pass — March 5, 2026

### Baseline Playtest Data (Stage9A)
- Run 1 (`TrapHeavy`): defeat, `floorsCleared=0`, `floorsLost=2`, `kills=4`, `scrapEarned=38`
- Run 2 (`Balanced`): defeat, `floorsCleared=0`, `floorsLost=2`, `kills=16`, `scrapEarned=84`
- Run 3 (`TechHeavy`): defeat, `floorsCleared=0`, `floorsLost=2`, `kills=2`, `scrapEarned=23`

Observed issues from wave/floor logs:
- Ground Floor and Upper Floor were collapsing around mid-floor wave pressure (often before clearing wave sets).
- Tech-heavy was over-punished and economically starved before it could stabilize.
- Balanced was strongest (intended), but still failed to clear a floor in sampled runs.

### Change: Passive wave completion income `4 -> 5`
**Reason:** Baseline runs showed resource starvation before mid-floor adaptation. +1 passive income per cleared wave improves recovery without inflating kill-reward scaling.

### Change: Grey `HP 18 -> 16`, `Speed 1.6 -> 1.4`
**Reason:** Early-wave baseline pressure forced floor loss too quickly in trap-heavy and tech-heavy strategies. Slightly lowering Grey durability/speed keeps floor 1 approachable.

### Change: Stalker `HP 26 -> 24`, `Speed 2.1 -> 1.8`
**Reason:** Stalker mobility was creating frequent breach spikes in wave 2/3 transitions. Lower speed keeps reveal/counter windows meaningful.

### Change: Tech Unit `HP 34 -> 32`, `Speed 1.35 -> 1.2`
**Reason:** Tech units were surviving long enough to produce compounding pressure before balanced defenses stabilized. Small reductions preserve identity while smoothing difficulty.

### Change: Shotgun Mount `AttackInterval 1.2 -> 1.0`
**Reason:** Balanced logs showed weapon throughput lagging behind floor pacing. Faster cadence raises sustained DPS without changing per-shot damage or cost band.

### Change: Roomba `Damage 5 -> 6`, `AttackInterval 0.8 -> 0.7`
**Reason:** Tech-heavy runs underperformed heavily. This is a modest Smart Home Tech throughput bump while still leaving surge/hacking counters in place.

### Change: Ground floor wave counts reduced
- Wave 1: `3` (from 4-5 range in baseline assets)
- Wave 2: `3 + 1`
- Wave 3: `2 + 1`
- Wave 4: `3 + 2 + 1`
- Wave 5: `3 + 2`

**Reason:** Ground floor should be broadly survivable. Baseline floor 1 frequently failed before the intended adaptation loop.

### Change: Upper floor wave counts reduced
- Wave 1: `2 + 1`
- Wave 2: `3 + 1`
- Wave 3: `2 + 2 + 1`
- Wave 4: `3 + 2`
- Wave 5: `4 + 2`

**Reason:** Upper floor should escalate from Ground without instantly converting to retreat-only outcomes.

### Change: Attic wave counts reduced (still highest pressure)
- Wave 1: `2 + 1`
- Wave 2: `3 + 1`
- Wave 3: `4 + 2`
- Wave 4: `4 + 3`
- Wave 5: `5 + 3`

**Reason:** Attic remains the hardest floor, but baseline data indicated near-immediate collapse for non-balanced strategies.

### Data Consistency Work
- Mirrored the tuned values in both runtime factory fallbacks (`Stage1DataFactory`) and ScriptableObject assets under:
  - `Assets/_Project/ScriptableObjects/AlienData/`
  - `Assets/_Project/ScriptableObjects/DefenseData/`
  - `Assets/_Project/ScriptableObjects/WaveConfigs/`

### Verification Status
- Stage9A baseline runs were captured successfully and used for this pass.
- Post-change verification runs were blocked by Unity MCP transport instability in this session (HTTP bridge startup failures at `127.0.0.1:8080`).
- Follow-up required: rerun two post-tuning campaign simulations (`Balanced`, `TrapHeavy`) once MCP transport is healthy.
