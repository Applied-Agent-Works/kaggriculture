# Kaggriculture Agent Tuning Studio

**Status: direction established; implementation has not started.**

The Studio is intended to make Kaggriculture agents and matches selectable
and inspectable, support local simulation experiments, and provide a future
interface to a Microsoft Foundry agent that helps interpret run results and
support tuning.

This is a distinct project home. Preserve existing agents, experiment
artifacts, and visualizers while deciding which parts should be integrated.

## Near-term priority

Make existing agents and matches discoverable and viewable in the Studio.
Useful local operations—including running matches, retrieving evidence, and
opening the simulation viewer—should be available through real coding-agent
tools, not only through UI buttons.

## Intended workflow

1. Select an existing agent and opponent; inspect agent properties and
   available matches.
2. Use coding-agent tools to run simulations, inspect results, and open a
   selected match in the separate full-match visualizer.
3. Inspect named tunable parameters, distinct from fixed game facts, and
   preserve baseline values when proposing a candidate.
4. Compare controlled runs using recorded seeds, configuration, opponent,
   player positions, and behavior/economic metrics.
5. Later, provide bounded run evidence to a Foundry agent for advisory
   interpretation and tuning support, with human review and local validation.
6. Use interactive decision graphs when they help explain the selected
   agent's actual assumptions and decisions.

The existing "Beat the baseline" skill, natural-language requests, and
suggested candidate selections are possibilities to explore, not specified
requirements.

## Boundaries

- The full-farm, full-match viewer remains in a separate window.
- The existing Blazor/Fluent UI look and feel and interactive Mermaid graphs
  are useful references, not a mandate to copy the existing lesson UI or use a
  particular technology.
- Keep game-time policies local and independent of Foundry.
- Game rules are fixed facts, not tunable parameters.
- Foundry is a future advisory layer; it must not choose live actions,
  silently mutate policy, or declare a change successful without controlled
  local evidence.
- Any Foundry connection requires a separate approved architecture decision
  naming provider, budget, evidence flow, and human approval boundaries.

## Existing building blocks

- [`../../agents/`](../../agents/) contains the agent implementations.
- [`../../kaggriculture.py`](../../kaggriculture.py) and
  [`../../tools/run_match.py`](../../tools/run_match.py) support local game
  execution.
- [`../../tools/visualizers/`](../../tools/visualizers/) contains separate
  game viewers.
- [`../../docs/architecture/`](../../docs/architecture/) documents the agent
  and refinement boundaries.

## Incremental plan

See [IMPLEMENTATION_PLAN.md](IMPLEMENTATION_PLAN.md) for the staged delivery
path and [QUESTIONS.md](QUESTIONS.md) for unresolved decisions. Important
architectural decisions are recorded in
[ARCHITECTURE_DECISIONS.md](ARCHITECTURE_DECISIONS.md).
