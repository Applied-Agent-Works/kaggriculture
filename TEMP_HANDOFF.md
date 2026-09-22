# Temporary Handoff — Kaggriculture Carrot Agents

## Immediate status

The workspace is being reorganized around small, readable agents that teach
symbolic and probabilistic AI through Kaggriculture.

The carrot implementation was partially reorganized successfully:

```text
agents/
  __init__.py
  carrot/
    __init__.py
    shared.py       # crop facts, belief model, care/movement helpers
    decision.py     # belief-based policy; exposes agent(obs)
    conveyor.py     # unconditional baseline; exposes agent(obs)
```

`shared.py` contains the reusable implementation. `decision.py` and
`conveyor.py` have a package import plus a direct-file fallback import, so
they should work both through the root submission entry point and when the
local runner loads their file paths.

## Remaining reorganization work

The following files were intended to be archived, but have **not** been moved:

```text
main.py        -> agents/archive/main_v0.py
test_agent.py  -> agents/archive/test_agent_v0.py
```

After preserving the old `main.py`, replace the root file with:

```python
"""Current Kaggle submission entry point: carrot decision agent."""

from agents.carrot.decision import agent
```

Create `agents/archive/` if it does not yet exist. Do not delete either old
file: they are useful historical/teaching references.

## Critical environment blocker

Every command issued by this agent’s terminal bridge fails before execution:

```text
bwrap: execvp .../sdk-cache/codex/0.153.0/.../bin/codex: No such file or directory
```

This is not a project, Python, file-lock, or Kaggriculture error. It appears
to be a stale bundled-Codex path in the Remote-SSH VS Code agent host. The
server reboot did not refresh this existing conversation’s agent runtime.

The patch mechanism works for known file changes; terminal commands, file
inspection, moves, imports, and tests do not. Start a *new* Codex agent chat
after reloading/reconnecting VS Code, then first run `pwd` to confirm command
execution is healthy.

## First commands for the next session

```bash
pwd
rg --files -g 'main.py' -g 'test_agent.py' -g 'shared.py' -g 'decision.py' -g 'conveyor.py' -g 'run_match.py'
mkdir -p agents/archive
mv main.py agents/archive/main_v0.py
mv test_agent.py agents/archive/test_agent_v0.py
```

Then create the new root `main.py`, run an import/syntax check, and inspect
`run_match.py` before deciding its exact agent-path CLI syntax.

## Next implementation goals

1. Add an explicit fixed seed option to `run_match.py` so policy comparisons
   can be repeated exactly.
2. Test carrot conveyor vs `pass`, then vs `random`.
3. Test carrot decision vs conveyor on identical seeds.
4. Record money/reward and calibration observations; only then alter one
   belief parameter at a time.

One known limitation to study: the carrot agent can scale planting beyond one
farmer's ability to water all carrot tiles. Care-feasibility planning is
deliberately not yet modeled.

#Prompt

Work in /home/experiments/frontier-week/foundry-trainer.

First, read HANDOFF/README.md and HANDOFF/SOURCE_INVENTORY.md in full. Then inspect the included fixture at HANDOFF/memoization-experiment/, plus the authoritative Kaggriculture documents and source referenced by the handoff.

Your task is to turn this handoff into a clean, local-only evaluation project for hypertuning Kaggriculture’s explicit carrot-policy parameters. Kaggriculture is the system under test; do not modify its policy, game rules, submission entry point, or agent architecture.

The immediate implementation goal is checkpoint/resume incremental counterfactual regression:

Run and record a fixed baseline suite against one deterministic opponent, across a small fixed seed-and-seat suite.
Preserve enough state to restore the simulator immediately before each decision (or use a documented checkpoint interval).
Replay a candidate policy against recorded observations until its first action divergence.
Reuse the baseline result exactly when there is no divergence; otherwise restore the pre-divergence state and simulate the remaining suffix forward.
Validate results against clean full reruns for no divergence, deliberately late divergence, and early divergence.
Before implementing, inspect the copied profile_step8.py carefully: it currently records and replays decision traces, but it does not checkpoint or restore simulator state, and its imports still assume the Kaggriculture repository layout. Create an explicit, documented target-project binding—never rely on the current directory or a generic agents module.

Preserve provenance for copied material, keep generated caches/results disposable and ignored, and record source revisions, configuration, opponent, seeds, seats, candidate parameter old/new values, divergence turns, suffix lengths, outcomes, and audit status.

Constraints: no Azure provisioning, external APIs, credentials, connectors, cloud storage, LLM calls, or automatic policy mutation. A human applies policy changes; local matched evidence determines whether they are supported. Keep full clean-suite audits alongside the fast cached loop.

Start by reporting the proposed project layout and validation plan. Then implement the smallest end-to-end checkpoint/resume proof. Do not expand into candidate search or Foundry-hosted agents until that proof is demonstrated.