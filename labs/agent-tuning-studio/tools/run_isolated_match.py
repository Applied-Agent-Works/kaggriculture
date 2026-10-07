#!/usr/bin/env python3
"""Run one reproducible local match into a unique Studio run directory."""

from __future__ import annotations

import argparse
from datetime import datetime, timezone
import json
from pathlib import Path
import subprocess
import sys
import uuid


REPO_ROOT = Path(__file__).resolve().parents[3]
RUNS_ROOT = Path(__file__).resolve().parents[1] / "runs"
BUILTIN_AGENTS = {"pass", "random", "starter"}
MAX_STEPS = 720


def _repository_file(value: str, label: str) -> str:
    if value in BUILTIN_AGENTS:
        return value

    candidate = (REPO_ROOT / value).resolve()
    try:
        candidate.relative_to(REPO_ROOT)
    except ValueError as error:
        raise ValueError(f"{label} must be a path inside the repository") from error

    if not candidate.is_file():
        raise ValueError(f"{label} does not identify a repository file: {value}")

    return candidate.relative_to(REPO_ROOT).as_posix()


def _write_json(path: Path, value: object) -> None:
    path.write_text(json.dumps(value, indent=2) + "\n", encoding="utf-8")


def _source_revision() -> str:
    result = subprocess.run(
        ["git", "rev-parse", "HEAD"],
        cwd=REPO_ROOT,
        check=True,
        capture_output=True,
        text=True,
    )
    return result.stdout.strip()


def _run_id() -> str:
    timestamp = datetime.now(timezone.utc).strftime("%Y%m%dT%H%M%SZ")
    return f"{timestamp}-{uuid.uuid4().hex[:12]}"


def main() -> int:
    parser = argparse.ArgumentParser(
        description="Run a fixed-seed Kaggriculture match in an isolated Studio directory."
    )
    parser.add_argument("--agent", default="main.py")
    parser.add_argument("--opponent", default="random")
    parser.add_argument("--steps", type=int, default=24)
    parser.add_argument("--seed", type=int, required=True)
    args = parser.parse_args()

    if not 1 <= args.steps <= MAX_STEPS:
        parser.error(f"--steps must be between 1 and {MAX_STEPS}")

    try:
        agent = _repository_file(args.agent, "agent")
        opponent = _repository_file(args.opponent, "opponent")
        revision = _source_revision()
    except (ValueError, subprocess.CalledProcessError) as error:
        parser.error(str(error))

    run_directory = RUNS_ROOT / _run_id()
    run_directory.mkdir(parents=True, exist_ok=False)
    started_at = datetime.now(timezone.utc).isoformat()
    command = [
        sys.executable,
        "tools/run_match.py",
        "--agent",
        agent,
        "--opponent",
        opponent,
        "--steps",
        str(args.steps),
        "--seed",
        str(args.seed),
    ]
    manifest = {
        "schema": "kaggriculture-agent-tuning-studio-run/v1",
        "run_id": run_directory.name,
        "started_at": started_at,
        "source_revision": revision,
        "repository": str(REPO_ROOT),
        "agent": agent,
        "opponent": opponent,
        "steps": args.steps,
        "seed": args.seed,
        "command": command,
    }
    _write_json(run_directory / "manifest.json", manifest)

    completed = subprocess.run(
        command,
        cwd=REPO_ROOT,
        check=False,
        capture_output=True,
        text=True,
    )
    (run_directory / "stdout.txt").write_text(completed.stdout, encoding="utf-8")
    (run_directory / "stderr.txt").write_text(completed.stderr, encoding="utf-8")
    finished_at = datetime.now(timezone.utc).isoformat()
    _write_json(
        run_directory / "status.json",
        {
            "started_at": started_at,
            "finished_at": finished_at,
            "exit_code": completed.returncode,
            "succeeded": completed.returncode == 0,
        },
    )
    print(run_directory.relative_to(REPO_ROOT))
    return completed.returncode


if __name__ == "__main__":
    raise SystemExit(main())
