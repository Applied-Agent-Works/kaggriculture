# Questions and answers

## Open questions

### Q-001 — Which agent should anchor the first full experiment?

- Asked: 2026-10-02
- Question: Which existing agent and single tunable parameter should define
  the first end-to-end UI workflow?
- Why it matters: The first workflow should exercise real parameter editing,
  simulation, and comparison without prematurely designing a universal agent
  plugin system.
- Current evidence: The repository has a belief-based carrot agent and
  separate wheat and melon examples. See
  [the agent packages](../../agents/) and
  [the crop decision networks](../../docs/decision-networks/).
- Owner or next step: Choose the smallest useful experiment with the user.
- Status: Open

### Q-002 — How should agents describe tunable parameters?

- Asked: 2026-10-02
- Question: Should the Studio initially support one explicit agent adapter or
  a shared metadata contract for exposing parameters and their explanations?
- Why it matters: This determines how the UI can inspect and edit values
  without confusing them with fixed game rules or relying on fragile source
  parsing.
- Current evidence: Current policy parameters are explicit in Python source;
  no Studio-facing parameter contract exists yet.
- Owner or next step: Decide after choosing the first agent workflow.
- Status: Open

### Q-003 — How should a Studio run connect to the separate visualizer?

- Asked: 2026-10-02
- Question: What replay artifact or launch mechanism should connect an
  experiment result to the full-match viewer in its own window?
- Why it matters: The Studio should not embed the whole farm view, but the
  user should be able to move from a comparison to the corresponding match.
- Current evidence: The repository has local match-running and visualization
  components, but no integrated launch contract.
- Owner or next step: Define alongside the first simulator integration.
- Status: Open

## Answered questions

### Q-004 — Should the full-match viewer be embedded in the tuning interface?

- Asked: 2026-10-02
- Question: Should the full simulation view share the Studio window?
- Answer: No. Keep the full-match viewer in its own window so it has enough
  space to show the whole game.
- Evidence: Product direction recorded in
  [the architecture decisions](ARCHITECTURE_DECISIONS.md).
- Answered: 2026-10-02
- Follow-up: Define how the Studio opens the corresponding match; see Q-003.
- Status: Answered

### Q-005 — Should the existing decision-network UI be copied wholesale?

- Asked: 2026-10-02
- Question: Should the Studio retain the existing lab's lesson-first UI and
  all of its screens?
- Answer: No. Let simulation and agent-tuning work reveal the useful
  interface. Treat the existing visual style and interactive Mermaid graph
  as reusable ideas, not a requirement to preserve every screen.
- Evidence: Product direction recorded in
  [the architecture decisions](ARCHITECTURE_DECISIONS.md).
- Answered: 2026-10-02
- Follow-up: None
- Status: Answered
