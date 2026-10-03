# Agent Tuning Studio — Implementation Plan

**Status:** planning; no Studio application code has been started.

This plan turns the product direction in the
[Studio overview](README.md) into small, evidence-driven steps. It is a
working plan, not a commitment to build every phase in order or to a fixed
technology stack. Update it as early experiments reveal what is useful.

## Goal

Build a local interface for selecting a Kaggriculture agent, inspecting and
changing its explicit tunable parameters, running controlled simulations, and
understanding the results. From a result, the user can open the corresponding
full match in a separate simulation viewer.

The first version should prove one complete, understandable experiment. It
should not attempt to become a general-purpose agent IDE or an all-purpose
game dashboard.

## Agreed product decisions

- Keep the full-match farm viewer in its own window.
- Make actual agent tuning and simulator runs the Studio's primary workflow;
  do not carry over the existing lab's lesson-first screens by default.
- Treat the existing Blazor/Fluent UI presentation and interactive Mermaid
  graphs as useful references, not required implementation choices.
- Preserve fixed game rules as facts; expose only declared beliefs and policy
  preferences as tunable values.
- Keep runs local, reproducible, and reviewable. Do not add network services,
  external model calls, or runtime LLM dependencies.
- Preserve the existing Decision Network Lab and visualizers while developing
  the Studio.

## Delivery sequence

### Phase 0 — Choose the first experiment

Agree on one existing agent, one parameter, an opponent, and a small fixed
seed-and-seat suite. Record the baseline values, simulator configuration,
expected evidence, and a falsifiable hypothesis before changing a parameter.

**Exit criteria**

- The selected parameter is an explicit policy or belief value, not a game
  rule.
- The baseline and candidate differ in only that named value.
- The run suite, opponent, configuration, and seat assignments are specified.
- The result measures both economic outcomes and relevant policy behavior.

### Phase 1 — Prove the experiment contract without a UI

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

### Phase 2 — Build the smallest useful Studio workflow

Create a local UI that can select the first supported agent, display its
documented tunable parameters and fixed facts distinctly, edit a candidate,
choose the agreed run controls, launch the experiment, and show its status.
Begin with a narrow adapter for the chosen agent rather than designing a
universal plug-in system in advance.

The UI technology remains open. Choose it after checking the local process
launch, simulator integration, and separate-viewer needs against the
maintenance cost of reusing the existing Blazor/Fluent UI lab.

**Exit criteria**

- The first experiment can be configured and launched from the UI.
- The baseline remains available for comparison.
- Invalid or failed runs are shown as failures, not successful-looking
  results.
- The UI does not directly change game rules or the live policy's defaults.

### Phase 3 — Compare results and open the separate viewer

Show baseline-versus-candidate outcomes with the experiment inputs and
behavior metrics. Provide a clear way to open the corresponding match in the
existing full-match viewer, in its own window.

**Exit criteria**

- A user can identify which parameters and controls produced each result.
- Results link to their retained match artifacts.
- Opening a match does not embed or replace the Studio.
- Viewer launch failures are visible to the user.

### Phase 4 — Add decision-graph inspection where it helps

Explore an interactive Mermaid decision graph as a contextual explanation of
the selected agent's evidence, beliefs, decisions, and utility. Reuse the
existing interactive graph approach if it helps users understand real
agent behavior; do not add graphs merely to reproduce the teaching lab.

**Exit criteria**

- Displayed graph information corresponds to the selected agent and its
  documented model.
- Tunable values and fixed game facts remain distinguishable.
- Graph inspection complements the experiment and result workflow.

### Phase 5 — Expand based on demonstrated use

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

1. Which agent and single parameter should anchor the first end-to-end
   experiment?
2. How should that agent declare parameter names, types, bounds, descriptions,
   and defaults without parsing arbitrary source code?
3. What artifact or launch contract will open a specific run in the separate
   visualizer?
4. Which local UI technology best supports the first complete workflow?

Resolve only the questions needed for the next phase; leave later choices open
until evidence makes them relevant.
