# Questions and answers

## Open questions

### Q-006 — Which agent and match properties should appear first?

- Asked: 2026-10-03
- Question: Which existing agent properties and match summaries should the
  first selectable, viewable catalog expose?
- Why it matters: This is the near-term product priority and defines a useful
  first slice before broad tuning support.
- Current evidence: Agent policies live under `agents/`; match running and
  visualizing are currently separate workflows.
- Owner or next step: Inspect the existing agent entry points and match
  artifacts, then choose a narrow initial catalog.
- Status: Open

### Q-007 — Which actual coding-agent tools should the Studio expose?

- Asked: 2026-10-03
- Question: What coding-agent tool interface should provide operations for
  listing/inspecting agents and matches, running a match, accessing its
  evidence, and opening the separate visualizer?
- Why it matters: These utilities should be callable by the coding agent
  itself, not only implemented as UI-only controls.
- Current evidence: The repository has local Python match-running and separate
  visualizer code, but no Studio tool contract.
- Owner or next step: Identify the host's supported tool-extension mechanism
  and define the smallest useful operation set.
- Status: Open

### Q-008 — What is the Foundry agent's advisory boundary?

- Asked: 2026-10-03
- Question: What run evidence may a Microsoft Foundry agent receive, what may
  it recommend, and which provider/budget approvals are required?
- Why it matters: Foundry is the intended longer-term direction for helping
  interpret run results and tune agents, while the game-time policy remains
  local and candidate changes remain controlled experiments.
- Current evidence: The durable boundary is documented in
  [the Foundry refinement architecture](../../docs/architecture/foundry-refinement-architecture.md);
  this Studio has not been authorized to make external calls.
- Owner or next step: Define a bounded evidence package, provider, budget,
  data flow, and human-approval contract before implementing connectivity.
- Status: Open

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
- Answer: Open replay-specific URLs in one named visualizer window. The
  browser creates that window when absent and reuses it when present. If the
  local visualizer host is unavailable, Matchmaker should start it before
  opening the replay.
- Evidence: User decision in this session; the isolated runner now saves raw
  replay JSON alongside its manifest and evidence report.
- Answered: 2026-10-06
- Follow-up: Implemented in Matchmaker. Its **Run in visualizer** action runs
  a planned record first, then sends retained replay JSON to one reusable
  full-match visualizer window. Older records without a replay are rerun from
  their stored controls to produce one; existing artifacts remain on disk.
  The local Vite host starts on demand and uses the repository visualizer
  source with shared `@kaggle-environments/core` dependencies from a nearby
  Kaggle Environments checkout.
- Status: Answered

## Answered questions

### Q-015 — Which UI component system should the Studio use?

- Asked: 2026-10-06
- Question: Should Agent Tuning Studio screens continue with custom HTML and
  JavaScript controls, or use the existing Fluent UI for Blazor system?
- Answer: All Agent Tuning Studio application controls and
  data-management screens use Fluent UI for Blazor. The separate full-match
  visualizer remains its own specialized visualization surface.
- Evidence: User direction in this session and [ADR-012](ARCHITECTURE_DECISIONS.md).
- Answered: 2026-10-06
- Follow-up: Completed. The Matchmaker controls and data-management screens
  now live in the Fluent UI Blazor client, and the server hosts its built
  output.
- Status: Answered

### Q-016 — How should agents ensure the Matchmaker server is available?

- Asked: 2026-10-06
- Question: Should each agent launch or check the Matchmaker server
  independently, or should the repository provide one shared helper?
- Answer: Provide one shared helper that verifies the Matchmaker health
  endpoint, starts a detached local loopback server only when needed, and can
  restart a process it previously launched. Make its lifecycle commands
  discoverable in common and agent-specific instructions.
- Evidence: [server helper](tools/matchmaker/server.py), [tool guide](tools/matchmaker/README.md),
  and [ADR-013](ARCHITECTURE_DECISIONS.md).
- Answered: 2026-10-06
- Follow-up: None
- Status: Answered

### Q-013 — Should deterministic baseline challenges be a separate surface?

- Asked: 2026-10-06
- Question: Should the repeatable “Beat the baseline” workflow be part of
  Matchmaker or exposed as a separate development-agent tool?
- Answer: Keep it separate. Beat the Baseline owns matched baseline/candidate
  runs, fixed seeds, optional seat swaps, and aggregate deltas. Matchmaker
  owns individual match lifecycle operations; Match Analyzer interprets the
  resulting evidence.
- Evidence: [Beat the Baseline agent](../../.github/agents/kaggriculture-beat-the-baseline.agent.md),
  [comparison tool](tools/beat_the_baseline/), and the existing
  [carrot baseline policy](../../agents/carrot/conveyor.py).
- Answered: 2026-10-06
- Follow-up: The deterministic comparison has now been exercised. The first
  persisted one-parameter tuning-session runner is available for carrot,
  wheat, and melon; add richer lifecycle, decision-trace, and calibration
  metrics after the session workflow is exercised with real hypotheses.
- Status: Answered

### Q-012 — Should game setup and match display be one surface?

- Asked: 2026-10-06
- Question: Should selecting/configuring/storing a match and opening that
  match in the separate visualizer be exposed through one agent surface?
- Answer: Yes. Merge Match Display into the Matchmaker. Matchmaker now owns
  match selection, execution, artifact storage/retrieval, replay location,
  and separate-window display launch. Match Analyzer remains independent.
- Evidence: [Matchmaker agent](../../.github/agents/kaggriculture-matchmaker.agent.md)
  and [Matchmaker tool skeleton](tools/matchmaker/).
- Answered: 2026-10-06
- Follow-up: Define the display-launch adapter when replay artifacts are
  available.
- Status: Answered

### Q-011 — Which development-agent surfaces should be separate?

- Asked: 2026-10-06
- Question: Should match setup, full-match display, and match analysis be
  exposed through one general tool or separate agent-facing tools?
- Answer: Keep them separate. Game Setup owns selecting/configuring/running
  matches and storing/retrieving artifacts; Match Display owns the separate
  visualizer window; Match Analyzer owns evidence interpretation and
  controlled comparisons.
- Evidence: [surface agents](../../.github/agents/) and
  [surface skeletons](tools/).
- Answered: 2026-10-06
- Follow-up: Bind the artifact contracts after the three surfaces have been
  exercised independently.
- Status: Answered

### Q-010 — Which workspace tool mechanism should start the Studio?

- Asked: 2026-10-06
- Question: Should the initial Studio tool surface use a standalone server or
  repository-shared VS Code/Copilot workspace customization?
- Answer: Start with workspace-shared custom agents and instructions, without
  a standalone server. The first runner is manually invoked, source-editing
  tools are excluded, and each local match receives a unique output
  directory.
- Evidence: [workspace agents](../../.github/agents/),
  [Studio instructions](../../.github/instructions/agent-tuning-studio.instructions.md),
  and [`tools/run_isolated_match.py`](tools/run_isolated_match.py).
- Answered: 2026-10-06
- Follow-up: Define stronger command enforcement and the broader catalog/tool
  contract before adding more operations.
- Status: Answered

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

### Q-009 — Should Foundry interpretation be part of the longer-term direction?

- Asked: 2026-10-03
- Question: Is the intended eventual role limited to local dashboards, or
  should an agent help interpret run outcomes and support tuning?
- Answer: A Microsoft Foundry agent is part of the intended longer-term
  direction for interpreting simulation results and helping tune the agents.
  Natural-language interaction, the "Beat the baseline" skill, and possible
  selection suggestions are ideas to explore, not specified requirements.
- Evidence: The product priority in the
  [Studio overview](README.md) and staged workflow in the
  [implementation plan](IMPLEMENTATION_PLAN.md).
- Answered: 2026-10-03
- Follow-up: Define the reviewed evidence, provider/budget, and data-flow
  contract before making any external connection; see Q-008.
- Status: Answered

### Q-014 — What is the first executable Matchmaker operation?

- Asked: 2026-10-07
- Question: How should a selected Matchmaker record become a reproducible
  local run?
- Answer: Require an explicit run operation for a `planned` record. The
  server and CLI mark it `running`, invoke the existing isolated fixed-seed
  runner, and persist the terminal status, unique artifact path, exit code,
  and error message. A seat of `1` swaps simulator player order without
  changing the catalog roles.
- Evidence: [Matchmaker server](src/Matchmaker.Server/Program.cs),
  [isolated runner](tools/run_isolated_match.py), and
  [Matchmaker CLI](tools/matchmaker/catalog.py).
- Answered: 2026-10-07
- Follow-up: The CLI and server run paths were exercised with one-step
  fixed-seed matches. They retain the manifest, status, evidence report,
  decision trace, and raw `env.toJSON()` replay. Q-003 now defines the
  separate-window behavior, and the server exposes a safe replay endpoint;
  bind the existing visualizer to that endpoint and implement on-demand host
  startup.
- Status: Answered
