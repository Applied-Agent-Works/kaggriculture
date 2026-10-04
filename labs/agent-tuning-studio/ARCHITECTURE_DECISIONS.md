# Architectural decisions

## Decision summary

| ID | Title | Status | Date |
|---|---|---|---|
| ADR-001 | Make the Studio experiment-driven and keep the full-match viewer separate | Accepted | 2026-10-02 |
| ADR-002 | Prioritize agent/match discovery and coding-agent tools before Foundry interpretation | Accepted | 2026-10-03 |

## ADR-001 — Make the Studio experiment-driven and keep the full-match viewer separate

- Date: 2026-10-02
- Status: Accepted
- Context: The repository has a full-match visualizer and a Blazor decision
  network lab. The user wants to continue the tunable-agent work by combining
  useful parts of both systems, without doing the whole integration at once or
  carrying over UI that did not help the practical workflow.
- Decision: The Agent Tuning Studio is the home for a local workflow to select
  agents, inspect and tune explicit parameters, run reproducible simulations,
  and review results. The full-match visualization remains in a separate
  window. The final interface should be discovered through actual simulation
  and tuning use, rather than prescribed by the existing lesson screens.
  The existing Blazor/Fluent UI look and feel and interactive Mermaid graph
  components are reusable design references, not a commitment to copy the
  whole application or lock the Studio to that technology.
- Alternatives considered: Embed the full farm viewer in the Studio; copy the
  existing lesson UI wholesale; design a universal tuning interface before
  running real experiments.
- Consequences: Keep the existing lab and visualizers intact as source
  material. Define an experiment and replay handoff before building the
  integrated workflow. Keep policy parameters inspectable and separate from
  fixed game facts.
- Evidence: User-approved direction; see the
  [Studio overview](README.md) and
  [open questions](QUESTIONS.md).
- Supersedes: None

## ADR-002 — Prioritize agent/match discovery and coding-agent tools before Foundry interpretation

- Date: 2026-10-03
- Status: Accepted
- Context: The Studio's longer-term role includes connecting run evidence to a
  Microsoft Foundry agent that can help interpret results and support tuning.
  However, a useful near-term product must first expose agents, matches, and
  local simulation/viewer utilities.
- Decision: Prioritize selecting and inspecting agents and matches in the
  Studio, and expose needed local operations through actual coding-agent tools.
  Treat Foundry-based result interpretation as a later advisory capability.
  Natural-language interaction, the "Beat the baseline" skill, and
  recommended candidate selections remain exploratory ideas rather than
  specified requirements.
- Alternatives considered: Build the Foundry interaction first; build a
  tuning-only dashboard with no coding-agent tool surface; prescribe a
  natural-language and skill workflow before the experiment interface exists.
- Consequences: Near-term work can stay local and prove agent/match discovery,
  reproducible simulation, evidence, and replay access. Foundry calls are not
  authorized by this decision: a separate architecture decision must specify
  provider, budget, evidence flow, and approval boundaries before connection.
- Evidence: User-approved product refinement; see the
  [Studio overview](README.md), [implementation plan](IMPLEMENTATION_PLAN.md),
  and [question log](QUESTIONS.md).
- Supersedes: None
