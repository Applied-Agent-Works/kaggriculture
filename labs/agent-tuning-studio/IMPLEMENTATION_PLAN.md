# Agent Tuning Studio — Implementation Plan

**Status:** planning; no Studio application code has been started.

This plan turns the product direction in the
[Studio overview](README.md) into small, evidence-driven steps. It is a
working plan, not a commitment to build every phase in order or to a fixed
technology stack. Update it as early experiments reveal what is useful.

## Goal

Build an interface where existing Kaggriculture agents and matches can be
selected and inspected, local operations are available through real
coding-agent tools, and controlled simulation results can be reviewed. The
longer-term direction is to make run evidence available to a Microsoft Foundry
agent for advisory interpretation and tuning support. From a result, the user
can open the corresponding full match in a separate simulation viewer.

The first version should prove one complete, understandable experiment. It
should not attempt to become a general-purpose agent IDE or an all-purpose
game dashboard.

## Agreed product decisions

- Keep the full-match farm viewer in its own window.
- Prioritize making agents and matches selectable and viewable before broad
  parameter-tuning or natural-language features.
- Expose needed operations such as running simulations and opening the
  visualizer through real coding-agent tools, not only UI controls.
- Do not carry over the existing lab's lesson-first screens by default.
- Treat the existing Blazor/Fluent UI presentation and interactive Mermaid
  graphs as useful references, not required implementation choices.
- Preserve fixed game rules as facts; expose only declared beliefs and policy
  preferences as tunable values.
- Keep Kaggriculture's game-time agent local and independent of Foundry.
  Foundry is a planned, separate advisory layer for interpreting completed run
  evidence; it must not choose live actions or silently mutate agent policy.
- A Foundry connection requires a future explicit architecture decision naming
  the provider, budget, and permitted data flow. Until then, do not add
  external calls, credentials, or service dependencies.
- Preserve the existing Decision Network Lab and visualizers while developing
  the Studio.

## Delivery sequence

### Phase 0 — Make agents and matches discoverable

Define what the Studio means by an available agent and an available match.
Show agent identity and useful properties, and make existing match results
selectable and inspectable. Prefer a small, truthful catalog over a broad
agent plug-in architecture.

**Exit criteria**

- A user can select an existing agent and inspect its identity and available
  properties.
- A user can select an existing match and inspect its recorded summary and
  available artifacts.
- Unsupported or unavailable properties/artifacts are reported explicitly.

### Phase 1 — Expose useful local operations as coding-agent tools

Keep the coding-agent tool surface separated by responsibility:

- **Matchmaker** selects agents, configures and runs local matches, stores and
  retrieves match records, locates replay artifacts, and opens the full-match
  visualizer in its own window.
- **Match Analyzer** retrieves completed evidence and compares controlled
  runs.

Provide the smallest useful operations needed to inspect agents and matches,
run a local match, retrieve its evidence, and open its visualization. These
must be real callable tools available to the coding agent, not merely buttons
that perform a similar action only inside the UI. The first implementation
only binds the isolated Matchmaker runner; display launching and analysis
remain skeleton capabilities until their artifact handoffs are defined.

**Exit criteria**

- The coding agent can discover and invoke the agreed local operations through
  its actual tool interface.
- A match run reports progress and errors explicitly and retains its
  reproducibility inputs and result artifacts.
- A completed match can be opened in the separate full-match viewer.
- The game-time policy remains independent of the coding-agent tool layer.

### Phase 2 — Choose and prove the first controlled tuning experiment

Agree on one existing agent, one parameter, an opponent, and a small fixed
seed-and-seat suite. Record the baseline values, simulator configuration,
expected evidence, and a falsifiable hypothesis before changing a parameter.

**Exit criteria**

- The selected parameter is an explicit policy or belief value, not a game
  rule.
- The baseline and candidate differ in only that named value.
- The run suite, opponent, configuration, and seat assignments are specified.
- The result measures both economic outcomes and relevant policy behavior.

### Phase 3 — Prove the experiment contract without relying on UI state

Use the local simulator to run the baseline and candidate through the same
controlled suite. Define a small, versioned experiment record containing
enough information to reproduce a run and connect it to its output.

At minimum, retain agent identity and source revision, parameter values,
opponent, seed, seat, game configuration, terminal status and rewards, and
the chosen behavior metrics. Preserve the match/replay output needed by the
separate viewer.

**Exit criteria**

- A baseline and candidate can be run without editing the live policy source
  between runs.
- Repeating the same experiment inputs reproduces the same reported results.
- Candidate parameters cannot silently mutate the baseline.
- A result can be traced to its inputs and corresponding match artifact.

### Phase 4 — Build the smallest useful tuning workflow

Extend the agent and match discovery interface so a user can inspect the
chosen agent's documented tunable parameters and fixed facts distinctly,
edit a candidate, choose the agreed run controls, launch the experiment, and
show its status. Begin with a narrow adapter for the chosen agent rather than
designing a universal plug-in system in advance.

The UI technology remains open. Choose it after checking the local process
launch, simulator integration, and separate-viewer needs against the
maintenance cost of reusing the existing Blazor/Fluent UI lab.

**Exit criteria**

- The first experiment can be configured and launched from the UI.
- The baseline remains available for comparison.
- Invalid or failed runs are shown as failures, not successful-looking
  results.
- The UI does not directly change game rules or the live policy's defaults.

### Phase 5 — Compare results and open the separate viewer

Show baseline-versus-candidate outcomes with the experiment inputs and
behavior metrics. Provide a clear way to open the corresponding match in the
existing full-match viewer, in its own window.

**Exit criteria**

- A user can identify which parameters and controls produced each result.
- Results link to their retained match artifacts.
- Opening a match does not embed or replace the Studio.
- Viewer launch failures are visible to the user.

### Phase 6 — Add decision-graph inspection where it helps

Explore an interactive Mermaid decision graph as a contextual explanation of
the selected agent's evidence, beliefs, decisions, and utility. Reuse the
existing interactive graph approach if it helps users understand real
agent behavior; do not add graphs merely to reproduce the teaching lab.

**Exit criteria**

- Displayed graph information corresponds to the selected agent and its
  documented model.
- Tunable values and fixed game facts remain distinguishable.
- Graph inspection complements the experiment and result workflow.

### Phase 7 — Add Foundry advisory interpretation

After the local experiment and evidence workflow is useful, define a reviewed
handoff for completed run evidence to a Microsoft Foundry agent. The Foundry
agent may interpret results and help the user consider tuning options; it is
not the live player and does not automatically modify policy or declare a
candidate successful. Natural-language requests, use of the "Beat the
baseline" skill, and suggested candidate selections are hypotheses for
exploration, not committed interaction requirements.

This phase is blocked on an explicit architecture decision that identifies
the provider, budget, approved data flow, and human approval boundary. Do not
make external calls while that decision is open.

**Exit criteria**

- The Foundry agent receives bounded, attributable experiment evidence rather
  than an implicit view of arbitrary local state.
- Its interpretation distinguishes observed results from hypotheses and
  recommendations.
- Any proposed tuning change is a human-reviewed candidate and must pass
  matched local simulation before being called better.
- Provider, cost, and data-flow decisions have explicit approval.

### Phase 8 — Expand based on demonstrated use

Add more agents, parameters, opponents, metrics, or analysis only when the
first end-to-end workflow shows a concrete need. Keep agent-specific behavior
explicit and preserve the same experiment controls.

**Exit criteria**

- Each added agent exposes documented parameters through a tested adapter or
  an agreed metadata contract.
- Existing experiment records remain readable or have a clear migration path.
- Added capability does not weaken reproducibility or baseline preservation.

## Experiment and safety invariants

- Change one named parameter per comparison.
- Hold seeds, seat assignments, opponent, simulator version, and configuration
  fixed within a comparison.
- Record the source revision and parameter values for both baseline and
  candidate.
- Measure economic outcomes separately from prediction calibration.
- Do not claim a parameter is better from one lucky match.
- Do not tune or relabel fixed game rules to improve a result.
- Do not submit to Kaggle, provision cloud resources, or change credentials as
  part of the Studio workflow.

## Open questions that gate implementation choices

See the [question log](QUESTIONS.md) for the durable record. The first
implementation decisions depend on:

1. What agent and match properties should be shown first?
2. Which actual coding-agent tool interface should expose local run and viewer
   utilities?
3. Which match artifact or launch contract opens a specific run in the
   separate visualizer?
4. Which agent and single parameter should anchor the first controlled tuning
   experiment?
5. How should an agent declare parameter names, types, bounds, descriptions,
   and defaults without parsing arbitrary source code?
6. What evidence and approvals would a future Foundry interpretation workflow
   require?
7. Which local UI technology best supports the first complete workflow?

Resolve only the questions needed for the next phase; leave later choices open
until evidence makes them relevant.
