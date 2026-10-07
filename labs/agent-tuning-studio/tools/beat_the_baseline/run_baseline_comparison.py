#!/usr/bin/env python3
"""Run a matched baseline-versus-candidate Kaggriculture comparison."""

from __future__ import annotations

import argparse
from datetime import datetime, timezone
import json
from pathlib import Path
import re
import subprocess
import sys
import uuid


REPO_ROOT = Path(__file__).resolve().parents[4]
RUNS_ROOT = Path(__file__).resolve().parents[2] / "runs"
BUILTIN_AGENTS = {"pass", "random", "starter"}
MAX_STEPS = 720
PLAYER_RESULT = re.compile(
    r"Player (?P<player>[01]): reward=(?P<reward>-?\d+(?:\.\d+)?), "
    r"status=(?P<status>\S+)"
)


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


def _challenge_id() -> str:
    timestamp = datetime.now(timezone.utc).strftime("%Y%m%dT%H%M%SZ")
    return f"{timestamp}-{uuid.uuid4().hex[:12]}"


def _agent_reference(
    value: str,
    *,
    match_directory: Path,
    role: str,
) -> str:
    if value in BUILTIN_AGENTS:
        return value

    if not value.endswith(".py"):
        raise ValueError(f"{role} must identify a Python file: {value}")
    module_name = value[:-3].replace("/", ".")

    wrapper = match_directory / f"{role}_entry.py"
    if all(part.isidentifier() for part in module_name.split(".")):
        source = (
            f"import sys\n"
            f"sys.path.insert(0, {str(REPO_ROOT)!r})\n"
            f"from {module_name} import agent\n"
        )
    else:
        source = (
            "import importlib.util\n"
            "import sys\n"
            f"sys.path.insert(0, {str(REPO_ROOT)!r})\n"
            f"_spec = importlib.util.spec_from_file_location({role!r}, "
            f"{str((REPO_ROOT / value).resolve())!r})\n"
            "if _spec is None or _spec.loader is None:\n"
            f"    raise ImportError('cannot load {role} from generated wrapper')\n"
            f"_module = importlib.util.module_from_spec(_spec)\n"
            "sys.modules[_spec.name] = _module\n"
            "_spec.loader.exec_module(_module)\n"
            "agent = _module.agent\n"
        )
    wrapper.write_text(source, encoding="utf-8")
    return wrapper.relative_to(REPO_ROOT).as_posix()


def _match_result(stdout: str) -> dict[str, object]:
    players = {
        int(match.group("player")): {
            "reward": float(match.group("reward")),
            "status": match.group("status"),
        }
        for match in PLAYER_RESULT.finditer(stdout)
    }
    if set(players) != {0, 1}:
        raise ValueError("match output did not contain both player results")
    return {"players": players}


def _run_match(
    challenge_directory: Path,
    *,
    label: str,
    agent: str,
    opponent: str,
    seed: int,
    steps: int,
) -> dict[str, object]:
    match_directory = challenge_directory / label
    match_directory.mkdir()
    agent_reference = _agent_reference(
        agent,
        match_directory=match_directory,
        role="agent",
    )
    opponent_reference = _agent_reference(
        opponent,
        match_directory=match_directory,
        role="opponent",
    )
    command = [
        sys.executable,
        "tools/run_match.py",
        "--agent",
        agent_reference,
        "--opponent",
        opponent_reference,
        "--steps",
        str(steps),
        "--seed",
        str(seed),
        "--report-dir",
        str(match_directory / "evidence"),
    ]
    completed = subprocess.run(
        command,
        cwd=REPO_ROOT,
        check=False,
        capture_output=True,
        text=True,
    )
    (match_directory / "stdout.txt").write_text(completed.stdout, encoding="utf-8")
    (match_directory / "stderr.txt").write_text(completed.stderr, encoding="utf-8")
    result: dict[str, object] = {
        "label": label,
        "agent": agent,
        "opponent": opponent,
        "seed": seed,
        "steps": steps,
        "command": command,
        "exit_code": completed.returncode,
        "succeeded": completed.returncode == 0,
    }
    if completed.returncode == 0:
        try:
            result.update(_match_result(completed.stdout))
            evidence_path = match_directory / "evidence" / "report.json"
            if not evidence_path.is_file():
                raise ValueError("match did not produce an evidence report")
            result["evidence"] = json.loads(evidence_path.read_text(encoding="utf-8"))
        except ValueError as error:
            result["succeeded"] = False
            result["error"] = str(error)
    _write_json(match_directory / "status.json", result)
    return result


def _comparison_row(
    baseline_match: dict[str, object],
    candidate_match: dict[str, object],
    *,
    baseline_player: int,
    candidate_player: int,
) -> dict[str, object]:
    baseline_players = baseline_match["players"]
    candidate_players = candidate_match["players"]
    assert isinstance(baseline_players, dict)
    assert isinstance(candidate_players, dict)
    baseline = baseline_players[baseline_player]
    candidate = candidate_players[candidate_player]
    assert isinstance(baseline, dict)
    assert isinstance(candidate, dict)
    baseline_reward = float(baseline["reward"])
    candidate_reward = float(candidate["reward"])
    return {
        "label": f"{baseline_match['label']} vs {candidate_match['label']}",
        "seed": baseline_match["seed"],
        "baseline_player": baseline_player,
        "candidate_player": candidate_player,
        "baseline_reward": baseline_reward,
        "candidate_reward": candidate_reward,
        "candidate_delta": candidate_reward - baseline_reward,
        "baseline_metrics": baseline_match.get("evidence", {}).get("players", {}).get(
            str(baseline_player), {}
        ),
        "candidate_metrics": candidate_match.get("evidence", {}).get("players", {}).get(
            str(candidate_player), {}
        ),
    }


def main() -> int:
    parser = argparse.ArgumentParser(
        description="Run a deterministic matched baseline-versus-candidate comparison."
    )
    parser.add_argument("--baseline", required=True)
    parser.add_argument("--candidate", required=True)
    parser.add_argument("--opponent", default="pass")
    parser.add_argument("--seed", type=int, action="append", required=True)
    parser.add_argument("--steps", type=int, default=720)
    parser.add_argument("--swap-seats", action="store_true")
    args = parser.parse_args()

    if not 1 <= args.steps <= MAX_STEPS:
        parser.error(f"--steps must be between 1 and {MAX_STEPS}")
    if len(set(args.seed)) != len(args.seed):
        parser.error("--seed values must be unique")

    try:
        baseline = _repository_file(args.baseline, "baseline")
        candidate = _repository_file(args.candidate, "candidate")
        opponent = _repository_file(args.opponent, "opponent")
        revision = _source_revision()
    except (ValueError, subprocess.CalledProcessError) as error:
        parser.error(str(error))

    challenge_directory = RUNS_ROOT / "baseline-challenges" / _challenge_id()
    challenge_directory.mkdir(parents=True, exist_ok=False)
    manifest = {
        "schema": "kaggriculture-agent-tuning-studio-baseline-challenge/v1",
        "challenge_id": challenge_directory.name,
        "started_at": datetime.now(timezone.utc).isoformat(),
        "source_revision": revision,
        "repository": str(REPO_ROOT),
        "baseline": baseline,
        "candidate": candidate,
        "opponent": opponent,
        "seeds": args.seed,
        "steps": args.steps,
        "swap_seats": args.swap_seats,
    }
    _write_json(challenge_directory / "manifest.json", manifest)

    matches: list[dict[str, object]] = []
    comparisons: list[dict[str, object]] = []
    for seed in args.seed:
        baseline_match = _run_match(
            challenge_directory,
            label=f"seed-{seed}-baseline-player-0",
            agent=baseline,
            opponent=opponent,
            seed=seed,
            steps=args.steps,
        )
        candidate_match = _run_match(
            challenge_directory,
            label=f"seed-{seed}-candidate-player-0",
            agent=candidate,
            opponent=opponent,
            seed=seed,
            steps=args.steps,
        )
        matches.extend([baseline_match, candidate_match])
        if baseline_match["succeeded"] and candidate_match["succeeded"]:
            comparisons.extend(
                [
                    _comparison_row(
                        baseline_match,
                        candidate_match,
                        baseline_player=0,
                        candidate_player=0,
                    ),
                ]
            )
        if args.swap_seats:
            baseline_swapped = _run_match(
                challenge_directory,
                label=f"seed-{seed}-baseline-player-1",
                agent=opponent,
                opponent=baseline,
                seed=seed,
                steps=args.steps,
            )
            candidate_swapped = _run_match(
                challenge_directory,
                label=f"seed-{seed}-candidate-player-1",
                agent=opponent,
                opponent=candidate,
                seed=seed,
                steps=args.steps,
            )
            matches.extend([baseline_swapped, candidate_swapped])
            if baseline_swapped["succeeded"] and candidate_swapped["succeeded"]:
                comparisons.extend(
                    [
                        _comparison_row(
                            baseline_swapped,
                            candidate_swapped,
                            baseline_player=1,
                            candidate_player=1,
                        ),
                    ]
                )

    failed_matches = [match for match in matches if not match["succeeded"]]
    total_delta = sum(float(row["candidate_delta"]) for row in comparisons)
    if failed_matches:
        outcome = "failed"
    elif total_delta > 0:
        outcome = "candidate_leads"
    elif total_delta < 0:
        outcome = "baseline_leads"
    else:
        outcome = "tie"

    summary = {
        "challenge_id": challenge_directory.name,
        "outcome": outcome,
        "matches": matches,
        "comparisons": comparisons,
        "baseline_total_reward": sum(
            float(row["baseline_reward"]) for row in comparisons
        ),
        "candidate_total_reward": sum(
            float(row["candidate_reward"]) for row in comparisons
        ),
        "candidate_total_delta": total_delta,
        "failed_match_count": len(failed_matches),
        "finished_at": datetime.now(timezone.utc).isoformat(),
    }
    _write_json(challenge_directory / "summary.json", summary)
    print(challenge_directory.relative_to(REPO_ROOT))
    print(f"Outcome: {outcome}")
    return 1 if failed_matches else 0


if __name__ == "__main__":
    raise SystemExit(main())
