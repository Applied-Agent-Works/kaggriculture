# Architectural decisions

## Decision summary

| ID | Title | Status | Date |
|---|---|---|---|
| ADR-001 | Make the Studio experiment-driven and keep the full-match viewer separate | Accepted | 2026-10-02 |
| ADR-002 | Prioritize agent/match discovery and coding-agent tools before Foundry interpretation | Accepted | 2026-10-03 |
| ADR-003 | Start with workspace-scoped Copilot agents and isolated run output | Accepted | 2026-10-06 |
| ADR-004 | Keep setup, display, and analysis as separate agent surfaces | Superseded | 2026-10-06 |
| ADR-005 | Merge game setup and match display | Superseded | 2026-10-06 |
| ADR-006 | Name the merged surface Matchmaker | Accepted | 2026-10-06 |
| ADR-007 | Store Matchmaker metadata in a local catalog | Accepted | 2026-10-06 |
| ADR-008 | Make deterministic baseline challenges a separate surface | Accepted | 2026-10-06 |
| ADR-009 | Preserve descriptive evidence beside each challenge match | Accepted | 2026-10-07 |
| ADR-010 | Execute planned matches through the isolated runner | Accepted | 2026-10-07 |
| ADR-011 | Open replays in one reusable visualizer window | Accepted | 2026-10-06 |
| ADR-012 | Use Fluent UI for the Agent Tuning Studio interface | Accepted | 2026-10-06 |
| ADR-013 | Provide a shared Matchmaker server lifecycle command | Accepted | 2026-10-06 |

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

## ADR-003 — Start with workspace-scoped Copilot agents and isolated run output

- Date: 2026-10-06
- Status: Accepted
- Context: The Studio needs a first tool surface that lets different agents
  inspect the repository and run local simulations without sharing mutable
  output or editing the live policy. A standalone tool server would introduce
  another process and contract before the local workflow is understood.
- Decision: Start with repository-shared VS Code/Copilot custom agents and
  on-demand Studio instructions. Provide a read-only inspector and a
  manually invoked experiment runner. The runner validates repository-local
  agent files, requires a fixed seed, invokes the existing local runner
  without a shell, and writes each run to a unique ignored directory with a
  manifest, status, and captured output.
- Alternatives considered: Add an MCP or other standalone tool server first;
  allow a general editing agent to run experiments in the shared checkout;
  write all concurrent results to one common log.
- Consequences: The first slice is easy to discover in VS Code and keeps
  policy execution local. Unique output directories prevent run artifacts from
  colliding, but the runner is not a security sandbox for arbitrary Python
  code. Catalog discovery, parameter editing, replay launching, stronger
  command enforcement, and UI integration remain open.
- Evidence: [workspace agent definitions](../../.github/agents/),
  [Studio instructions](../../.github/instructions/agent-tuning-studio.instructions.md),
  and [`tools/run_isolated_match.py`](tools/run_isolated_match.py).
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

## ADR-004 — Keep setup, display, and analysis as separate agent surfaces

- Date: 2026-10-06
- Status: Superseded
- Context: Development agents need different interactions with the local
  Kaggriculture workflow. Match creation and artifact storage, full-match
  display, and evidence interpretation have different safety and ownership
  boundaries.
- Decision: Skeleton three separate workspace-agent surfaces and tool areas:
  Game Setup, Match Display, and Match Analyzer. Pass identified artifacts
  between them. Game Setup owns selection, controls, bounded execution,
  storage/retrieval, and replay references. Match Display owns the separate
  visualizer window. Match Analyzer owns completed evidence and controlled
  comparison. No surface may silently perform another surface's work.
- Alternatives considered: One general-purpose Studio agent; embedding
  display and analysis into setup; implementing a unified service before the
  contracts are understood.
- Consequences: Agents can be permissioned and evolved independently, and
  incomplete functionality is reported honestly. Binding, persistent
  catalogs, replay launch, and analyzer implementation remain future work.
- Evidence: [separate workspace agents](../../.github/agents/) and
  [separate tool skeletons](tools/).
- Superseded by: ADR-005

## ADR-005 — Merge game setup and match display

- Date: 2026-10-06
- Status: Accepted
- Context: The initial split treated match setup and opening its selected
  replay as separate agent surfaces, but both operations belong to one
  match-lifecycle workflow. Keeping them separate adds coordination without
  providing a useful safety boundary.
- Decision: Merge Game Setup and Match Display into one Game Setup surface.
  Game Setup owns selecting agents and opponents, configuring and running
  matches, storing and retrieving match records, locating replay artifacts,
  and opening the selected match in the separate full-match visualizer.
  Match Analyzer remains a separate read-only evidence surface.
- Alternatives considered: Keep three separate surfaces; merge analysis into
  setup; build a unified general-purpose Studio agent.
- Consequences: Match lifecycle operations have one owner and one artifact
  handoff. Display launching remains separate from evidence interpretation,
  and the visualizer remains a separate window. The standalone Match Display
  customization and tool directory are removed.
- Evidence: [Matchmaker agent](../../.github/agents/kaggriculture-matchmaker.agent.md)
  and [Matchmaker tool skeleton](tools/matchmaker/).
- Supersedes: ADR-004

## ADR-006 — Name the merged surface Matchmaker

- Date: 2026-10-06
- Status: Accepted
- Context: The merged match-lifecycle surface covers both match configuration
  and the separate-window display handoff. “Game Setup” no longer describes
  the full responsibility clearly enough.
- Decision: Name the merged workspace agent and tool area **Matchmaker**.
  Matchmaker owns selecting agents and opponents, configuring and running
  matches, storing and retrieving records, locating replays, and opening
  matches in the separate visualizer. Match Analyzer remains separate.
- Alternatives considered: Keep “Game Setup”; use separate setup and display
  names; use one general-purpose Studio agent.
- Consequences: Current Studio-facing terminology and paths use Matchmaker.
  Historical ADRs and question entries retain their original wording as
  decision history.
- Evidence: [Matchmaker agent](../../.github/agents/kaggriculture-matchmaker.agent.md)
  and [Matchmaker tool skeleton](tools/matchmaker/).
- Supersedes: ADR-005

## ADR-007 — Store Matchmaker metadata in a local catalog

- Date: 2026-10-06
- Status: Accepted
- Context: Matchmaker needs truthful agent and match records that both the
  local UI and workspace agents can inspect and mutate. Run artifacts and
  agent source must not be treated as disposable catalog rows.
- Decision: Store agent and match metadata in ignored JSON catalog files under
  `labs/agent-tuning-studio/catalog/`. Expose list, get, create, update, and
  delete operations through the local Matchmaker server and the checked-in
  `tools/matchmaker/catalog.py` CLI. Agent deletion removes only the catalog
  record. Match deletion removes only the catalog record, never the referenced
  source or artifact. Match records preserve seed, seat, step count, status,
  artifact path, and source revision; creating a record does not run a match.
- Alternatives considered: Parse source and run directories on every request;
  let catalog deletion remove source or artifacts; make the UI the only
  client of the catalog.
- Consequences: The first CRUD surface is local, inspectable, and usable by
  workspace agents without external services. Cross-process writes use atomic
  replacement, while richer locking, replay indexing, and match execution
  remain follow-up work.
- Evidence: [Matchmaker catalog tool](tools/matchmaker/catalog.py),
  [Matchmaker tool README](tools/matchmaker/README.md), and the local server
  under `src/Matchmaker.Server/`.
- Supersedes: None

## ADR-008 — Make deterministic baseline challenges a separate surface

- Date: 2026-10-06
- Status: Accepted
- Context: The earlier “Beat the baseline” idea appears in Studio planning,
  but no implemented skill or contract was found. A repeatable comparison
  needs stricter controls than an ordinary Matchmaker run and should not be
  confused with evidence interpretation.
- Decision: Add a separate Beat the Baseline workspace agent and tool. It
  requires explicit seeds, runs baseline and candidate against the same
  opponent under matched controls, optionally swaps seats, and records
  manifests, per-match outputs, statuses, and aggregate reward deltas in a
  unique challenge directory. It reports a lead, tie, or failure; Match
  Analyzer remains responsible for interpretation and recommendations.
- Alternatives considered: Add a natural-language skill with implicit
  controls; fold the workflow into Matchmaker; let Match Analyzer launch
  matches; declare a policy better from one result.
- Consequences: Baseline comparisons are deterministic and easy to repeat.
  The surface also provides an explicit tuning-session runner for the
  documented iterative workflow: one named parameter, repeated matched
  rounds, and persisted keep/review evidence. It does not edit policies,
  game rules, or default parameter instances; generated wrappers are isolated
  under ignored run directories. It does not explain causes, measure
  calibration, or prove general superiority.
- Evidence: [Beat the Baseline agent](../../.github/agents/kaggriculture-beat-the-baseline.agent.md),
  [comparison tool](tools/beat_the_baseline/), the existing
  [carrot conveyor baseline](../../agents/carrot/conveyor.py), and the
  [tuning-session runner](tools/beat_the_baseline/run_tuning_session.py).
- Supersedes: None

## ADR-009 — Preserve descriptive evidence beside each challenge match

- Date: 2026-10-07
- Status: Accepted
- Context: Reward deltas alone cannot distinguish an economic result from
  changes in action behavior, crop lifecycle, or an unexamined confound.
  The public simulator exposes observations and requested actions, but does
  not provide an authoritative accepted-order audit for every market action.
- Decision: Extend the local match runner with an optional evidence report
  containing per-player action counts, plant/pass decisions, observed crop
  lifecycle losses, active crops, requested market orders, quoted sale value,
  and pre-action JSONL traces. Mark realized sale values and invalid/no-op
  action counts unavailable instead of estimating them.
- Alternatives considered: Keep reward-only summaries; infer accepted sales
  and invalid actions from bank or market deltas; modify the live policy to
  emit experiment data.
- Consequences: Baseline challenges are more inspectable without changing
  agent source or game-time behavior. The reports remain descriptive and
  incomplete until the simulator exposes authoritative order-result data.
- Evidence: [match evidence builder](../../tools/match_evidence.py),
  [local match runner](../../tools/run_match.py), and
  [Beat the Baseline comparison runner](tools/beat_the_baseline/run_baseline_comparison.py).
- Supersedes: None

## ADR-010 — Execute planned matches through the isolated runner

- Date: 2026-10-07
- Status: Accepted
- Context: Matchmaker catalog CRUD could record controls but could not yet
  execute an individual match or retain the resulting run artifact. The
  repository already has an isolated fixed-seed runner that creates a unique
  ignored directory and captures simulator evidence.
- Decision: Matchmaker owns an explicit run operation for `planned` records.
  The local server endpoint and catalog CLI mark the record `running`, invoke
  `tools/run_isolated_match.py` without a shell, and persist `succeeded` or
  `failed`, the unique artifact path, exit code, and error message. The
  recorded seat determines which selected participant is passed as simulator
  player 0; catalog agent/opponent roles remain unchanged.
- Alternatives considered: Let CRUD creation implicitly run a match; invoke
  the simulator directly from the browser; duplicate simulator logic in the
  server; or silently treat a failed process as a successful record.
- Consequences: A match now has an explicit lifecycle and retained evidence,
  while replay indexing, display launch, and outcome interpretation remain
  separate operations. The current run artifact contains the manifest,
  status, evidence report, decision trace, and raw replay JSON. A server
  restart during execution can leave a record marked `running`, which remains
  a follow-up recovery concern.
- Evidence: [isolated runner](tools/run_isolated_match.py), [Matchmaker
  server](src/Matchmaker.Server/Program.cs), and [Matchmaker CLI](tools/matchmaker/catalog.py).
- Supersedes: None

## ADR-011 — Open replays in one reusable visualizer window

- Date: 2026-10-06
- Status: Accepted
- Context: Matchmaker needs to open the replay for a selected match in the
  separate full-match visualizer. The user wants one visualizer window reused
  across matches, with a new window opened only when none exists.
- Decision: Matchmaker opens a replay-specific URL using one stable named
  browser target. The browser creates the target when absent and navigates the
  existing target when present. Matchmaker starts the local visualizer host
  when it is not already responding.
- Alternatives considered: Open a new browser window for every replay; require
  users to start the visualizer manually; display the full farm inside the
  Matchmaker page.
- Consequences: Replay selection stays in Matchmaker while the full visualizer
  remains a separate window. The viewer must load a replay from a request-time
  URL, and Matchmaker must detect or start the local viewer host. The runner
  now retains raw replay JSON beside its descriptive evidence.
- Evidence: User decision in this session, [isolated runner](tools/run_isolated_match.py),
  and [local match runner](../../tools/run_match.py).
- Supersedes: None

## ADR-012 — Use Fluent UI for the Agent Tuning Studio interface

- Date: 2026-10-06
- Status: Accepted
- Context: The active Matchmaker page was implemented with plain HTML
  controls and custom JavaScript, while a Blazor client scaffold already
  existed. The user wants a consistent Fluent UI system that is easier to
  understand and maintain.
- Decision: All Agent Tuning Studio controls and data-management surfaces use
  Microsoft Fluent UI for Blazor. Keep layout and typography styles local
  where useful, but use Fluent components for interactive controls, status
  indicators, cards, progress, and messages. The separate full-match
  visualizer remains its own specialized visualization surface.
- Alternatives considered: Continue with hand-built HTML controls; mix Fluent
  UI with native controls; replace the existing Studio client framework.
- Consequences: Studio UI work belongs in the Blazor client and the server
  hosts its built output. Shared Studio instructions and workspace agent
  contracts carry this rule. The Matchmaker static HTML/JavaScript page is
  being replaced by the Fluent UI client.
- Evidence: User direction in this session and existing Fluent UI usage in
  the [Decision Network Lab](../decision-network-lab/src/DecisionNetworkLab.Client/).
- Supersedes: ADR-001's open choice about whether the Studio should adopt
  the existing Blazor/Fluent UI implementation style.

## ADR-013 — Provide a shared Matchmaker server lifecycle command

- Date: 2026-10-06
- Status: Accepted
- Context: Agents and people need a dependable way to use the Matchmaker UI
  and HTTP API without remembering a manual server launch sequence or
  colliding with a server that is already running.
- Decision: Provide repository-owned `server.py status`, `ensure`, and
  `restart` actions. `ensure` checks the Matchmaker health endpoint first and
  starts the local server in a detached background process only when needed.
  `restart` stops only a process previously started and recorded by the helper,
  then starts it again. Use port 5190 by default, bind to the host's active
  private interface so the shared browser can reach it, verify readiness on
  that same address, and retain process metadata and startup output under the ignored
  run directory.
- Alternatives considered: Ask each agent to launch `dotnet run` directly;
  rely on a manually maintained server process; create a global machine
  service.
- Consequences: Studio agent instructions can point to one shared lifecycle
  tool, and the server remains available after the agent command exits.
  Externally launched processes are not terminated by the helper. Server
  lifecycle remains local to the repository, while the visualizer host remains
  a separate future integration. Binding the private interface allows browser
  access from the local network, so this development server is not an
  internet-facing deployment.
- Evidence: [Matchmaker server helper](tools/matchmaker/server.py) and
  [Matchmaker tool guide](tools/matchmaker/README.md).
- Supersedes: None
