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
