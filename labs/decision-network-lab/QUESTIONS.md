# Questions and answers

This log keeps the lab's questions durable across chats and worktrees. Open
questions are not failures; they show where the next investigation belongs.

## Open questions

### Q-001 — How should the lab connect to controlled local experiments?

- Asked: 2026-09-27
- Question: Should the browser import frozen JSON evidence, start a local
  process through a server, or remain a separate viewer for experiment files?
- Why it matters: The answer will determine the later evaluation boundary
  without making the first client-side experience dependent on a backend.
- Current evidence: The existing Python runner and memoization experiment are
  local, while the current lab has no server.
- Owner or next step: Compare the smallest artifact-exchange options after the
  first UI has been reviewed. The new carrot graph is a separate dependency
  and should be pointed to explicitly when ready.
- Status: Open

### Q-002 — When should the current point estimates become probability models?

- Asked: 2026-09-27
- Question: What evidence and resolving outcomes are sufficient to replace the
  wheat and melon point estimates with calibrated price distributions?
- Why it matters: The UI should teach the difference between a transparent
  placeholder and a measured belief.
- Current evidence: The crop documents explicitly describe the current point
  estimates as provisional.
- Owner or next step: Define one controlled experiment per crop after the
  initial experience is usable.
- Status: Open

## Answered questions

### Q-003 — What belongs in the first user experience?

- Asked: 2026-09-27
- Question: Should the first lab be a full simulator or a decision-network
  laboratory?
- Answer: It is a small decision-network laboratory. It lets a learner choose
  a crop, change visible assumptions, compare `PLANT` with `PASS`, and read an
  explanation.
- Evidence: The agreed planning discussion and this lab's scope in `README.md`.
- Answered: 2026-09-27
- Follow-up: Add full simulation only after the first experience shows what
  feedback is actually useful.
- Status: Answered

### Q-004 — Which networks are in the first scope?

- Asked: 2026-09-27
- Question: Which crop networks should the lab start with?
- Answer: Carrot, wheat, and melon/watermelon. Tomato and strawberry are
  ongoing-crop networks and remain a later phase.
- Evidence: The current Kaggriculture decision-network documents.
- Answered: 2026-09-27
- Follow-up: None for the first slice.
- Status: Answered

### Q-005 — Does the first slice require a server?

- Asked: 2026-09-27
- Question: Should an ASP.NET Core backend be created immediately?
- Answer: No. The first slice runs its decision calculations in Blazor WASM.
  A server may be added later for persistence, protected credentials, or
  external API integration.
- Evidence: The approved client-first architecture.
- Answered: 2026-09-27
- Follow-up: Revisit only when the artifact or API requirement is concrete.
- Status: Answered

### Q-007 — What remains before Phase 1 is complete?

- Asked: 2026-09-27
- Question: Is the first Decision Network Laboratory phase finished, or does
  it still need acceptance work?
- Answer: The first vertical slice and automated core tests are implemented.
  Browser checks and node selection are complete for all three crops. The
  `MarketInventory` decision and source-assumption review are deferred until
  the new carrot graph is available.
- Evidence: The Phase 1 checklist in `README.md`.
- Answered: 2026-09-27
- Follow-up: Resume the deferred model questions when the new carrot graph is
  available.
- Status: Answered

### Q-008 — What Phase 1 checks are intentionally deferred?

- Asked: 2026-09-27
- Question: Should `MarketInventory` and the final source-assumption review be
  resolved before the current lab moves forward?
- Answer: No. Browser validation and node selection are accepted for carrot,
  wheat, and melon. The `MarketInventory` decision and source-assumption
  review are deferred until the user supplies the new carrot decision graph
  from the other project.
- Evidence: Browser review confirmation and the Phase 1 checklist in `README.md`.
- Answered: 2026-09-27
- Follow-up: Resume these questions when the new carrot graph is available.
- Status: Answered

### Q-009 — How should Phase 2 evidence appear in the lab UI?

- Asked: 2026-09-27
- Question: Should the lab load frozen experiment packages, call a local
  evaluation service, or eventually provide both workflows?
- Why it matters: Phase 2 is not complete if evidence can only be generated
  in a terminal. The UI must make controls, results, traces, and provenance
  understandable without turning into a hidden simulator.
- Current evidence: The lab is currently client-side and has no artifact
  importer or evaluation API. The new local evidence producer is in
  `evaluation/run_carrot_experiment.py`; it generated a 12-run carrot
  package using the sibling `kaggle-environments` source checkout.
- Owner or next step: Define the evidence schema first, then sketch the first
  read-only experiment-review screen before choosing a server boundary.
- Status: Open

### Q-010 — Is the Phase 1.5 browser simulator authoritative?

- Asked: 2026-09-27
- Question: Should the new browser-local simulator replace the Python
  Kaggriculture simulator for evaluation?
- Answer: No. It is a seeded educational simulator for immediate feedback on
  the three one-time crop examples. The Python simulator remains authoritative
  for batch matches, lifecycle evidence, and contest-facing validation.
- Evidence: ADR-012 and the Phase 1.5 boundary in `README.md`.
- Answered: 2026-09-27
- Follow-up: Compare any policy change against fixed Python runs before
  treating it as an improvement.
- Status: Answered

### Q-011 — Which later phase owns the deferred backlog?

- Asked: 2026-09-27
- Question: Should the new carrot graph, full Python evidence importing,
  browser-to-Python simulation, formal sensitivity sweeps, and carrot
  graph/evaluator alignment be assigned to Phase 2, Phase 3, or separate
  phases?
- Answer: Not assigned yet. Keep these as a phase-unassigned backlog until
  the new carrot graph and the evidence architecture are available for a
  deliberate boundary decision.
- Evidence: The deferred backlog in `README.md` and the current architecture
  separation between the lab, Python simulator, and live agent.
- Owner or next step: Revisit when the new carrot graph is provided.
- Status: Open

### Q-006 — Can the lab be reconstructed on another machine?

- Asked: 2026-09-27
- Question: Can a new checkout restore the SDK and package dependencies without
  copying local virtual-environment-style folders?
- Answer: Yes. The lab pins the .NET SDK in `global.json`, records the client
  package graph in `packages.lock.json`, and rebuilds `bin/` and `obj/` locally.
- Evidence: The lab setup instructions and Microsoft SDK/NuGet guidance.
- Answered: 2026-09-27
- Follow-up: Add a setup script only if manual restore proves too repetitive.
- Status: Answered
