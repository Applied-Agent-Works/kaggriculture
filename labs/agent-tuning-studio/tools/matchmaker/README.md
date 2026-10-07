# Matchmaker tool

This is the future home of the Agent Tuning Studio's matchmaker adapter.

## Responsibility

- Select agents and opponents.
- Define reproducible match controls.
- Run bounded local matches.
- Store and retrieve manifests, statuses, summaries, and replay references.
- Open a selected match or replay in the separate full-match visualizer.

The currently implemented isolated operation remains at
[`../run_isolated_match.py`](../run_isolated_match.py). It writes one unique
ignored run directory per invocation and records the source revision, seed,
agent, opponent, and step count.

## Not this tool

Interpreting outcomes belongs to
[`../match_analyzer/`](../match_analyzer/).

Catalog persistence, replay indexing, display launching, and a richer match
configuration API are deliberately not implemented yet.
