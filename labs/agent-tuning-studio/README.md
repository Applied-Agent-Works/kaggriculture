# Kaggriculture Agent Tuning Studio

**Status: direction and project home established; implementation has not
started.**

The Agent Tuning Studio will provide a local interface for selecting
Kaggriculture agents, inspecting and adjusting their explicit parameters,
running reproducible simulations, and examining the results.

This is a new home for that work, not a replacement for the existing
[Decision Network Lab](../decision-network-lab/). Keep the lab and visualizer
work intact while identifying which pieces are useful to reuse.

## Product priority

The first practical priority is not a general tuning assistant. It is making
existing agents and match results selectable and inspectable in one interface,
with useful local operations available through real coding-agent tools.

The longer-term direction is for this experience to work with a Microsoft
Foundry agent that helps interpret simulation results and support agent tuning.
Natural-language requests, the existing "Beat the baseline" skill, and
suggested candidate selections are possibilities to explore later; they are
not a defined interaction contract yet.

## Intended experiment loop

1. Select an existing agent and an opponent.
2. Inspect available agents, their behavior and properties, and existing
   matches and results.
3. Use coding-agent tools to perform relevant local operations, such as
   running matches, retrieving experiment evidence, and opening a match in
   the separate visualizer.
4. Inspect the agent's named tunable parameters, distinct from fixed game
   facts, and define a candidate without losing the baseline values.
5. Run controlled simulations with recorded seeds, configuration, opponent,
   and player positions.
6. Compare outcomes and relevant behavior, such as final bank, wins, crop
   lifecycle counts, sales, and decision counts.
7. Over time, use a Foundry agent to help interpret run results and support
   tuning decisions, with human review and controlled simulation evidence.
8. When useful, inspect an interactive decision graph to understand the
   assumptions and decisions behind the agent.

The interface should take shape through real simulation and tuning work. This
is not a commitment to port every screen, lesson, or workflow from the
Decision Network Lab.

## Product boundaries

- The Studio controls and reviews experiments; it is not a replacement for
  the local Python agent or the Kaggriculture game engine.
- The existing agents and matches should become discoverable and inspectable
  before investing in broad model-editing or natural-language features.
- Match-running, artifact-access, and viewer-launch operations should be
  exposed through actual coding-agent tools when they are needed by an agent;
  the Studio must not pretend a UI-only action is available to the coding
  agent.
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
- Keep Kaggriculture game-time policies local and independent of Foundry.
  Foundry is a planned, separate advisory layer for interpreting completed run
  evidence; it must not choose live game actions or silently mutate policies.
- Any future Foundry connection is a separate architecture and provider/budget
  decision. Until explicitly approved, development and evaluation remain local
  with no external model calls, credentials, or service dependencies.
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

1. Make agents and matches selectable, inspectable, and viewable in the new
   interface.
2. Identify the local operations the coding agent needs and expose them as
   real tools, starting with the smallest useful set for inspecting agents and
   matches, running a match, and opening its visualization.
3. Choose one controlled tuning experiment and define how its agent exposes
   tunable parameters while preserving baseline values.
4. Compare runs and evidence reproducibly, and connect results to the separate
   full-match visualizer.
5. Define a reviewed evidence handoff to a Foundry advisory agent that can
   interpret runs and support tuning. Explore natural-language interaction,
   "Beat the baseline," and suggested selections only after the run/evidence
   workflow is useful.
6. Add decision-graph inspection where it helps explain actual agent
   behavior.

These are steps for exploration, not a fixed delivery schedule. This first
stage establishes the home and records the direction; it adds no application
code or simulator integration.
