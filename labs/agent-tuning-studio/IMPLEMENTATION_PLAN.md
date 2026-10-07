# Agent Tuning Studio — Implementation Plan

**Status:** planning; no Studio application code has been started.

This is a staged working plan, not a fixed schedule or a commitment to a
particular UI stack. Adjust it as agent and simulation use reveals what is
valuable.

## Goal

Provide an interface for selecting and inspecting Kaggriculture agents and
matches, operate local simulation and visualization utilities through real
coding-agent tools, and support controlled tuning experiments. Longer term,
make completed run evidence available to a Microsoft Foundry agent for
advisory interpretation and tuning support.

## Agreed direction

- Prioritize selectable, inspectable agents and matches.
- Expose needed local operations through tools the coding agent can actually
  invoke.
- Keep the full simulation viewer in its own window.
- Let real agent experiments shape the UI; do not port lesson screens by
  default.
- Treat Blazor/Fluent UI styling and interactive Mermaid graphs as references,
  not required technology choices.
- Keep game facts separate from beliefs and policy preferences.
- Keep game-time decisions local and independent of Foundry.
- Do not connect external services until a separate provider, budget, evidence
  flow, and approval decision is explicitly made.

## Phases

### Phase 0 — Agent and match discovery

Define a narrow catalog of available agents and matches. Show identity,
documented properties, summaries, and available artifacts without inventing
metadata or concealing unavailable data.

**Exit criteria**

- Users can select an existing agent and inspect its identity/properties.
- Users can select a match and inspect its recorded result and artifacts.
- Missing or unsupported information is explicit.

### Phase 1 — Coding-agent utility tools

Identify the host's actual tool-extension mechanism and expose the smallest
useful local operations: inspect agents/matches, run a match, retrieve its
evidence, and open its visualization.

**Exit criteria**

- The coding agent can discover and invoke the operations as real tools.
- Match errors and progress are surfaced; runs preserve reproducibility data.
- A completed run can be opened in the separate viewer.
- The game policy does not depend on the tool layer.

### Phase 2 — First controlled tuning experiment

Choose one agent, one named belief or policy parameter, an opponent, and a
small fixed seed-and-seat suite. Record baseline values and define a
falsifiable hypothesis before changing anything.

**Exit criteria**

- Only the selected tunable parameter changes.
- Configuration, opponent, seeds, and seats are fixed across comparison runs.
- Economic outcomes and relevant policy behavior are recorded separately from
  any forecast-calibration metric.

### Phase 3 — Reproducible experiment evidence

Define a small versioned experiment record with agent/source identity,
parameters, opponent, seed, seat, simulator configuration, terminal status,
rewards, behavior metrics, and replay/match artifact references.

**Exit criteria**

- Baseline and candidate run without mutating the live default policy.
- Repeated equal inputs reproduce the reported result.
- Evidence can be traced to its inputs and match artifacts.

### Phase 4 — Minimal tuning UI

Extend agent/match discovery with controls to edit a candidate, select the
agreed run parameters, start a run, and see explicit status/results. Start
with a narrow adapter for the first agent, not a universal plugin framework.

### Phase 5 — Comparison and separate viewer

Present baseline/candidate outcomes and their inputs. Link each result to its
match artifact and open it in the separate full-match visualizer.

### Phase 6 — Useful decision-graph context

Add interactive Mermaid decision graphs only when they explain the selected
agent's actual evidence, beliefs, decisions, or utility. Keep facts and
tunable assumptions visually distinct.

### Phase 7 — Foundry advisory interpretation

After local runs and evidence are useful, define a reviewed evidence handoff
to a Foundry agent. Its role is to interpret results and suggest hypotheses or
candidate changes—not to take live actions, mutate policy, or declare success.
Natural language, "Beat the baseline," and selection suggestions remain
exploratory.

This phase is blocked until an explicit architecture decision approves the
provider, budget, evidence flow, and human-approval boundary. Do not make
external calls while that decision is open.

### Phase 8 — Expand from demonstrated need

Add more agents, parameters, metrics, or workflows only when the first
end-to-end workflow demonstrates a concrete need.

## Experiment invariants

- Change one named parameter per comparison.
- Keep seeds, seats, opponent, simulator version, and configuration fixed.
- Record source revisions and baseline/candidate values.
- Do not tune fixed game rules.
- Do not call a change better from a lucky match.
- Do not submit to Kaggle, provision cloud resources, or alter credentials as
  part of this work.

See [QUESTIONS.md](QUESTIONS.md) for decisions that still gate implementation.
