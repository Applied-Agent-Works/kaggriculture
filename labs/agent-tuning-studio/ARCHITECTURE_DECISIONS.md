# Architectural  

## Decision summary

| ID | Title | Status | Date |
|---|---|---|---|
| ADR-001 | Keep full-match viewing separate and make the Studio experiment-driven | Accepted | 2026-10-02 |
| ADR-002 | Prioritize agent/match discovery and coding-agent tools before Foundry | Accepted | 2026-10-03 |

## ADR-001 — Keep full-match viewing separate and make the Studio experiment-driven

- Date: 2026-10-02
- Status: Accepted
- Context: The repository contains game visualizers and a decision-network
  learning experience. The user wants to combine useful ideas without
  embedding the full farm view or retaining lesson UI that did not provide
  practical tuning utility.
- Decision: Build a distinct local Studio around agent selection, parameter
  inspection, simulations, and results. Keep full-match viewing in a separate
  window. The existing Fluent UI look and interactive Mermaid graph are
  references, not mandates.
- Alternatives considered: Embed the game viewer; copy the existing lesson
  interface wholesale; define a final UI before using simulation workflows.
- Consequences: Preserve existing agents and viewers. Let real experiments
  shape the Studio interface.
- Evidence: User direction recorded in the
  [Studio overview](README.md).
- Supersedes: None

## ADR-002 — Prioritize agent/match discovery and coding-agent tools before Foundry

- Date: 2026-10-03
- Status: Accepted
- Context: A Foundry agent is intended to help interpret runs and support
  tuning, but agent and match selection and local operations are the immediate
  practical priority.
- Decision: First make agents and matches selectable and inspectable, and
  expose local run/evidence/viewer utilities through actual coding-agent
  tools. Treat Foundry interpretation as a later advisory capability.
  Natural-language workflows, "Beat the baseline," and suggested selections
  remain exploratory.
- Alternatives considered: Build Foundry integration first; provide only
  UI-triggered operations; prescribe a natural-language workflow before
  agent/match discovery exists.
- Consequences: Near-term development remains local. No Foundry connection is
  authorized by this decision; provider, budget, evidence flow, and approval
  boundaries require a separate explicit decision.
- Evidence: User direction and the
  [implementation plan](IMPLEMENTATION_PLAN.md).
- Supersedes: None
