# Kaggriculture Agent Tuning Studio

**Status: direction and project home established; implementation has not
started.**

The Agent Tuning Studio will provide a local interface for selecting
Kaggriculture agents, inspecting and adjusting their explicit parameters,
running reproducible simulations, and examining the results.

This is a new home for that work, not a replacement for the existing
[Decision Network Lab](../decision-network-lab/). Keep the lab and visualizer
work intact while identifying which pieces are useful to reuse.

## Intended experiment loop

1. Select an existing agent and an opponent.
2. Inspect the agent's behavior and named tunable parameters, distinct from
   fixed game facts.
3. Define a candidate change without losing the baseline values.
4. Run local simulations with recorded seeds, configuration, opponent, and
   player positions.
5. Compare outcomes and relevant behavior, such as final bank, wins, crop
   lifecycle counts, sales, and decision counts.
6. Open the resulting full-match visualization in its own window.
7. When useful, inspect an interactive decision graph to understand the
   assumptions and decisions behind the agent.

The interface should take shape through real simulation and tuning work. This
is not a commitment to port every screen, lesson, or workflow from the
Decision Network Lab.

## Product boundaries

- The Studio controls and reviews experiments; it is not a replacement for
  the local Python agent or the Kaggriculture game engine.
- The full-farm, full-match simulation viewer remains a separate window so it
  can use the space needed to show the game.
- Interactive Mermaid graphs and the look and feel of the existing
  Blazor/Fluent UI lab are useful design references. Reuse should be based on
  demonstrated value; neither the full existing UI nor its framework is
  mandated here.
- Keep game rules, uncertain beliefs, and policy preferences distinguishable.
  Game rules are not tuning parameters.
- Preserve controlled, reproducible experiments: change one named parameter
  at a time, hold other conditions fixed, and evaluate economic results
  separately from prediction calibration.
- Keep the workflow local. Do not add cloud services, external model calls, or
  runtime LLM dependencies.
- Candidate persistence, parameter metadata, and the exact way the Studio
  launches or links to a replay are still open design questions.

## Existing building blocks

- [`../../agents/`](../../agents/) contains the Python agents and their
  policies.
- [`../../kaggriculture.py`](../../kaggriculture.py) is the local game source;
  [`../../tools/run_match.py`](../../tools/run_match.py) is the local match
  runner.
- [`../decision-network-lab/`](../decision-network-lab/) contains an existing
  Blazor/Fluent UI experience and interactive decision graphs. It currently
  evaluates narrow teaching scenarios rather than running Python agents in
  the full game simulator.
- [`../../tools/visualizers/`](../../tools/visualizers/) contains the separate
  game visualizers.
- [`../../docs/architecture/kaggriculture-agent-architecture.md`](../../docs/architecture/kaggriculture-agent-architecture.md)
  documents the policy and experiment invariants.

## Incremental path

1. Agree on one useful end-to-end experiment and the evidence it must preserve.
2. Define how an agent exposes its tunable parameters and how a candidate is
   kept separate from the baseline.
3. Connect the local UI to reproducible runs of the real simulator.
4. Present comparisons and provide a way to open the associated match in the
   separate visualizer.
5. Add decision-graph inspection where it helps explain actual agent behavior.

These are steps for exploration, not a fixed delivery schedule. This first
stage establishes the home and records the direction; it adds no application
code or simulator integration.
