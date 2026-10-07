# Beat the Baseline tool

This is a deterministic comparison tool, separate from Matchmaker and Match
Analyzer.

## Responsibility

The tool runs a baseline agent and a candidate agent against the same
opponent, using the same explicit seed suite, step count, and configuration.
With `--swap-seats`, it repeats each comparison with the controlled player
positions exchanged. It records:

- the source revision and all comparison inputs;
- one manifest for the challenge;
- per-match stdout, stderr, status, and a descriptive evidence report;
- per-player action counts, plant/pass decisions, crop lifecycle observations,
  requested market orders, and decision-time JSONL traces;
- aggregate baseline and candidate rewards and deltas.

Evidence that the public simulator does not expose reliably is represented as
`null`, not inferred: realized sale values and invalid/no-op action counts are
currently unavailable, and sale quantities are recorded as requested order
quantities. Each match report lists these limitations explicitly.

The result says only which side led under the selected controls. It does not
explain why, establish statistical significance, or replace Match Analyzer.

## Iterative tuning sessions

When a development agent is asked to tune a supported decision policy, use the
session runner. It runs successive rounds for one declared parameter, keeps
the same seeds, opponent, episode length, and seat controls, and writes a
manifest plus a summary for every round. Candidate wrappers use explicit
dataclass values and do not edit the live policy source.

Supported parameterized entry points are currently:

- `agents/carrot/decision.py` with the named fields in `CarrotParameters`;
- `agents/melon/decision.py` with `future_price_multiplier` or `pass_utility`;
- `agents/wheat/decision.py` with `future_price_multiplier` or `pass_utility`.

Example:

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

By default every candidate is compared with the original baseline. Add
`--auto-promote` only when the caller explicitly wants a candidate that leads
under the selected aggregate metric to become the next *in-session* baseline.
Promotion never changes agent source and is recorded for human review.

The session is evidence collection, not an automatic claim that the best
round is generally better. Review the round summaries and behavioral evidence
with Match Analyzer before changing a real policy.

## Example

```bash
.venv/bin/python \
  labs/agent-tuning-studio/tools/beat_the_baseline/run_baseline_comparison.py \
  --baseline agents/carrot/conveyor.py \
  --candidate agents/carrot/decision.py \
  --opponent pass \
  --seed 42 --seed 43 \
  --steps 720 \
  --swap-seats
```

The output directory is unique and ignored under
`labs/agent-tuning-studio/runs/`. Do not point multiple challenges at a shared
directory.

Each match contains an `evidence/report.json` file and
`evidence/decision-traces/player-<n>.jsonl` files. The traces contain only the
observations available to that player before its recorded action, plus the
action itself. They are descriptive evidence, not an evaluation verdict.

## Not this tool

- Matchmaker selects agents, runs individual matches, stores/retrieves match
  artifacts, and opens the separate visualizer.
- Match Analyzer interprets completed evidence and controlled comparisons.

This tool intentionally does not edit policy source. Its generated parameter
wrappers are isolated under the session run directory and ignored by Git.
