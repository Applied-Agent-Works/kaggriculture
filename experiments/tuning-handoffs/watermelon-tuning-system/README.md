# watermelon-tuning-system: agent handoff

## Status and destination

Prepared in Kaggriculture on 2026-09-24 for transfer to
`/home/experiments/frontier-week/foundry-trainer/watermelon-tuning-system`.
That sibling project was unavailable to this session, including an escalated
read attempt. Its carrot tuning implementation has not been inspected.
This folder contains documentation and an unchanged source snapshot, not an
implemented tuner. Keep the original Kaggriculture files in place.

The game identifier is `MELON`; "watermelon" means the game's MELON crop.

## Start here

1. Read the destination project's applicable AGENTS.md and carrot tuner docs.
2. Inspect its actual evaluator and checkpoint/resume interfaces before
   deciding what can be reused. Preserve its existing changes and artifacts.
3. Read [crop network](source/melon-decision-network.md),
   [game rules](source/README.md), and
   [refinement boundaries](source/foundry-refinement-architecture.md).
4. Check [source hashes](SOURCE_SHA256SUMS) against the copied files and read
   [source revision](SOURCE_REVISION.txt). The hashes identify the actual
   working-tree snapshot, including files untracked at the source revision.
5. Report the proposed layout and validation plan, then implement the smallest
   local end-to-end evaluation. Do not start candidate search before the
   checkpoint/resume correctness proof passes.

## Included code and explicit target binding

The `source/` folder preserves these import paths:

- `agents/one_time_crop.py`: shared loop, utility, and parameters.
- `agents/melon/shared.py`: crop constants and policy adapters.
- `agents/melon/decision.py`: default decision entry point.
- `agents/melon/conveyor.py`: simpler baseline.
- Package initializer files and the local match runner.

Bind the evaluator explicitly to this snapshot or an explicitly configured
Kaggriculture checkout. Record which one supplies the policy and which supplies
the simulator. Never infer the target from the current directory or whichever
generic `agents` package happens to import. Verify resolved module paths.
Isolate targets in separate processes or use a documented package-loading
strategy; sibling modules named `shared` can collide in the import cache.

The included runner uses installed `kaggle_environments`. The copied local
game source is a reference: its presence does not mean that runner imports it.
Record and verify the actual simulator implementation, version, and config.

## Current policy and candidate injection

These are one-active-crop agents. The decision policy uses a point estimate:

`utility = 6 * current_quote * future_price_multiplier - 80`

It plants only when utility is strictly above `pass_utility` and other
gates allow investment. Existing crops still receive care if investment stops.
There is no probabilistic sale-price model yet.

Initial parameters:
- `future_price_multiplier = 1.0`: provisional future-price assumption.
- `pass_utility = 0.0`: policy preference, distinct from a belief.
- `season_days = 30`: horizon configuration; do not tune it as a belief.

The crop wrapper currently uses defaults and accepts only an observation.
To evaluate a candidate without editing policy files, call
`agents.one_time_crop.decision_agent(obs, MELON, parameters)`, importing
`MELON` from the explicitly bound `agents.melon.shared` and constructing
`PointPriceParameters` from that same source snapshot. Keep crop facts fixed.
Do not mutate module globals or the default dataclass instance.

A useful first experiment asks whether changing only the price multiplier
changes any action at all. Many multipliers may yield identical decisions.
No divergence is a legitimate result; do not report it as improved performance.

## Correctness before tuning

The supplied code has not had match-level validation in this session.
First verify direct file loading, package loading, and a complete crop lifecycle:
buy seed, plant, water, harvest, transfer to shed, sell, and repeat.

Characterize these source-visible limitations before interpreting scores:

- No hour-of-day planting guard: a seed planted on the final turn of a day
  cannot receive the following WATER action that day.
- No allowance for delivering and selling a last-day harvest in the horizon gate.
- Helpers use the default season horizon even if a different season_days value
  reaches the top-level decision; use defaults for the first proof.
- The policy services only its first active crop, and does not clear weeds.
- Current-price persistence ignores future supply/demand and batch sale slippage.

Report defects separately from parameter experiments. Do not silently patch
the original policy while claiming a matched parameter-only comparison.

## Evaluation and checkpoint/resume proof

Use a small fixed seed-and-seat suite, a deterministic opponent, and frozen
game configuration. Record a baseline using full clean simulations first.

A checkpoint must preserve all state required to continue identically:
simulator state and step history as needed, RNG state, game configuration,
private inventories, town/market state, and any opponent or policy state.
Recorded observations alone are not checkpoints.

Replay candidate decisions against recorded observations until the first full
action-dictionary divergence (farmer, hands, and market). If no action differs,
reuse the baseline only after validating the policy is stateless and RNG-free.
On divergence, restore the state immediately BEFORE that action, execute the
candidate action and opponent action using normal turn order, and simulate
forward with the candidate. Do not follow the old observation stream afterward.

Validate against independent full clean candidate reruns for:
- no divergence;
- deliberately late divergence;
- early divergence;
- both player seats.

Compare trajectories or state hashes, final bank/reward, and terminal statuses,
not just win/loss. Retain full-suite audits alongside the cached fast path.

## Evidence and boundaries

Record source revision AND hashes, simulator/dependency versions, config,
opponent identity, seeds, seats, old/new parameter values, first divergence,
suffix length, outcome, lifecycle counts, sales, plant/pass counts, and audit
status. Change one named parameter per experiment. Store generated results and
caches separately from source snapshots and ignore disposable output in git.

Evaluate financial outcomes separately from forecast quality. Do not use
Brier score until a probability distribution and resolving labels actually
exist; the current policy makes a point forecast.

All work is local. No external APIs, cloud provisioning, credentials, paid
models, connectors, or automatic mutation of the original game agents.
Human review and matched local evidence govern any promotion of a candidate.

