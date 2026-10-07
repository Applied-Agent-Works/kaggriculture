# Matchmaker tool

This is the home of the Agent Tuning Studio's local Matchmaker adapter.

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

The metadata catalog is managed by
[`catalog.py`](catalog.py). It supports CRUD for agent and match records:

```bash
.venv/bin/python labs/agent-tuning-studio/tools/matchmaker/catalog.py agents list
.venv/bin/python labs/agent-tuning-studio/tools/matchmaker/catalog.py agents create \
  --id agents/carrot/decision.py \
  --label "Carrot decision" \
  --kind repository \
  --entry-point agents/carrot/decision.py
.venv/bin/python labs/agent-tuning-studio/tools/matchmaker/catalog.py matches create \
  --id carrot-smoke-42 \
  --agent agents/carrot/decision.py \
  --opponent pass \
  --seed 42 \
  --steps 24
```

The same records are available through the local Matchmaker server at
`/api/matchmaker/agents` and `/api/matchmaker/matches`, with list, get, create,
update, and delete operations. Removing a record never deletes an agent source
file or a match artifact. Match CRUD records controls and provenance; it does
not run the simulator.

## Not this tool

Interpreting outcomes belongs to
[`../match_analyzer/`](../match_analyzer/).

Replay indexing, display launching, and match execution from the server UI
remain separate follow-up operations.
