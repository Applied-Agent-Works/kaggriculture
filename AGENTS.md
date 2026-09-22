# Kaggriculture Agent Instructions

## Purpose

This repository develops readable Kaggriculture agents as a practical way to
study symbolic AI, probability, Bayesian belief networks, influence diagrams,
and decision theory. The aim is not a black-box LLM game player. The aim is an
explicit local policy whose assumptions, inferences, and trade-offs can be
inspected and improved through controlled experiments.

Read these documents before changing architecture or decision logic:

1. `README.md` — canonical Kaggriculture rules, action interface, and game
   configuration.
2. `GETTING_STARTED.md` — building an agent, local testing, and Kaggle
   workflow.
3. `kaggriculture-agent-architecture.md` — the agent's current and planned
   architecture, decision networks, experiment method, and invariants.
4. `carrot-decision-network.md` — the first concrete carrot influence diagram
   and its current assumptions.
5. `foundry-refinement-architecture.md` — the separate Foundry-based offline
   refinement system and its boundaries.
6. `TEMP_MEMORY.md` and `TEMP_HANDOFF.md` — temporary historical context;
   consult them when reconstructing decisions that are not yet reflected in
   the durable architecture documents.

## Current project state

The current implementation is deliberately narrow:

- `agents/carrot/shared.py` contains carrot facts, named belief/policy
  parameters, inference functions, expected utility, and shared care/movement
  helpers.
- `agents/carrot/conveyor.py` exposes the unconditional carrot baseline.
- `agents/carrot/decision.py` exposes the probabilistic plant-versus-pass
  policy.
- Root `main.py` is the Kaggle submission entry point for the decision policy.
- `run_match.py` runs local matches and accepts `--seed` for reproducibility.
- `agents/archive/` retains historical implementations; do not delete them
  without explicit user approval.

The first economic question is intentionally only:

> Given one empty unlocked tile and an already-owned carrot seed, should the
> agent plant a carrot now or choose `PASS`?

Do not silently expand this into an all-crop, all-action optimizer.

## Architectural invariants

1. Keep game facts, probabilistic beliefs, and policy preferences separate.
2. Game rules are fixed facts, not parameters to tune after a loss.
3. State uncertain assumptions as named probabilities, distributions, or
   documented heuristics. Do not bury them in opaque control flow.
4. A policy decision needs an explicit alternative, utility comparison, or
   constraint.
5. Care-critical work (watering, feeding, preservation) takes priority over
   speculative investment unless a documented network changes that policy.
6. The game-time agent must run locally with no Foundry, JEV, network, or LLM
   dependency.
7. Keep a simpler baseline when adding reasoning, so the added complexity can
   earn its place.
8. Use controlled, reproducible experiments. Change one named belief or policy
   parameter at a time while holding seed suite, configuration, opponent, and
   other parameters fixed.
9. Evaluate economic outcomes and belief calibration separately.
10. Preserve the local agent's readability and teaching value over premature
    generalization.

## Experiment protocol

Before modifying a belief or policy parameter:

1. Record the baseline source revision and parameter values.
2. Define one falsifiable hypothesis and the metric expected to change.
3. Use a fixed seed suite; swap player positions when relevant.
4. Hold game configuration, opponent, and all unrelated parameters constant.
5. Record final bank/reward, win/loss, crop lifecycle counts, sales, and
   decision counts.
6. Where predictions resolve, score their calibration (for example, Brier
   score) independently from match reward.
7. Keep, revert, or mark the candidate inconclusive with an evidence-backed
   rationale.

Do not call a parameter "improved" because of one lucky match or a persuasive
explanation.

## Foundry refinement boundary

Foundry is an offline, advisory refinement tool. It may inspect structured run
summaries and decision traces, explain possible failure modes, and propose one
falsifiable next experiment. It must not choose a live game action, mutate the
agent, reinterpret game rules, or declare a change successful without matched
local results.

No external model, evaluator, search, storage, or other service API may be
used. The project has no supplied keys, tokens, usage allocation, or spending
budget for external calls. Do not provision Azure resources, add credentials,
call paid models, or add external connectors. Treat any later exception as a
new explicit architecture decision that must name the provider and budget.

## Working conventions

- Inspect the relevant implementation, documents, and `git status` before
  editing. The worktree may contain user changes.
- Preserve unrelated changes and historical material.
- Use `apply_patch` for local edits.
- Prefer `rg` for file and text discovery.
- Run the smallest relevant verification after changes. Use `.venv/bin/python`
  when the system `python` executable is unavailable.
- Do not submit to Kaggle, publish code, create cloud resources, or alter
  credentials without explicit user authorization.
- Update the relevant architecture or decision document when a material
  architectural assumption changes.
- End substantive work with what changed, what was verified, what remains
  uncertain, and the next smallest useful action.

## Agent handoff expectation

Future agent sessions should be able to understand the project by reading this
file and the four durable documents named above. If a material decision occurs
only in conversation, record it in the appropriate durable document before
depending on it in later work.
