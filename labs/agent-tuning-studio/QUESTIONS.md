# Questions and answers

## Open questions

### Q-001 — Which agent and match properties should appear first?

- Asked: 2026-10-03
- Question: Which agents, properties, match summaries, and artifacts form the
  smallest useful first catalog?
- Why it matters: Agent and match discovery is the near-term priority and
  should be useful before a broad tuning feature set exists.
- Current evidence: Agents are under `agents/`; the runner and visualizers
  currently operate separately.
- Owner or next step: Inventory their current observable properties and
  choose one narrow first slice.
- Status: Open

### Q-002 — Which coding-agent tool interface should be used?

- Asked: 2026-10-03
- Question: Which host-supported extension surface should provide local
  operations for match runs, evidence retrieval, and opening the viewer?
- Why it matters: The operations must be real callable tools for the coding
  agent, not UI-only lookalikes.
- Current evidence: No Studio tool contract exists yet.
- Owner or next step: Inspect the host's supported tool-extension mechanism
  before implementing tool integration.
- Status: Open

### Q-003 — How should a selected run open in the separate visualizer?

- Asked: 2026-10-02
- Question: Which replay artifact or launch mechanism connects a result to
  its full-match viewer?
- Why it matters: The viewer stays in a separate window, but should open the
  exact match associated with the selected result.
- Current evidence: The runner and visualizers are separate workflows.
- Owner or next step: Define this contract with the first run-artifact design.
- Status: Open

### Q-004 — What is the Foundry advisory boundary and authorization?

- Asked: 2026-10-03
- Question: What evidence can a Foundry agent receive, what may it recommend,
  and what provider/budget approvals are needed?
- Why it matters: Foundry is a longer-term run-interpretation assistant, while
  the game-time policy must remain local and candidate changes require human
  review and matched evidence.
- Current evidence: See
  [the refinement architecture](../../docs/architecture/foundry-refinement-architecture.md).
- Owner or next step: Define provider, budget, data flow, evidence bounds,
  and human approval before any connection.
- Status: Open

### Q-005 — Which agent and parameter anchor the first tuning experiment?

- Asked: 2026-10-02
- Question: Which single existing agent parameter should be tuned first?
- Why it matters: It determines the first experiment and agent adapter.
- Current evidence: The carrot belief policy and one-time crop examples have
  explicit source-level assumptions.
- Owner or next step: Choose after the agent/match discovery slice is scoped.
- Status: Open

## Answered questions

### Q-006 — Should the full-match viewer be embedded?

- Asked: 2026-10-02
- Question: Should the simulation viewer share the Studio window?
- Answer: No. Keep the full-match viewer in its own window so it has room to
  show the whole game.
- Answered: 2026-10-02
- Follow-up: Define a link/launch contract; see Q-003.
- Status: Answered

### Q-007 — Should the existing lesson UI be copied wholesale?

- Asked: 2026-10-02
- Question: Should the Studio retain every screen and lesson from the
  decision-network experience?
- Answer: No. Let simulation and tuning use reveal the useful UI. Existing
  visual style and interactive Mermaid graphs may be reused selectively.
- Answered: 2026-10-02
- Follow-up: None
- Status: Answered

### Q-008 — Is Foundry part of the longer-term direction?

- Asked: 2026-10-03
- Question: Should the Studio eventually use an agent to interpret run
  outcomes and support tuning?
- Answer: Yes. A Microsoft Foundry agent is the intended longer-term advisory
  layer. Natural-language requests, "Beat the baseline," and suggested
  selections are ideas to explore, not settled requirements.
- Answered: 2026-10-03
- Follow-up: Resolve Q-004 before any external connection.
- Status: Answered
