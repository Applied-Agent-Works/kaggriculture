# Agent Tuning Studio instructions

These instructions apply to every change under this directory.

- All Agent Tuning Studio controls and data-management screens use Fluent UI
  for Blazor. Use Fluent components for buttons, fields, selects, checkboxes,
  badges, cards, progress, and messages.
- Semantic HTML and local CSS may provide page structure and typography, but
  do not introduce native interactive controls or another control library.
- Put application UI in `src/Matchmaker.Client/`. The server hosts the
  published Blazor client; do not create a parallel hand-built application
  under `src/Matchmaker.Server/wwwroot/`.
- Keep the separate full-match visualizer as its own specialized
  visualization surface.
- Preserve the local-only Matchmaker API and isolated run/artifact
  boundaries.
- Before using the Matchmaker UI or API, run
  `.venv/bin/python labs/agent-tuning-studio/tools/matchmaker/server.py ensure`.
  This shared helper checks health and starts the local server if needed;
  use `status` for a read-only check and `restart` when it must be restarted.
- Read `README.md`, `IMPLEMENTATION_PLAN.md`, `QUESTIONS.md`, and
  `ARCHITECTURE_DECISIONS.md` before changing the Studio's direction.
