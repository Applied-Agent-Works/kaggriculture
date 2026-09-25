# Temporary Handoff — Kaggriculture Crop Networks

## Immediate status

This workspace studies symbolic AI, probability, belief networks, influence
diagrams, and decision theory through small, readable Kaggriculture agents.
The next learning focus is standalone wheat and melon crop networks, followed
by tomato and strawberry. Keep larger crop-to-animal production chains out of
scope for those first crop models.

The agent reorganization is complete:

```text
agents/
  __init__.py
  carrot/
    __init__.py
    shared.py       # carrot facts, beliefs, utility, care/movement helpers
    decision.py     # belief-based policy; exposes agent(obs)
    conveyor.py     # unconditional baseline; exposes agent(obs)
```

The two carrot wrappers expose `agent(obs)` and share the implementation in
`shared.py`. Root `main.py` imports the decision policy. Historical agents are
preserved under `agents/archive/`.

## Existing learning and architecture documents

- `AGENTS.md` — current authoritative working instructions and project state.
- `README.md` — official game mechanics and crop facts.
- `kaggriculture-agent-architecture.md` — overall network architecture and
  current learning sequence.
- `carrot-decision-network.md` — the completed first crop example.
- `kaggriculture-ai-notes.md` — introductory logic/probability notes.
- `foundry-refinement-architecture.md` — intended boundary for future offline
  Foundry analysis. It describes a design; it does not mean a Foundry agent or
  service is configured in this repository.

## Current local runner

`run_match.py` accepts `--agent`, `--opponent`, `--steps`, and optional
`--seed`. Use the same seed, configuration, opponent, and seat assignments
when comparing policies. The default length is one day (24 turns); use 720
turns for a full season.

## Next learning steps

1. Compare the wheat and melon decision-network drafts with their one-tile
   code. The first policies use current price as a point estimate; they are
   not yet probabilistic.
2. Choose how to represent and estimate each crop's future sale-price
   uncertainty before tuning belief values.
3. Later study tomato and strawberry as ongoing crops with repeated scheduled
   yields, then move on to production chains.
4. Preserve the fixed-facts / beliefs / utility distinction. Do not tune
   game rules to improve outcomes.

The carrot agent can scale planting beyond one farmer's ability to water all
carrot tiles; care feasibility is not modeled yet. Treat it as a known
limitation, not a resolved result.
