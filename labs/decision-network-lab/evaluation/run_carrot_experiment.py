#!/usr/bin/env python3
"""Generate a fixed-seed carrot evidence package for Phase 2A.

This is an evidence producer, not a live policy. It compares the current
carrot decision policy with the simpler conveyor baseline against a fixed
``pass`` opponent and swaps player seats for every seed.
"""

from __future__ import annotations

import argparse
import gzip
import hashlib
import json
import subprocess
import sys
from dataclasses import dataclass, field
from datetime import datetime, timezone
from pathlib import Path
from statistics import mean
from typing import Callable


LAB_ROOT = Path(__file__).resolve().parents[1]
PROJECT_ROOT = LAB_ROOT.parents[1]
DEFAULT_SEEDS = (42, 43, 44)
DEFAULT_STEPS = 720

Agent = Callable[[dict], dict]


def parse_seeds(value: str) -> list[int]:
    """Parse values such as ``42,43`` and ``42-44``."""
    seeds: list[int] = []
    for part in value.split(","):
        part = part.strip()
        if not part:
            continue
        if "-" in part:
            start, end = (int(item) for item in part.split("-", maxsplit=1))
            if end < start:
                raise argparse.ArgumentTypeError(f"seed range must ascend: {part}")
            seeds.extend(range(start, end + 1))
        else:
            seeds.append(int(part))
    if not seeds:
        raise argparse.ArgumentTypeError("provide at least one seed")
    return seeds


def source_revision() -> str:
    """Return the current revision when this checkout is a Git worktree."""
    try:
        completed = subprocess.run(
            ["git", "rev-parse", "HEAD"],
            cwd=PROJECT_ROOT,
            check=True,
            capture_output=True,
            text=True,
        )
    except (OSError, subprocess.CalledProcessError):
        return "unknown"
    return completed.stdout.strip()


def source_fingerprint() -> str:
    """Hash the policy files used by this experiment."""
    digest = hashlib.sha256()
    for relative_path in (
        "agents/carrot/decision.py",
        "agents/carrot/conveyor.py",
        "agents/carrot/shared.py",
    ):
        digest.update(relative_path.encode("utf-8"))
        digest.update((PROJECT_ROOT / relative_path).read_bytes())
    return digest.hexdigest()


def clone_json(value: object) -> object:
    """Copy simulator dictionaries without importing a third-party helper."""
    return json.loads(json.dumps(value))


@dataclass
class TraceAgent:
    """Wrap a policy and preserve small, pre-action decision evidence."""

    name: str
    function: Agent
    record_belief: bool
    calls: int = 0
    trace: list[dict] = field(default_factory=list)

    def __call__(self, observation: dict, *_: object) -> dict:
        action = self.function(observation)

        trace_entry = {
            "step": observation.get("step"),
            "day": observation.get("day"),
            "hour": observation.get("hour"),
            "decision": None,
            "evidence": None,
            "belief": None,
            "utility": None,
            "action": clone_json(action),
        }

        if self.record_belief:
            from agents.carrot.shared import explain_carrot_decision

            explanation = explain_carrot_decision(observation)
            trace_entry.update(
                {
                    "decision": explanation["recommendation"],
                    "evidence": {
                        "market_prices": clone_json(observation.get("market", {}).get("prices", {})),
                        "market_inventory": clone_json(observation.get("market", {}).get("inventory", {})),
                        "visible_opponent_ripe_carrots": explanation["visible_opponent_ripe_carrots"],
                        "known_carrot_demand_per_day": explanation["known_carrot_demand_per_day"],
                    },
                    "belief": {
                        "high_opponent_supply_probability": explanation["high_opponent_supply_probability"],
                        "expected_new_shop_carrot_demand_per_day": explanation["expected_new_shop_carrot_demand_per_day"],
                        "price_distribution": explanation["price_distribution"],
                        "expected_sale_price": explanation["expected_sale_price"],
                    },
                    "utility": {
                        "plant_carrot": explanation["plant_utility"],
                        "pass": explanation["pass_utility"],
                    },
                }
            )

        self.trace.append(trace_entry)

        self.calls += 1
        return action


def action_counts(trace: list[dict]) -> dict[str, int]:
    """Summarize the candidate's farmer actions without interpreting outcomes."""
    counts: dict[str, int] = {}
    for turn in trace:
        farmer_action = turn.get("action", {}).get("farmer", [])
        action_name = farmer_action[0] if farmer_action else "UNKNOWN"
        counts[action_name] = counts.get(action_name, 0) + 1
    return counts


def run_match(
    *,
    policy_name: str,
    policy: Agent,
    seed: int,
    decision_player: int,
    steps: int,
    record_belief: bool,
) -> tuple[dict, list[dict]]:
    """Run one policy against the fixed pass opponent."""
    try:
        from kaggle_environments import make
    except ModuleNotFoundError as error:
        raise SystemExit(
            "kaggle_environments is required in the active Python environment. "
            "Use the project's existing simulator environment; this harness does not install it."
        ) from error

    traced_policy = TraceAgent(policy_name, policy, record_belief)
    players = [traced_policy, "pass"] if decision_player == 0 else ["pass", traced_policy]
    environment = make(
        "kaggriculture",
        configuration={"episodeSteps": steps, "seed": seed},
        debug=True,
    )
    environment.run(players)

    final_states = environment.steps[-1]
    opponent_player = 1 - decision_player
    policy_state = final_states[decision_player]
    opponent_state = final_states[opponent_player]
    return (
        {
            "policy": policy_name,
            "seed": seed,
            "decision_player": decision_player,
            "opponent": "pass",
            "steps": steps,
            "policy_reward": policy_state.reward,
            "opponent_reward": opponent_state.reward,
            "policy_status": policy_state.status,
            "opponent_status": opponent_state.status,
            "decision_calls": traced_policy.calls,
            "farmer_action_counts": action_counts(traced_policy.trace),
        },
        traced_policy.trace,
    )


def write_json(path: Path, value: object) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(value, indent=2) + "\n", encoding="utf-8")


def comparison_summary(summaries: list[dict]) -> dict:
    """Create descriptive paired deltas without declaring a winner."""
    by_key: dict[tuple[int, int], dict[str, dict]] = {}
    for summary in summaries:
        key = (summary["seed"], summary["decision_player"])
        by_key.setdefault(key, {})[summary["policy"]] = summary

    pairs = []
    for (seed, decision_player), policies in sorted(by_key.items()):
        baseline = policies.get("baseline-conveyor")
        candidate = policies.get("candidate-decision")
        if baseline is None or candidate is None:
            continue
        pairs.append(
            {
                "seed": seed,
                "decision_player": decision_player,
                "baseline_reward": baseline["policy_reward"],
                "candidate_reward": candidate["policy_reward"],
                "candidate_minus_baseline": candidate["policy_reward"] - baseline["policy_reward"],
            }
        )

    deltas = [pair["candidate_minus_baseline"] for pair in pairs]
    return {
        "comparison": "candidate-decision minus baseline-conveyor",
        "paired_runs": len(pairs),
        "mean_delta": mean(deltas) if deltas else None,
        "minimum_delta": min(deltas) if deltas else None,
        "maximum_delta": max(deltas) if deltas else None,
        "descriptive_only": True,
        "note": "This small fixed suite is evidence for review, not a claim of policy improvement.",
        "pairs": pairs,
    }


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--seeds", type=parse_seeds, default=list(DEFAULT_SEEDS))
    parser.add_argument("--steps", type=int, default=DEFAULT_STEPS)
    parser.add_argument(
        "--output",
        type=Path,
        default=LAB_ROOT / "evaluation" / "results" / "carrot-decision-vs-conveyor-v1",
    )
    args = parser.parse_args()

    sys.path.insert(0, str(PROJECT_ROOT))
    from agents.carrot.conveyor import agent as conveyor_agent
    from agents.carrot.decision import agent as decision_agent

    args.output.mkdir(parents=True, exist_ok=True)
    trace_directory = args.output / "decision-traces"
    policies = {
        "baseline-conveyor": (conveyor_agent, False),
        "candidate-decision": (decision_agent, True),
    }
    summaries: list[dict] = []
    trace_files: list[str] = []

    for policy_name, (policy, record_belief) in policies.items():
        for seed in args.seeds:
            for decision_player in (0, 1):
                summary, trace = run_match(
                    policy_name=policy_name,
                    policy=policy,
                    seed=seed,
                    decision_player=decision_player,
                    steps=args.steps,
                    record_belief=record_belief,
                )
                summaries.append(summary)

                trace_path = trace_directory / (
                    f"{policy_name}-seed-{seed}-seat-{decision_player}.json.gz"
                )
                trace_path.parent.mkdir(parents=True, exist_ok=True)
                with gzip.open(trace_path, "wt", encoding="utf-8") as stream:
                    json.dump(trace, stream, indent=2)
                trace_files.append(str(trace_path.relative_to(args.output)))

    summaries_path = args.output / "run-summaries.jsonl"
    with summaries_path.open("w", encoding="utf-8") as stream:
        for summary in summaries:
            stream.write(json.dumps(summary) + "\n")

    write_json(args.output / "comparison-summary.json", comparison_summary(summaries))

    manifest = {
        "schema_version": "1.0",
        "experiment_id": "carrot-decision-vs-conveyor-v1",
        "purpose": "Compare the current carrot decision policy with the conveyor baseline under fixed local controls.",
        "created_at_utc": datetime.now(timezone.utc).isoformat(),
        "source": {
            "git_revision": source_revision(),
            "policy_fingerprint_sha256": source_fingerprint(),
            "policy_files": [
                "agents/carrot/decision.py",
                "agents/carrot/conveyor.py",
                "agents/carrot/shared.py",
            ],
        },
        "controls": {
            "seeds": args.seeds,
            "decision_players": [0, 1],
            "opponent": "pass",
            "episode_steps": args.steps,
            "game": "kaggriculture",
        },
        "policies": {
            "baseline": "baseline-conveyor",
            "candidate": "candidate-decision",
        },
        "artifacts": {
            "run_summaries": "run-summaries.jsonl",
            "comparison_summary": "comparison-summary.json",
            "decision_traces": trace_files,
        },
    }
    write_json(args.output / "manifest.json", manifest)
    print(f"Wrote experiment package to {args.output}")
    print(f"Recorded {len(summaries)} run summaries and {len(trace_files)} decision traces.")


if __name__ == "__main__":
    main()
