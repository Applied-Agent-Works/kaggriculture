#!/usr/bin/env python3
"""Run successive matched rounds for one declared agent parameter.

The session creates wrapper entry points with explicit parameter objects. It
never edits the live agent source and never treats a positive round as proof
of general improvement.
"""

from __future__ import annotations

import argparse
from dataclasses import asdict, fields
from datetime import datetime, timezone
import importlib
import json
from pathlib import Path
import subprocess
import sys
import uuid


REPO_ROOT = Path(__file__).resolve().parents[4]
RUNS_ROOT = Path(__file__).resolve().parents[2] / "runs"
COMPARISON_RUNNER = Path(__file__).with_name("run_baseline_comparison.py")
MAX_STEPS = 720
sys.path.insert(0, str(REPO_ROOT))

ADAPTERS = {
    "carrot": {
        "default_module": "agents.carrot.shared",
        "default_name": "DEFAULT_PARAMETERS",
        "kind": "carrot",
        "paths": {"agents/carrot/decision.py"},
        "allowed_parameters": {
            field.name for field in fields(
                importlib.import_module("agents.carrot.shared").CarrotParameters
            )
        },
    },
    "melon": {
        "default_module": "agents.one_time_crop",
        "default_name": "DEFAULT_POINT_PRICE_PARAMETERS",
        "kind": "one_time_crop",
        "crop_module": "agents.melon.shared",
        "crop_name": "MELON",
        "paths": {"agents/melon/decision.py"},
        "allowed_parameters": {
            "future_price_multiplier",
            "pass_utility",
        },
    },
    "wheat": {
        "default_module": "agents.one_time_crop",
        "default_name": "DEFAULT_POINT_PRICE_PARAMETERS",
        "kind": "one_time_crop",
        "crop_module": "agents.wheat.shared",
        "crop_name": "WHEAT",
        "paths": {"agents/wheat/decision.py"},
        "allowed_parameters": {
            "future_price_multiplier",
            "pass_utility",
        },
    },
}


def _write_json(path: Path, value: object) -> None:
    path.write_text(json.dumps(value, indent=2) + "\n", encoding="utf-8")


def _session_id() -> str:
    timestamp = datetime.now(timezone.utc).strftime("%Y%m%dT%H%M%SZ")
    return f"{timestamp}-{uuid.uuid4().hex[:12]}"


def _source_revision() -> str:
    result = subprocess.run(
        ["git", "rev-parse", "HEAD"],
        cwd=REPO_ROOT,
        check=True,
        capture_output=True,
        text=True,
    )
    return result.stdout.strip()


def _repository_file(value: str, label: str) -> str:
    candidate = (REPO_ROOT / value).resolve()
    try:
        candidate.relative_to(REPO_ROOT)
    except ValueError as error:
        raise ValueError(f"{label} must be a path inside the repository") from error
    if not candidate.is_file():
        raise ValueError(f"{label} does not identify a repository file: {value}")
    return candidate.relative_to(REPO_ROOT).as_posix()


def _adapter_for(agent: str) -> tuple[str, dict[str, object]]:
    for name, adapter in ADAPTERS.items():
        if agent in adapter["paths"]:
            return name, adapter
    supported = ", ".join(sorted(path for data in ADAPTERS.values() for path in data["paths"]))
    raise ValueError(
        f"agent {agent!r} has no safe parameter adapter; supported agents: {supported}"
    )


def _parse_value(raw: str) -> object:
    try:
        return json.loads(raw)
    except json.JSONDecodeError as error:
        raise ValueError(
            f"parameter values must be JSON scalars (for example 0.35 or 1.1): {raw!r}"
        ) from error


def _parameter_defaults(adapter: dict[str, object]) -> dict[str, object]:
    module = importlib.import_module(str(adapter["default_module"]))
    defaults = getattr(module, str(adapter["default_name"]))
    return asdict(defaults)


def _validate_parameter(
    adapter: dict[str, object],
    parameter: str,
    value: object,
) -> None:
    allowed = adapter["allowed_parameters"]
    assert isinstance(allowed, set)
    if parameter not in allowed:
        raise ValueError(
            f"parameter {parameter!r} is not exposed by this adapter; "
            f"choose one of {', '.join(sorted(allowed))}"
        )
    if isinstance(value, bool) or not isinstance(value, (int, float)):
        raise ValueError(f"parameter {parameter!r} must be numeric, got {value!r}")


def _parameter_snapshot(
    defaults: dict[str, object],
    parameter: str,
    value: object,
) -> dict[str, object]:
    snapshot = dict(defaults)
    snapshot[parameter] = value
    return snapshot


def _wrapper_source(
    adapter: dict[str, object],
    parameter: str,
    value: object,
) -> str:
    if adapter["kind"] == "carrot":
        return (
            "from dataclasses import replace\n"
            "from agents.carrot.shared import DEFAULT_PARAMETERS, carrot_decision_agent\n"
            f"PARAMETERS = replace(DEFAULT_PARAMETERS, {parameter}={value!r})\n"
            "\n"
            "def agent(obs):\n"
            "    return carrot_decision_agent(obs, PARAMETERS)\n"
        )
    return (
        "from dataclasses import replace\n"
        "from agents.one_time_crop import (\n"
        "    DEFAULT_POINT_PRICE_PARAMETERS,\n"
        "    decision_agent,\n"
        ")\n"
        f"from {adapter['crop_module']} import {adapter['crop_name']}\n"
        f"PARAMETERS = replace(DEFAULT_POINT_PRICE_PARAMETERS, {parameter}={value!r})\n"
        "\n"
        "def agent(obs):\n"
        f"    return decision_agent(obs, {adapter['crop_name']}, PARAMETERS)\n"
    )


def _write_wrapper(
    path: Path,
    adapter: dict[str, object],
    parameter: str,
    value: object,
) -> str:
    path.write_text(_wrapper_source(adapter, parameter, value), encoding="utf-8")
    return path.relative_to(REPO_ROOT).as_posix()


def _challenge_path(stdout: str) -> Path | None:
    for line in reversed(stdout.splitlines()):
        candidate = (REPO_ROOT / line.strip()).resolve()
        if line.startswith("labs/agent-tuning-studio/runs/baseline-challenges/"):
            try:
                candidate.relative_to(REPO_ROOT)
            except ValueError:
                return None
            if (candidate / "summary.json").is_file():
                return candidate
    return None


def _run_round(
    round_directory: Path,
    *,
    baseline_wrapper: str,
    candidate_wrapper: str,
    opponent: str,
    seeds: list[int],
    steps: int,
    swap_seats: bool,
) -> tuple[int, Path | None]:
    command = [
        sys.executable,
        str(COMPARISON_RUNNER),
        "--baseline",
        baseline_wrapper,
        "--candidate",
        candidate_wrapper,
        "--opponent",
        opponent,
        "--steps",
        str(steps),
    ]
    for seed in seeds:
        command.extend(["--seed", str(seed)])
    if swap_seats:
        command.append("--swap-seats")

    completed = subprocess.run(
        command,
        cwd=REPO_ROOT,
        capture_output=True,
        text=True,
        check=False,
    )
    (round_directory / "runner-stdout.txt").write_text(
        completed.stdout, encoding="utf-8"
    )
    (round_directory / "runner-stderr.txt").write_text(
        completed.stderr, encoding="utf-8"
    )
    return completed.returncode, _challenge_path(completed.stdout)


def _round_outcome(summary: dict[str, object]) -> dict[str, object]:
    failed = int(summary["failed_match_count"])
    delta = float(summary["candidate_total_delta"])
    if failed:
        outcome = "failed"
    elif delta > 0:
        outcome = "candidate_leads"
    elif delta < 0:
        outcome = "baseline_leads"
    else:
        outcome = "tie"
    return {
        "outcome": outcome,
        "candidate_total_delta": delta,
        "candidate_mean_delta": delta / max(1, len(summary["comparisons"])),
        "failed_match_count": failed,
        "comparison_count": len(summary["comparisons"]),
    }


def main() -> int:
    parser = argparse.ArgumentParser(
        description=(
            "Run successive matched rounds for one carrot, wheat, or melon "
            "decision parameter."
        )
    )
    parser.add_argument("--agent", required=True)
    parser.add_argument("--parameter", required=True)
    parser.add_argument(
        "--candidate-value",
        action="append",
        required=True,
        help="JSON numeric value; repeat to create successive rounds",
    )
    parser.add_argument(
        "--baseline-value",
        help="JSON numeric starting value; defaults to the agent's checked-in default",
    )
    parser.add_argument("--opponent", default="pass")
    parser.add_argument("--seed", type=int, action="append", required=True)
    parser.add_argument("--steps", type=int, default=720)
    parser.add_argument("--swap-seats", action="store_true")
    parser.add_argument(
        "--auto-promote",
        action="store_true",
        help="Use a leading candidate as the next round's in-session baseline",
    )
    parser.add_argument(
        "--hypothesis",
        default="The named parameter improves the candidate under the fixed controls.",
    )
    args = parser.parse_args()

    if not 1 <= args.steps <= MAX_STEPS:
        parser.error(f"--steps must be between 1 and {MAX_STEPS}")
    if len(set(args.seed)) != len(args.seed):
        parser.error("--seed values must be unique")

    try:
        agent = _repository_file(args.agent, "agent")
        opponent = (
            args.opponent
            if args.opponent in {"pass", "random", "starter"}
            else _repository_file(args.opponent, "opponent")
        )
        adapter_name, adapter = _adapter_for(agent)
        defaults = _parameter_defaults(adapter)
        baseline_value = (
            defaults[args.parameter]
            if args.baseline_value is None
            else _parse_value(args.baseline_value)
        )
        _validate_parameter(adapter, args.parameter, baseline_value)
        candidate_values = [_parse_value(raw) for raw in args.candidate_value]
        for value in candidate_values:
            _validate_parameter(adapter, args.parameter, value)
        revision = _source_revision()
    except (ValueError, KeyError, subprocess.CalledProcessError) as error:
        parser.error(str(error))

    session_directory = RUNS_ROOT / "tuning-sessions" / _session_id()
    session_directory.mkdir(parents=True, exist_ok=False)
    baseline_parameters = _parameter_snapshot(
        defaults, args.parameter, baseline_value
    )
    session_manifest = {
        "schema": "kaggriculture-agent-tuning-studio-tuning-session/v1",
        "session_id": session_directory.name,
        "started_at": datetime.now(timezone.utc).isoformat(),
        "source_revision": revision,
        "repository": str(REPO_ROOT),
        "agent": agent,
        "adapter": adapter_name,
        "opponent": opponent,
        "parameter": args.parameter,
        "baseline_parameters": baseline_parameters,
        "candidate_values": candidate_values,
        "seeds": args.seed,
        "steps": args.steps,
        "swap_seats": args.swap_seats,
        "auto_promote": args.auto_promote,
        "hypothesis": args.hypothesis,
        "status": "running",
    }
    _write_json(session_directory / "manifest.json", session_manifest)

    current_value = baseline_value
    rounds: list[dict[str, object]] = []
    session_failed = False
    for index, candidate_value in enumerate(candidate_values, start=1):
        round_directory = session_directory / f"round-{index:03d}"
        round_directory.mkdir()
        current_parameters = _parameter_snapshot(
            defaults, args.parameter, current_value
        )
        candidate_parameters = _parameter_snapshot(
            defaults, args.parameter, candidate_value
        )
        round_manifest = {
            "schema": "kaggriculture-agent-tuning-studio-tuning-round/v1",
            "session_id": session_directory.name,
            "round": index,
            "started_at": datetime.now(timezone.utc).isoformat(),
            "source_revision": revision,
            "agent": agent,
            "adapter": adapter_name,
            "opponent": opponent,
            "parameter": args.parameter,
            "baseline_parameters": current_parameters,
            "candidate_parameters": candidate_parameters,
            "seeds": args.seed,
            "steps": args.steps,
            "swap_seats": args.swap_seats,
            "hypothesis": args.hypothesis,
        }
        _write_json(round_directory / "manifest.json", round_manifest)
        baseline_wrapper = _write_wrapper(
            round_directory / "baseline_entry.py",
            adapter,
            args.parameter,
            current_value,
        )
        candidate_wrapper = _write_wrapper(
            round_directory / "candidate_entry.py",
            adapter,
            args.parameter,
            candidate_value,
        )
        return_code, challenge_directory = _run_round(
            round_directory,
            baseline_wrapper=baseline_wrapper,
            candidate_wrapper=candidate_wrapper,
            opponent=opponent,
            seeds=args.seed,
            steps=args.steps,
            swap_seats=args.swap_seats,
        )
        if challenge_directory is None:
            outcome = {
                "outcome": "failed",
                "candidate_total_delta": None,
                "candidate_mean_delta": None,
                "failed_match_count": None,
                "comparison_count": 0,
                "runner_exit_code": return_code,
            }
            session_failed = True
        else:
            summary = json.loads(
                (challenge_directory / "summary.json").read_text(encoding="utf-8")
            )
            outcome = _round_outcome(summary)
            outcome["challenge_directory"] = str(
                challenge_directory.relative_to(REPO_ROOT)
            )
            if return_code != 0:
                session_failed = True
        promoted = bool(
            args.auto_promote
            and outcome["outcome"] == "candidate_leads"
            and outcome["failed_match_count"] == 0
        )
        if promoted:
            current_value = candidate_value
        round_result = {
            **round_manifest,
            "finished_at": datetime.now(timezone.utc).isoformat(),
            **outcome,
            "promoted_for_next_round": promoted,
            "next_baseline_value": current_value,
        }
        _write_json(round_directory / "summary.json", round_result)
        rounds.append(
            {
                "round": index,
                "candidate_value": candidate_value,
                "outcome": outcome["outcome"],
                "candidate_total_delta": outcome["candidate_total_delta"],
                "promoted_for_next_round": promoted,
                "directory": str(round_directory.relative_to(REPO_ROOT)),
            }
        )

    session_manifest.update(
        {
            "status": "failed" if session_failed else "completed",
            "finished_at": datetime.now(timezone.utc).isoformat(),
            "rounds": rounds,
            "final_in_session_baseline_parameters": _parameter_snapshot(
                defaults, args.parameter, current_value
            ),
        }
    )
    _write_json(session_directory / "manifest.json", session_manifest)
    print(session_directory.relative_to(REPO_ROOT))
    print(f"Rounds: {len(rounds)}")
    print(f"Final in-session baseline value: {current_value}")
    print(f"Status: {session_manifest['status']}")
    return 1 if session_failed else 0


if __name__ == "__main__":
    raise SystemExit(main())
