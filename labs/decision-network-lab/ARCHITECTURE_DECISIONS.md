# Architectural decisions

These records explain why the lab currently has its shape. New material
changes should add a decision rather than silently rewriting an old one.

## Decision summary

| ID | Title | Status | Date |
|---|---|---|---|
| ADR-001 | Keep the first experience in its own lab | Accepted | 2026-09-27 |
| ADR-002 | Start with a standalone Blazor WASM client | Accepted | 2026-09-27 |
| ADR-003 | Keep the calculation core separate from the UI | Accepted | 2026-09-27 |
| ADR-004 | Minimize external dependencies | Accepted | 2026-09-27 |
| ADR-005 | Begin with three one-time crop networks | Accepted | 2026-09-27 |
| ADR-006 | Render network diagrams as interactive SVG | Accepted | 2026-09-27 |
| ADR-007 | Make the lab reconstructable from source | Accepted | 2026-09-27 |
| ADR-008 | Defer model-review questions until the new carrot graph arrives | Accepted | 2026-09-27 |
| ADR-009 | Use MSTest for core evaluator tests | Accepted | 2026-09-27 |
| ADR-010 | Exchange Phase 2 evidence through frozen local packages | Accepted | 2026-09-27 |
| ADR-011 | Prototype evidence review inside the existing client screen | Accepted | 2026-09-27 |
| ADR-014 | Use Fluent UI for general presentation controls | Accepted | 2026-09-27 |
| ADR-015 | Use Mermaid for the interactive network SVG | Accepted | 2026-09-27 |
| ADR-016 | Keep evaluation focused and refresh the local sample automatically | Accepted | 2026-09-28 |
| ADR-017 | Load Phase 2A evidence through local browser file selection | Accepted | 2026-09-28 |
| ADR-018 | Separate match recordings, replays, and agent provenance | Accepted | 2026-09-28 |

## ADR-001 — Keep the first experience in its own lab

- Date: 2026-09-27
- Status: Accepted
- Context: The existing repository contains live agents, decision documents,
  experiments, and a separate TypeScript visualizer. The first user
  experience needs room to evolve without turning those areas into one product.
- Decision: Place the first decision-network experience under
  `labs/decision-network-lab`.
- Alternatives considered: Put the UI beside the live Python submission;
  replace the existing TypeScript visualizer; create a new top-level
  repository.
- Consequences: The lab is easy to work on in its own chat or worktree, but it
  must explicitly document which source materials it learns from.
- Evidence: The shared `labs/AGENTS.md` collaboration rules.
- Supersedes: None.

## ADR-002 — Start with a standalone Blazor WASM client

- Date: 2026-09-27
- Status: Accepted
- Context: The first experience needs immediate interactive feedback and does
  not yet need protected server behavior or external API calls.
- Decision: Use a standalone Blazor WebAssembly client for the first slice.
- Alternatives considered: Hosted Blazor WASM with an ASP.NET Core server;
  port the existing TypeScript visualizer; build a server-rendered application.
- Consequences: The first slice can run locally and be statically hosted. A
  later server can be added at an explicit boundary if a concrete need arises.
- Evidence: The client-only sequence documented in `README.md`.
- Supersedes: None.

## ADR-003 — Keep the calculation core separate from the UI

- Date: 2026-09-27
- Status: Accepted
- Context: UI work and model work should be possible in separate chats without
  either side needing to modify the other.
- Decision: Put network definitions, scenario types, belief calculations,
  utility calculations, and explanations in `DecisionNetworkLab.Core`. Keep
  Razor components and styling in `DecisionNetworkLab.Client`.
- Alternatives considered: Put calculations directly in Razor components;
  create a shared UI/model project; call the Python agent from the page.
- Consequences: The client has a small stable contract. The model can be
  tested or reused without a browser, and the UI cannot quietly create a
  second decision engine.
- Evidence: The project reference direction and core class diagram in
  `README.md`.
- Supersedes: None.

## ADR-004 — Minimize external dependencies

- Date: 2026-09-27
- Status: Accepted
- Context: The lab should remain credible and maintainable without collecting
  a large package ecosystem.
- Decision: Use the Microsoft Blazor framework packages supplied by the
  project template and our own CSS/SVG. Do not add a third-party component
  library unless a concrete need is documented and approved.
- Alternatives considered: Fluent UI component packages; another chart or
  diagram library; a JavaScript graph framework.
- Consequences: The first UI may require more local markup, but its behavior
  and styling remain easy for a beginner to inspect.
- Evidence: The dependency policy in `labs/AGENTS.md` and the generated
  project files.
- Supersedes: None.

## ADR-005 — Begin with three one-time crop networks

- Date: 2026-09-27
- Status: Accepted
- Context: The repository contains carrot, wheat, and game `MELON` as the
  current one-time crop examples. Tomato and strawberry use ongoing scheduled
  production and are a later learning step.
- Decision: Implement carrot, wheat, and melon/watermelon in the first lab
  experience. Carrot uses the first coarse belief model; wheat and melon use
  transparent point estimates.
- Alternatives considered: Include tomato and strawberry immediately; combine
  all crops in a portfolio optimizer; invent a fourth one-time network.
- Consequences: The first experience stays aligned with the existing teaching
  sequence and makes model maturity visible rather than pretending all crops
  have equal evidence.
- Evidence: The crop decision-network documents and the current core catalog.
- Supersedes: None.

## ADR-006 — Render network diagrams as interactive SVG

- Date: 2026-09-27
- Status: Accepted
- Context: The lab is intended to teach system and decision-network structure,
  and UML-style diagrams are an important part of the user's understanding.
  Static cards and a separate text edge list do not make relationships easy to
  follow.
- Decision: Render the current network definition as an SVG diagram in the
  Blazor client. Draw arrowed information-flow edges and make each node
  selectable by mouse or keyboard.
- Alternatives considered: Static cards only; a third-party graph library;
  Mermaid rendered as a separate document.
- Consequences: The UI owns layout and interaction, while the core continues
  to own node meaning and network data. The first layout is intentionally
  simple and may evolve as the networks become more complex.
- Evidence: `DecisionNetworkDiagram.razor` and the interactive diagram
  sequence in `README.md`.
- Supersedes: None.

## ADR-007 — Make the lab reconstructable from source

- Date: 2026-09-27
- Status: Accepted
- Context: A Python virtual environment can be recreated from a dependency
  description. The Blazor lab needs the same practical property without
  committing SDK installations, package caches, or build output.
- Decision: Pin the .NET SDK with `global.json`, commit the client NuGet lock
  file, document restore commands, and ignore generated `bin/` and `obj/`
  directories.
- Alternatives considered: Commit the NuGet cache; commit build output;
  require each machine to choose its own SDK; use a container immediately.
- Consequences: A new checkout still needs the pinned .NET SDK installed and
  network access for the first package restore, but the source repository
  describes exactly what to restore and build.
- Evidence: `README.md`, `global.json`, and the client package lock file.
- Supersedes: None.

## ADR-008 — Defer model-review questions until the new carrot graph arrives

- Date: 2026-09-27
- Status: Accepted
- Context: The current lab has a first-pass carrot belief model and provisional
  wheat/melon point estimates. A newer carrot decision graph is being developed
  in another project and will change what should be reviewed or modeled.
- Decision: Treat browser validation and node selection as complete for the
  current three graphs, but defer the `MarketInventory` modeling decision and
  final source-assumption review until the new carrot graph is explicitly
  provided.
- Alternatives considered: Resolve the current assumptions immediately;
  block all Phase 1 work; silently merge the other project's graph later.
- Consequences: The test suite may proceed against the current explicit
  behavior, while model interpretation remains provisional and documented.
- Evidence: `README.md` Phase 1 checklist and `QUESTIONS.md` Q-008.
- Supersedes: None.

## ADR-009 — Use MSTest for core evaluator tests

- Date: 2026-09-27
- Status: Accepted
- Context: Phase 1 needs automated tests for the core calculations, but the
  lab deliberately started without a test framework or large dependency set.
- Decision: Add one MSTest-based core test project using the standard MSTest
  package family and a locked package graph. Keep browser tests and simulator
  integration outside this first suite.
- Alternatives considered: xUnit; a package-free console self-check; testing
  through the browser only.
- Consequences: `dotnet test` provides credible repeatable verification, at
  the cost of a small approved test dependency. The core remains independent
  of the Blazor client.
- Evidence: `tests/DecisionNetworkLab.Core.Tests/` and the passing Phase 1
  test run.
- Supersedes: None.

## ADR-010 — Exchange Phase 2 evidence through frozen local packages

- Date: 2026-09-27
- Status: Accepted
- Context: Phase 2 needs simulator results and decision traces to become
  understandable in the lab UI without making the UI a live simulator or
  adding a server prematurely.
- Decision: The first evidence producer writes a manifest, JSONL run
  summaries, and compressed decision traces to a local package. A later UI
  increment may import and review those frozen packages.
- Alternatives considered: Call the simulator directly from Blazor; add an
  ASP.NET Core API immediately; store results only in terminal output.
- Consequences: Evidence is reproducible and portable, while the UI remains
  client-side. The package schema becomes a contract that must be versioned as
  Phase 2 grows.
- Evidence: `evaluation/README.md` and `evaluation/run_carrot_experiment.py`.
- Supersedes: None.

## ADR-011 — Prototype evidence review inside the existing client screen

- Date: 2026-09-27
- Status: Accepted
- Context: Phase 2 evidence must become understandable in the same laboratory
  experience. A separate dashboard would make it harder to compare the model,
  decision, and evidence together.
- Decision: Add a compact, read-only evidence-review prototype below the model
  workspace in the existing Blazor client. Use a frozen fixture derived from
  the first carrot package until the JSON package importer is designed.
- Alternatives considered: Create a separate dashboard; add a server API
  immediately; make the browser run the simulator directly.
- Consequences: We can refine the information density and explanatory layout
  before committing to an importer or server boundary. The fixture must remain
  visibly marked as prototype evidence.
- Evidence: `ExperimentEvidencePrototype.razor` and the Phase 2 section in
  `README.md`.
- Supersedes: None.

## ADR-012 — Use a narrow local simulator for Phase 1.5 feedback

- Date: 2026-09-27
- Status: Accepted
- Context: The first screen currently recalculates utility when a learner
  changes a number, but it does not show a causal trace or an outcome from a
  reproducible run. The full Kaggriculture Python simulator cannot run inside
  the Blazor WebAssembly client without adding a server or a separate runtime.
- Decision: Add a small deterministic C# simulator for the three existing
  one-time crop examples. It consumes the same core evaluation result as the
  UI, returns visible lifecycle steps, and uses an explicit seed. Keep the
  Python Kaggriculture simulator as the authority for batch matches and
  evidence packages.
- Alternatives considered: Launch Python from a server API immediately; try
  to compile the full Python simulator into the browser; keep the screen
  description-only; make the browser simulator authoritative for game rules.
- Consequences: The learner gets immediate local feedback without cloud or
  server dependencies. The local simulator must remain visibly educational,
  and its assumptions must not be copied into `main.py` without matched
  Python evidence.
- Evidence: `InteractiveSimulator.cs`, `DecisionTraceModels.cs`, and the
  Phase 1.5 section in `README.md`.
- Supersedes: ADR-010 only for immediate interactive teaching feedback;
  ADR-010 remains the evidence-package boundary.

## ADR-013 — Share structured decision traces between core and UI

- Date: 2026-09-27
- Status: Accepted
- Context: The first UI displayed long explanation strings and could not show
  which network values changed after an input was adjusted.
- Decision: The core evaluator returns named action values and ordered trace
  steps containing node IDs, roles, values, and explanations. The client
  renders those records in the same evidence-to-decision order as the SVG
  diagram. The SVG remains directly selectable through its own interaction.
- Alternatives considered: Recompute explanations in Razor components;
  attach behavior directly to SVG nodes; keep only a list of prose strings.
- Consequences: Core calculations remain independent of Blazor while the UI
  can become more interactive. Trace wording and node IDs are now part of the
  small local contract and should be tested when the graph changes. A later UI
  increment may connect trace selection to SVG selection.
- Evidence: `DecisionResult`, `DecisionTraceModels.cs`, and
  `InteractiveEvaluationPanel.razor`.
- Supersedes: None.

## ADR-014 — Use Fluent UI for general presentation controls

- Date: 2026-09-27
- Status: Accepted
- Context: The Phase 1.5 screen had accumulated hand-built controls, cards,
  and input styling. The user wants a visually robust interface that remains
  understandable and not oversized.
- Decision: Use the approved `Microsoft.FluentUI.AspNetCore.Components` v5
  package for general cards, buttons, badges, selects, sliders, number inputs,
  providers, and the component stylesheet. Keep the custom dark palette,
  teaching-specific layout, evidence table, and interactive SVG local.
- Alternatives considered: Continue expanding hand-built CSS; use the Fluent
  Web Components/npm route; add a larger design-system or chart framework.
- Consequences: The UI gains consistent accessible controls and fewer local
  style rules at the cost of one additional NuGet package and a larger client
  dependency graph. The calculation core remains independent of the package.
- Evidence: `DecisionNetworkLab.Client.csproj`, `Program.cs`, `index.html`,
  and the Fluent migration in the Phase 1.5 client components.
- Supersedes: The presentation portion of ADR-004; the dependency-minimizing
  boundary remains in force for the core and simulator.

## ADR-015 — Use Mermaid for the interactive network SVG

- Date: 2026-09-27
- Status: Accepted
- Context: The network diagram should be derived from a readable Mermaid
  topology and remain interactive in the Blazor experience. The previous
  renderer duplicated layout decisions in C# and SVG markup.
- Decision: Generate Mermaid flowchart text from the core network definition,
  render it in the browser with the pinned Mermaid 12.0.0 ESM module from
  jsDelivr, and bridge trusted node-click callbacks through JavaScript
  interop. Keep the C# model as the source of truth and keep the live
  Kaggriculture agent independent of this browser dependency.
- Alternatives considered: Continue maintaining hand-positioned SVG; use the
  Mermaid CLI to produce static SVG files; add a separate graph visualization
  framework.
- Consequences: Mermaid owns layout and SVG generation while Blazor owns
  selected-node state and explanations. The lab currently has a versioned CDN
  runtime dependency; if offline reconstruction becomes a requirement, vendor
  the pinned module or move rendering to a build step.
- Evidence: `MermaidDefinitionBuilder.cs`,
  `MermaidDecisionNetworkDiagram.razor`, and `wwwroot/js/mermaid-interop.js`.
- Supersedes: The diagram-rendering portion of ADR-006; the custom SVG
  interaction boundary is replaced by Mermaid plus JS interop.

## ADR-016 — Keep evaluation focused and refresh the local sample automatically

- Date: 2026-09-28
- Status: Accepted
- Context: The first Evaluation card repeated the entire decision trace and
  required a separate button to run a narrow seeded browser simulation. The
  scenario controls already recalculate the expected evaluation immediately.
- Decision: Label observed values as current game state and keep forecasting
  values under model assumptions. Remove the repeated full trace from the
  Evaluation card. Refresh the seeded local sample automatically when the
  scenario or seed changes, while keeping the core trace available to the
  Mermaid diagram and evidence surfaces.
- Alternatives considered: Keep the manual Run button; repeat the trace in
  every surface; make the seed part of game state; connect the browser to the
  Python simulator.
- Consequences: The first screen gives immediate feedback with less visual
  duplication. The sample remains a deterministic teaching realization, not a
  full daily game simulation or authoritative batch result.
- Evidence: `ScenarioControls.razor`, `InteractiveEvaluationPanel.razor`,
  `Home.razor.cs`, and the Phase 1.5 acceptance notes in `README.md`.
- Supersedes: The Evaluation-card presentation portion of ADR-013; the
  structured trace contract remains in the core model.

## ADR-017 — Load Phase 2A evidence through local browser file selection

- Date: 2026-09-28
- Status: Accepted
- Context: Existing experiment runs are produced in the Agent and Opponent
  Selection workflow. The Decision Network Lab should review those artifacts
  without copying simulator history into this worktree or starting Python from
  the browser.
- Decision: The first evidence reader accepts user-selected `manifest.json`,
  `comparison-summary.json`, and `run-summaries.jsonl` files through the
  browser's local file input. Core validation normalizes the JSON into the
  existing UI-friendly evidence model. Compressed decision traces remain a
  separate follow-up increment.
- Alternatives considered: Copy run packages into the lab; add an API server;
  run the Python simulator from Blazor; accept arbitrary unversioned files.
- Consequences: The lab remains client-side and does not duplicate experiment
  data. The user must select the package files, and trace previews initially
  indicate that compressed trace import is pending.
- Evidence: `PHASE2_EVIDENCE_CONTRACT.md`, `EvidencePackageReader.cs`, and
  `ExperimentEvidencePrototype.razor`.
- Supersedes: The importer portion of ADR-010 remains planned, with the local
  browser file boundary now selected for its first implementation.

## ADR-018 — Separate match recordings, replays, and agent provenance

- Date: 2026-09-28
- Status: Accepted
- Context: Phase 2 has several existing source types. Local match history
  contains recorded matches and an index; the Kaggriculture visualizer contains
  replay JSON; agent folders contain Python source. Treating these as one file
  format would blur what was observed and what was executable source.
- Decision: Group the Phase 2 UI into Local match history and Replay recordings.
  Keep `opponent-experiment/` and `agents/` as provenance metadata only. Add
  separate adapters behind a common evidence model.
- Alternatives considered: Combine all JSON into one generic list; execute
  agents from the browser; copy source and recordings into the lab; treat
  visualizer replays as equivalent to match-history records.
- Consequences: The user can tell what kind of recording is being reviewed.
  Match-history catalog loading can proceed before full replay support, and
  provenance can be shown without expanding the runtime boundary.
- Evidence: `PHASE2_EVIDENCE_CONTRACT.md`, `MatchHistoryReader.cs`, and
  `EvidenceReviewPanel.razor`.
- Supersedes: None.

## ADR-020 — Use a minimal local host for configured evidence directories

- Date: 2026-09-28
- Status: Accepted
- Context: The user wants the lab to load the existing match-history directory
  automatically. Blazor WebAssembly cannot read an arbitrary Windows path
  without a user file selection, so the file picker did not satisfy the
  intended workflow.
- Decision: Add a small ASP.NET Core Server project with no third-party
  dependencies. It serves the built client and exposes one read-only
  `/api/match-history` endpoint. The endpoint reads only the configured
  `index.json` and agent-catalog source, normalizes their descriptive metadata,
  and never executes Python or copies recordings.
- Alternatives considered: Keep the file picker as the primary workflow; add a
  cloud API; execute the Python launcher from the browser; copy the sibling
  worktree into the lab.
- Consequences: The normal command is now the Server project, and the source
  directory is configured in `src/DecisionNetworkLab.Server/appsettings.json`.
  The live contest agent remains entirely separate and local.
- Evidence: `DecisionNetworkLab.Server.csproj`, `Program.cs`,
  `appsettings.json`, and `AgentMetadataReader.cs`.
- Supersedes: The client-only primary-loading portion of ADR-017.

## ADR-021 — Complete Phase 2 with separate recording adapters

- Date: 2026-09-28
- Status: Accepted
- Context: Match history, visualizer replays, and frozen evidence packages are
  related but have different schemas and meanings.
- Decision: Keep three visible source groups in the UI. Match history and
  replay files share a day-by-day recording timeline after separate adapters
  normalize them. Frozen packages use the existing evidence reader and load
  compressed decision-trace previews through the local host.
- Alternatives considered: Merge all JSON into one generic record type; show
  only summary rows; execute the visualizer or Python agent from the browser.
- Consequences: The user can move from “who played whom?” to “what happened
  on day 7?” without confusing a replay with a match-history record or a
  policy trace with simulator truth.
- Evidence: `RecordedMatchReader.cs`, `ReplayRecordingReader.cs`,
  `RecordingTimeline.razor`, and `FrozenEvidenceReview.razor`.
- Supersedes: The deferred adapter portions of ADR-018.

## ADR-019 — Show local match history as an expandable catalog

- Date: 2026-09-28
- Status: Accepted
- Context: The existing local match-history index already contains enough
  metadata to answer the first review question—who played whom, who won, and
  under which seed and run conditions—without reading every large recording.
- Decision: Load the user-selected `index.json` in memory and render one
  expandable list item per match. Show the pairing and winner in the collapsed
  summary; show rewards, seed, season, statuses, timestamp, source, ID, and
  recording path when expanded.
- Alternatives considered: Copy all recordings into the lab; load every full
  recording before displaying the list; treat agent source descriptions as if
  they were fields in the match index.
- Consequences: The first review surface is fast and descriptive. Opening a
  full recording and enriching a row from agent provenance remain separate
  adapters. The browser still needs the user to select the producer's
  `index.json` because Blazor WebAssembly cannot browse an arbitrary local
  directory.
- Evidence: `MatchHistoryReader.cs`, `MatchHistoryCatalog.razor`, and
  `PHASE2_EVIDENCE_CONTRACT.md`.
- Supersedes: None.
