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
