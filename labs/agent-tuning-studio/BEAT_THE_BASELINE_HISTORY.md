# Beat the Baseline history and recovery guide

**Status:** Evidence-backed reconstruction
**Last updated:** 2026-10-06
**Purpose:** Give a future development agent one findable record of the
historical “Beat the baseline” workflow, the experiments that survived in the
repository, and the limits of what can be recovered.

This document reconstructs the workflow from repository documentation,
notebooks, source comments, tuning handoffs, saved analysis, committed
history, generated Studio artifacts, and available session history. It does
not claim to recover experiment records that are not present.

## Executive summary

The historical workflow was an iterative, controlled tuning loop:

1. Establish a baseline source revision and parameter snapshot.
2. State one falsifiable hypothesis.
3. Select a fixed seed suite and fixed controls.
4. Run baseline and candidate under matched conditions.
5. Swap player seats when player order can affect the trajectory.
6. Record economic and behavioral evidence.
7. Change one named belief or policy parameter.
8. Repeat the identical suite.
9. Keep, revert, or mark the candidate inconclusive.
10. Continue with another round until the defined experiment is exhausted or
    no supported improvement remains.

The repository strongly documents this method and preserves several completed
multi-match suites. It does **not** preserve a complete ledger of every
historical round, parameter adjustment, and final stopping decision.

The important rule is:

> A candidate leading one match, or producing a persuasive explanation, is not
> enough to call it improved.

## What “Beat the Baseline” meant

“Beat the baseline” had two related meanings:

1. **The experimental question:** Can a more explicit policy or belief model
   outperform, or justify a trade-off against, a simpler policy under matched
   controls?
2. **The repeated workflow:** Run a baseline, change one named parameter,
   rerun the same controls, inspect the result, and use the result to choose
   the next round.

The original work was not an all-crop optimizer. The first economic question
was deliberately narrow:

> Given one empty unlocked tile and an already-owned seed, should the agent
> plant now or choose `PASS`?

The carrot comparison was the central test. The simple carrot conveyor
continuously grows carrots; the decision agent uses an explicit belief and
utility model before planting. The architecture says the decision network
must earn its added complexity against that simpler baseline.

See:

- [`carrot-decision-network.md`](../../docs/decision-networks/carrot-decision-network.md)
- [`kaggriculture-agent-architecture.md`](../../docs/architecture/kaggriculture-agent-architecture.md)
- [`AGENTS.md`](../../AGENTS.md)

## Reconstructed chronology

### 1. Early exploratory evidence

Available session history contains an August 12 Kaggriculture exploration in
which an initial melon agent beat the built-in starter in 4/4 games by about
1,983 coins on average.

That was an encouraging smoke result, not a robust baseline challenge:

- the full seed suite is not preserved in the available history;
- the seat arrangement is not fully recorded;
- the parameter snapshot is not preserved;
- there is no surviving round-by-round tuning ledger for that work.

It should be treated as an early signal that motivated controlled experiments,
not as proof of a generally stronger policy.

### 2. Explicit carrot decision network

The carrot model separated:

- fixed game facts;
- uncertain market and opponent-supply beliefs;
- policy preferences and opportunity costs;
- mechanical care and movement logic.

The initial carrot parameters included:

| Parameter | Initial value | Meaning |
|---|---:|---|
| `supply_high_given_zero_ripe` | `0.05` | High opponent supply belief with no visible ripe carrots |
| `supply_high_given_one_to_three_ripe` | `0.30` | High opponent supply belief with 1–3 visible ripe carrots |
| `supply_high_given_four_plus_ripe` | `0.70` | High opponent supply belief with 4+ visible ripe carrots |
| `low_price_multiplier` | `0.60` | Low future-price scenario |
| `high_price_multiplier` | `1.20` | High future-price scenario |
| `low_price_base` | `0.15` | Base low-price weight |
| `low_price_supply_weight` | `0.65` | Supply influence on low-price belief |
| `high_price_base` | `0.05` | Base high-price weight |
| `high_price_demand_weight` | `0.02` | Demand influence on high-price belief |
| `high_price_supply_penalty` | `0.30` | Supply penalty on high-price belief |
| `seed_opportunity_value` | `20.0` | Value of retaining an already-owned seed |
| `pass_utility` | `0.0` | Utility threshold for `PASS` |

The first explicitly recommended experiment was:

```text
baseline:
  P(high opponent supply | 1–3 ripe carrots) = 0.30

candidate:
  P(high opponent supply | 1–3 ripe carrots) = 0.35
```

The instructions explicitly prohibited changing the risk penalty, price table,
planting threshold, or care logic in the same comparison.

The decision network describes the loop as:

```text
Record baseline parameters
  -> run a fixed seed suite
  -> review score and prediction log
  -> change one parameter only
  -> repeat the identical seed suite
  -> compare the defined metric
  -> keep, revert, or mark inconclusive
```

Source: [`carrot-decision-network.md`](../../docs/decision-networks/carrot-decision-network.md).

### 3. Matched evaluation notebook

The memoization notebook contains the clearest concrete suite design.
Every substantive seed was run twice:

- decision policy as player 0;
- decision policy as player 1.

The same seed was reused for both seat assignments. This checked whether player
order and shared-market behavior affected the result.

The documented suite was:

| Suite | Seeds | Seat runs | Purpose |
|---|---:|---:|---|
| `pass` | 42–51 | 20 | Health check only |
| `random` | 42–71 | 60 | Robustness check |
| `starter` | 42–71 | 60 | Deterministic reference |
| carrot conveyor | 42–71 | 60 | Primary economic baseline |
| historical `test_agent_v0` | 42–71 | 60 | Historical reference |

Total: **260 matches**.

The notebook says to record the rows, source revision, and parameter values
before considering any parameter change. It also says to verify legal actions,
normal completion, crop survival, and inventory behavior before optimizing
reward.

Source: [`kaggriculture_playground.ipynb`](../../experiments/memoization/kaggriculture_playground.ipynb).

### 4. Memoization and replay work

The memoization work was intended to make repeated rounds cheaper. It
introduced:

- timing profiles;
- decision trace recording;
- trace replay;
- first action-divergence detection.

The surviving replay implementation is a **validation cache**, not a complete
simulator checkpoint/resume system. It can establish that a candidate produces
the same actions as a recorded baseline or identify the first differing action.
It does not restore simulator state and execute only the remaining suffix.

Sources:

- [`experiments/memoization/README.md`](../../experiments/memoization/README.md)
- [`profile_step8.py`](../../experiments/memoization/profile_step8.py)

### 5. Phase 2 carrot evidence package

The Decision Network Lab later defined a frozen evidence package for a smaller
carrot comparison:

- seeds `42`, `43`, and `44`;
- both player seats;
- 720 steps;
- fixed `pass` opponent;
- decision policy versus carrot conveyor;
- 12 total policy runs;
- compressed decision traces;
- source and policy provenance;
- paired candidate-minus-baseline deltas.

The producer deliberately emits descriptive evidence. It does not declare
that the candidate is better. Pairing is by `(seed, decision_player)`.

Sources:

- [`run_carrot_experiment.py`](../decision-network-lab/evaluation/run_carrot_experiment.py)
- [`evaluation/README.md`](../decision-network-lab/evaluation/README.md)
- [`PHASE2_EVIDENCE_CONTRACT.md`](../decision-network-lab/PHASE2_EVIDENCE_CONTRACT.md)

### 6. Phase 3 melon suite

The strongest surviving completed multi-candidate record is the melon Phase 3
suite.

Controls:

- canonical baseline: `melon_decision_v1`;
- parity control: `melon_parity_control_v1`;
- five atomic candidate hypotheses;
- seeds `42`, `43`, and `44`;
- both player positions;
- 30 simulated days;
- 720 steps per match;
- 36 completed matches;
- all matches ended with `DONE`.

Recorded results:

| Candidate | Runs | Mean candidate-minus-baseline delta | Wins | Losses | Draws |
|---|---:|---:|---:|---:|---:|
| parity control | 6 | `0.00` | 2 | 2 | 2 |
| reactive supply | 6 | `+14,237.67` | 6 | 0 | 0 |
| horizon margin | 6 | `+14,371.00` | 6 | 0 | 0 |
| glut risk | 6 | `+14,237.67` | 6 | 0 | 0 |
| conservative margin | 6 | `+14,237.67` | 6 | 0 | 0 |
| care gate | 6 | `+14,237.67` | 6 | 0 | 0 |

The result was **not** accepted as evidence that one of the five named
hypotheses was better. All five candidates shared a structural
`can_reach_planned_harvest` guard that the canonical baseline lacked.

The correct conclusion was:

> The suite identified a strong shared reachability effect, not an
> independently validated ranking of the five hypotheses.

The required next experiment was a reachability-only control using the
baseline price rule plus that guard. The atomic candidates should not be
ranked or promoted until they are compared against that control.

Sources:

- [`PHASE3_MELON_BASELINE_ANALYSIS.md`](../decision-network-lab/PHASE3_MELON_BASELINE_ANALYSIS.md)
- [`PHASE3_REACHABILITY_CONTROL_HANDOFF.md`](../decision-network-lab/PHASE3_REACHABILITY_CONTROL_HANDOFF.md)

### 7. Wheat and melon tuning handoffs

The wheat and melon handoffs formalized a more rigorous evaluator contract.
They require:

- explicit source snapshot binding;
- source hashes;
- simulator version and configuration;
- candidate injection without mutating module globals or default dataclass
  instances;
- one named parameter change;
- checkpoint/replay validation where used;
- first full action-dictionary divergence;
- independent clean candidate reruns;
- both player seats;
- lifecycle counts;
- sales;
- plant/pass counts;
- audit status.

A parameter that causes no action divergence is legitimate evidence that the
parameter did not affect behavior. It must not be reported as an improvement.

Current point-price wheat and melon policies use deterministic price estimates,
so Brier scoring is not yet valid for those point estimates. Economic reward
and belief calibration must remain separate.

Sources:

- [`watermelon-tuning-system/README.md`](../../experiments/tuning-handoffs/watermelon-tuning-system/README.md)
- [`wheat-tuning-system/README.md`](../../experiments/tuning-handoffs/wheat-tuning-system/README.md)

## Fixed controls and evidence requirements

Unless one of these is explicitly the experimental variable, hold the
following fixed:

- source revision;
- agent implementation;
- named baseline parameters;
- game rules and crop facts;
- episode length;
- game configuration;
- opponent implementation;
- seed suite;
- player position, or a declared seat-swap design;
- all unrelated parameters.

Record at least:

- final reward and bank;
- candidate-minus-baseline margin;
- win/loss/draw;
- completion status;
- crop lifecycle counts;
- crops planted, watered, harvested, lost, and sold;
- revenue and realized sale price;
- plant/pass decision counts;
- care failures;
- invalid or no-op actions when observable;
- decision traces and prediction outcomes when available.

Economic outcome and calibration are separate metrics. A favorable reward
delta does not prove that the probability model was calibrated.

## What could and could not be recovered

### Recovered with strong evidence

- The one-parameter-at-a-time experimental rule.
- Fixed seeds and matched controls.
- Seat swapping.
- The carrot conveyor as the primary baseline.
- The explicit carrot parameter table.
- The 260-match notebook suite.
- The 12-run Phase 2 carrot evidence package design.
- The completed 36-match melon suite.
- The reachability confound and required control.
- The wheat/melon evaluator handoff requirements.
- The separation between local policy execution, offline analysis, and human
  approval.

### Not recovered

No durable record was found containing a complete sequence such as:

```text
Round 1: parameter A = ...
Round 2: parameter A = ...
Round 3: parameter B = ...
Stopped because ...
```

The available repository and session-history search did not expose:

- every historical candidate value;
- every source revision used during tuning;
- every per-match result from the old agent-driven runs;
- a complete keep/revert ledger;
- a final no-improvement stopping decision;
- a complete later conversational transcript tied to this repository.

The surviving documentation is therefore a protocol and evidence trail, not a
complete database export.

## Current Studio recovery path

The current Studio has two related commands.

### One comparison

Use [`run_baseline_comparison.py`](tools/beat_the_baseline/run_baseline_comparison.py)
for one fixed baseline/candidate challenge:

```bash
.venv/bin/python \
  labs/agent-tuning-studio/tools/beat_the_baseline/run_baseline_comparison.py \
  --baseline agents/carrot/conveyor.py \
  --candidate agents/carrot/decision.py \
  --opponent pass \
  --seed 42 --seed 43 --seed 44 \
  --steps 720 \
  --swap-seats
```

### Repeated parameter rounds

Use [`run_tuning_session.py`](tools/beat_the_baseline/run_tuning_session.py)
when a development agent should test successive values for one supported
parameter:

```bash
.venv/bin/python \
  labs/agent-tuning-studio/tools/beat_the_baseline/run_tuning_session.py \
  --agent agents/carrot/decision.py \
  --parameter supply_high_given_one_to_three_ripe \
  --candidate-value 0.25 \
  --candidate-value 0.35 \
  --candidate-value 0.40 \
  --opponent pass \
  --seed 42 --seed 43 --seed 44 \
  --steps 720 \
  --swap-seats
```

Supported parameterized entry points are currently:

- `agents/carrot/decision.py`;
- `agents/melon/decision.py`;
- `agents/wheat/decision.py`.

The runner writes ignored records under:

```text
labs/agent-tuning-studio/runs/tuning-sessions/<session-id>/
```

Each round contains:

- `manifest.json`;
- generated baseline and candidate wrappers;
- runner stdout and stderr;
- `summary.json`;
- a link to the matched challenge directory under
  `runs/baseline-challenges/`.

Each matched child run also contains `evidence/report.json` and one
pre-action JSONL trace per player. These reports include action counts,
plant/pass decisions, observed crop lifecycle losses, active crops, requested
market orders, and quoted sale value. Realized sale values and invalid/no-op
counts are explicitly unavailable from the current public simulator step
records, so they remain `null` rather than being estimated.

By default, all candidates are compared with the starting baseline. The
optional `--auto-promote` flag carries a candidate-leading round into the next
in-session baseline only. It never edits policy source and never constitutes a
general improvement claim.

### Current Studio validation run

On 2026-10-07, the new evidence-producing runner exercised the documented
carrot experiment:

- source revision: `6f5312e201e69903d36473aba71044456e2a337e`;
- parameter: `supply_high_given_one_to_three_ripe`;
- checked-in baseline: `0.30`;
- candidates: `0.25`, `0.35`, and `0.40`;
- opponent: built-in `pass`;
- seeds: `42`, `43`, and `44`;
- 720 steps per match;
- both player seats;
- hypothesis: a higher high-supply belief would reduce premature planting
  and improve reward.

All three rounds completed successfully. Each round contained six paired
comparisons, every candidate delta was `0.0`, and no candidate produced an
action divergence from its matched baseline. The observed decision counts
and crop-loss metrics were unchanged. This is valid no-divergence evidence,
not evidence that any candidate improved the policy.

The persisted session is under
`runs/tuning-sessions/20261007T015319Z-0f3ff32f7466/`. The next experiment
should choose a parameter and evidence horizon that can actually affect
actions under the selected opponent and seed suite, or retain this result as
the documented null round.

## Responsibilities of the three Studio surfaces

Keep the workflow separated:

1. **Matchmaker** selects agents, configures and runs individual matches,
   stores and retrieves match records, and locates or opens replays.
2. **Beat the Baseline** runs fixed, matched baseline/candidate challenges and
   repeated one-parameter tuning sessions.
3. **Match Analyzer** interprets completed evidence, checks confounds, reviews
   behavioral metrics, and recommends whether a candidate is worth another
   controlled experiment.

The Beat the Baseline runner collects evidence. It should not silently edit
the game agent, reinterpret game rules, or declare a candidate generally
better.

## Recommended next experiment

To recreate the historical workflow faithfully:

1. Choose one current agent and one documented parameter.
2. Record its checked-in default and source revision.
3. Write a falsifiable hypothesis and expected metric.
4. Use seeds `42`, `43`, and `44` with both seats for a quick controlled
   suite, or `42` through `71` for the substantive notebook-sized suite.
5. Run the candidate sequence with
   [`run_tuning_session.py`](tools/beat_the_baseline/run_tuning_session.py).
6. Inspect every round, including ties and no-divergence results.
7. Use Match Analyzer to review economic and behavioral evidence.
8. Keep, revert, or mark inconclusive with a written reason.
9. Only then choose the next parameter or candidate value.

Do not treat the current runner's aggregate `candidate_leads` label as
automatic promotion. It is a controlled result under a selected suite, not a
claim about all future games.
