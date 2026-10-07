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
agent, opponent, step count, descriptive evidence, and raw `replay.json`.

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
not run the simulator until the explicit run operation is requested.

Run a planned match through the isolated runner:

```bash
.venv/bin/python labs/agent-tuning-studio/tools/matchmaker/catalog.py \
  matches run carrot-smoke-42
```

The server exposes the same operation as
`POST /api/matchmaker/matches/{id}/run`. It marks the record `running`, creates
a unique ignored run directory, and then records `succeeded` or `failed` with
the artifact path and exit/error information. Seat `1` swaps the selected
agent and opponent in the simulator while preserving the original catalog
roles.

## Ensure the local server is available

Before using the Matchmaker page or API, run the shared server helper from the
repository root:

```bash
.venv/bin/python labs/agent-tuning-studio/tools/matchmaker/server.py ensure
```

The helper checks `GET /api/matchmaker/status` at the host's active private
network address and prints that browser URL (port 5190 by default).
If Matchmaker is not responding, it starts the Server project in the
background, waits for its health endpoint, and writes startup output to the
ignored `runs/matchmaker-server.log`. Repeating `ensure` is safe; it reuses a
healthy server. Use `server.py status` to check without starting anything.
The detached process remains available after the agent command ends. Use
`server.py restart` when the running server needs a restart; the helper stops
only the process it launched and then starts it again. It refuses to stop an
unmanaged process. Process ownership records and logs are kept under the
ignored `runs/` directory. The server binds to the host's active private
interface so the shared browser can reach it; the API remains intended for
local development, not internet hosting. It can also use another port with
`--port`.

For a match with a retained replay, the raw replay is available from
`GET /api/matchmaker/matches/{id}/replay`. The endpoint serves only the
catalogued match's replay and does not expose arbitrary files from a run
folder. Matchmaker fetches this JSON and passes it to the visualizer through
the named window's message handoff.
The Fluent UI's **Run in visualizer** action also runs a planned match first,
then loads its replay in one reusable visualizer window. It can open any
retained replay artifact, including partial replays from failed runs; records
without a replay report that none is available. The visualizer host starts on
demand through `GET /api/matchmaker/visualizer` on port 5191. It uses the
checked-in visualizer source and a nearby Kaggle Environments checkout with
its pnpm dependencies installed. Set `KAGGRICULTURE_VISUALIZER_ROOT` when that
checkout cannot be discovered automatically.

## Not this tool

Interpreting outcomes belongs to
[`../match_analyzer/`](../match_analyzer/).

Replay indexing remains a follow-up operation. Outcome analysis belongs to
[`../match_analyzer/`](../match_analyzer/).
