---
name: Agent Tuning Studio
description: "Use when working on Agent Tuning Studio tools, experiment runs, agent discovery, reproducibility, or concurrent local workflows."
applyTo: "labs/agent-tuning-studio/**"
---

# Agent Tuning Studio guidance

- Work in agent-tuning-studio directory and its children  
- Unless told otherwise keep code clean and ask permission to install packages. in general fewer external packages is easier to read.
- Readability is very important
- Keep Studio coordination and tooling under `labs/agent-tuning-studio/`.
- All Agent Tuning Studio controls and data-management screens must use Fluent
  UI for Blazor. Use Fluent components for interactive controls, status
  indicators, cards, progress, and messages. Do not introduce a second control
  library or hand-built HTML controls. Semantic layout and typography may use
  HTML and local CSS. The separate full-match visualizer remains its own
  specialized visualization surface.
- Put Studio UI work in `src/Matchmaker.Client/` and keep the server's
  `wwwroot` free of a parallel hand-built application UI.
- Before using the Matchmaker UI or API, run
  `.venv/bin/python labs/agent-tuning-studio/tools/matchmaker/server.py ensure`.
  It checks health and starts the local server when it is unavailable;
  use its `restart` action when a restart is needed.
- Keep the game-time agent local and independent of Studio tooling.
- Treat game rules as fixed facts; expose only declared beliefs and policy
  preferences as experiment inputs.
- Give every run a unique output directory and write its manifest before
  interpreting its results.
- Preserve source revision, agent, opponent, seed, seat, step count, and
  configuration in run provenance.
- Never let concurrent runs share mutable output files.
- Do not edit live policy source as part of running an experiment.
- Do not call a candidate better without matched controls and behavioral as
  well as economic evidence.
- Keep the separate full-match visualizer separate from the Studio.
- Do not add external model calls, credentials, or service dependencies
  without a new approved architecture decision.

See `README.md`, `IMPLEMENTATION_PLAN.md`, `QUESTIONS.md`, and
`ARCHITECTURE_DECISIONS.md` in this directory for the durable workflow and
open decisions.
