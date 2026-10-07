#!/usr/bin/env python3
"""CRUD access to the local Matchmaker agent and match catalog.

Catalog operations only change metadata records. They never edit agent source,
run a match, or delete a match artifact.
"""

from __future__ import annotations

import argparse
from datetime import datetime, timezone
import json
import os
from pathlib import Path
import subprocess
import tempfile
from typing import Any


REPO_ROOT = Path(__file__).resolve().parents[4]
DEFAULT_CATALOG = Path(__file__).resolve().parents[2] / "catalog"
AGENTS_FILE = "agents.json"
MATCHES_FILE = "matches.json"


def now() -> str:
    return datetime.now(timezone.utc).isoformat()


def source_revision() -> str | None:
    result = subprocess.run(
        ["git", "rev-parse", "HEAD"],
        cwd=REPO_ROOT,
        check=False,
        capture_output=True,
        text=True,
    )
    return result.stdout.strip() if result.returncode == 0 else None


class Catalog:
    def __init__(self, directory: Path) -> None:
        self.directory = directory
        self.directory.mkdir(parents=True, exist_ok=True)
        self.agents_path = directory / AGENTS_FILE
        self.matches_path = directory / MATCHES_FILE
        self._ensure_files()

    def agents(self) -> list[dict[str, Any]]:
        return list(self._read(self.agents_path, "agents", "agent catalog"))

    def matches(self) -> list[dict[str, Any]]:
        return list(self._read(self.matches_path, "matches", "match catalog"))

    def save_agents(self, records: list[dict[str, Any]]) -> None:
        self._write(
            self.agents_path,
            {
                "schema": "kaggriculture-agent-tuning-studio-agent-catalog/v1",
                "agents": records,
            },
        )

    def save_matches(self, records: list[dict[str, Any]]) -> None:
        self._write(
            self.matches_path,
            {
                "schema": "kaggriculture-agent-tuning-studio-match-catalog/v1",
                "matches": records,
            },
        )

    def _ensure_files(self) -> None:
        if not self.agents_path.exists():
            timestamp = now()
            self.save_agents(
                [
                    {
                        "id": "main.py",
                        "label": "Carrot decision",
                        "description": "The current root submission policy for the local Kaggriculture agent.",
                        "traits": ["local", "decision policy"],
                        "available": True,
                        "kind": "repository",
                        "entryPoint": "main.py",
                        "createdAt": timestamp,
                        "updatedAt": timestamp,
                    },
                    {
                        "id": "random",
                        "label": "Random",
                        "description": "A built-in randomized opponent for smoke tests.",
                        "traits": ["built-in", "baseline"],
                        "available": True,
                        "kind": "builtin",
                        "entryPoint": "random",
                        "createdAt": timestamp,
                        "updatedAt": timestamp,
                    },
                    {
                        "id": "pass",
                        "label": "Pass",
                        "description": "A built-in no-op opponent for interface checks.",
                        "traits": ["built-in", "control"],
                        "available": True,
                        "kind": "builtin",
                        "entryPoint": "pass",
                        "createdAt": timestamp,
                        "updatedAt": timestamp,
                    },
                    {
                        "id": "starter",
                        "label": "Starter",
                        "description": "A built-in starter policy for local comparisons.",
                        "traits": ["built-in", "baseline"],
                        "available": True,
                        "kind": "builtin",
                        "entryPoint": "starter",
                        "createdAt": timestamp,
                        "updatedAt": timestamp,
                    },
                ]
            )
        if not self.matches_path.exists():
            self.save_matches([])

    @staticmethod
    def _read(path: Path, key: str, label: str) -> list[dict[str, Any]]:
        try:
            value = json.loads(path.read_text(encoding="utf-8"))
        except (OSError, json.JSONDecodeError) as error:
            raise RuntimeError(f"Could not read {label} at {path}: {error}") from error
        records = value.get(key) if isinstance(value, dict) else None
        if not isinstance(records, list) or not all(isinstance(item, dict) for item in records):
            raise RuntimeError(f"Catalog file {path} has an invalid {key} collection.")
        return records

    @staticmethod
    def _write(path: Path, value: dict[str, Any]) -> None:
        try:
            with tempfile.NamedTemporaryFile(
                "w",
                encoding="utf-8",
                dir=path.parent,
                prefix=f".{path.name}.",
                suffix=".tmp",
                delete=False,
            ) as temporary:
                json.dump(value, temporary, indent=2)
                temporary.write("\n")
                temporary_path = Path(temporary.name)
            temporary_path.replace(path)
        except OSError as error:
            raise RuntimeError(f"Could not write catalog file {path}: {error}") from error


def catalog() -> Catalog:
    configured = os.environ.get("KAGGRICULTURE_MATCHMAKER_DATA")
    return Catalog(Path(configured) if configured else DEFAULT_CATALOG)


def require_text(value: str, label: str) -> str:
    value = value.strip()
    if not value:
        raise ValueError(f"{label} is required.")
    return value


def parse_traits(value: str) -> list[str]:
    return list(dict.fromkeys(item.strip() for item in value.split(",") if item.strip()))


def agent_from_args(args: argparse.Namespace, existing: dict[str, Any] | None = None) -> dict[str, Any]:
    current = existing or {}
    record = {
        "id": require_text(args.id, "agent id"),
        "label": require_text(args.label if args.label is not None else current.get("label", ""), "agent label"),
        "description": (args.description if args.description is not None else current.get("description", "")).strip(),
        "traits": parse_traits(args.traits) if args.traits is not None else current.get("traits", []),
        "available": args.available if args.available is not None else current.get("available", True),
        "kind": args.kind if args.kind is not None else current.get("kind", "repository"),
        "entryPoint": args.entry_point if args.entry_point is not None else current.get("entryPoint"),
        "createdAt": current.get("createdAt") or now(),
        "updatedAt": now(),
    }
    if record["kind"] not in {"repository", "builtin"}:
        raise ValueError("agent kind must be repository or builtin.")
    if record["kind"] == "repository" and not record["entryPoint"]:
        raise ValueError("repository agents require --entry-point.")
    return record


def match_from_args(args: argparse.Namespace, existing: dict[str, Any] | None = None) -> dict[str, Any]:
    current = existing or {}
    record = {
        "id": require_text(args.id, "match id"),
        "agent": require_text(args.agent if args.agent is not None else current.get("agent", ""), "match agent"),
        "opponent": require_text(args.opponent if args.opponent is not None else current.get("opponent", ""), "match opponent"),
        "seed": args.seed if args.seed is not None else current.get("seed", 42),
        "steps": args.steps if args.steps is not None else current.get("steps", 24),
        "seat": args.seat if args.seat is not None else current.get("seat", 0),
        "status": args.status if args.status is not None else current.get("status", "planned"),
        "artifactPath": args.artifact_path if args.artifact_path is not None else current.get("artifactPath"),
        "sourceRevision": current.get("sourceRevision") or source_revision(),
        "configuration": current.get("configuration", {}),
        "createdAt": current.get("createdAt") or now(),
        "updatedAt": now(),
    }
    if not isinstance(record["seed"], int) or record["seed"] < 0:
        raise ValueError("seed must be a non-negative integer.")
    if not isinstance(record["steps"], int) or not 1 <= record["steps"] <= 720:
        raise ValueError("steps must be between 1 and 720.")
    if record["seat"] not in {0, 1}:
        raise ValueError("seat must be 0 or 1.")
    if record["status"] not in {"planned", "running", "succeeded", "failed", "cancelled"}:
        raise ValueError("status is invalid.")
    return record


def print_json(value: Any) -> None:
    print(json.dumps(value, indent=2, sort_keys=True))


def find(records: list[dict[str, Any]], record_id: str) -> dict[str, Any] | None:
    return next((record for record in records if record.get("id") == record_id), None)


def add_common_arguments(parser: argparse.ArgumentParser, *, required: bool) -> None:
    parser.add_argument("--id", required=required)
    parser.add_argument("--label", required=required)
    parser.add_argument("--description")
    parser.add_argument("--traits")
    parser.add_argument("--kind", choices=("repository", "builtin"))
    parser.add_argument("--entry-point", dest="entry_point")
    availability = parser.add_mutually_exclusive_group()
    availability.add_argument("--available", action="store_true", default=None)
    availability.add_argument("--unavailable", action="store_false", dest="available")


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(description="Manage Matchmaker agent and match metadata.")
    resources = parser.add_subparsers(dest="resource", required=True)

    agents = resources.add_parser("agents", help="manage agent records")
    agent_commands = agents.add_subparsers(dest="command", required=True)
    agent_commands.add_parser("list")
    agent_get = agent_commands.add_parser("get")
    agent_get.add_argument("record_id")
    agent_create = agent_commands.add_parser("create")
    add_common_arguments(agent_create, required=True)
    agent_update = agent_commands.add_parser("update")
    agent_update.add_argument("record_id")
    add_common_arguments(agent_update, required=False)
    agent_delete = agent_commands.add_parser("delete")
    agent_delete.add_argument("record_id")

    matches = resources.add_parser("matches", help="manage match records")
    match_commands = matches.add_subparsers(dest="command", required=True)
    match_commands.add_parser("list")
    match_get = match_commands.add_parser("get")
    match_get.add_argument("record_id")
    create_match = match_commands.add_parser("create")
    create_match.add_argument("--id", required=True)
    create_match.add_argument("--agent", required=True)
    create_match.add_argument("--opponent", required=True)
    create_match.add_argument("--seed", type=int)
    create_match.add_argument("--steps", type=int)
    create_match.add_argument("--seat", type=int)
    create_match.add_argument("--status", choices=("planned", "running", "succeeded", "failed", "cancelled"))
    create_match.add_argument("--artifact-path", dest="artifact_path")
    update_match = match_commands.add_parser("update")
    update_match.add_argument("record_id")
    update_match.add_argument("--agent")
    update_match.add_argument("--opponent")
    update_match.add_argument("--seed", type=int)
    update_match.add_argument("--steps", type=int)
    update_match.add_argument("--seat", type=int)
    update_match.add_argument("--status", choices=("planned", "running", "succeeded", "failed", "cancelled"))
    update_match.add_argument("--artifact-path", dest="artifact_path")
    match_delete = match_commands.add_parser("delete")
    match_delete.add_argument("record_id")
    match_run = match_commands.add_parser("run")
    match_run.add_argument("record_id")
    return parser


def run_match(record: dict[str, Any], store: Catalog) -> dict[str, Any]:
    if record.get("status") != "planned":
        raise ValueError(
            f"only planned matches can be run; {record.get('id')!r} is {record.get('status')!r}."
        )

    agents = {item.get("id"): item for item in store.agents()}
    selected = [agents.get(record.get("agent")), agents.get(record.get("opponent"))]
    if any(item is None for item in selected):
        raise ValueError("both match participants must refer to catalog agent ids.")
    if any(not item.get("available", False) for item in selected if item is not None):
        raise ValueError("both match participants must be available.")
    if any(not item.get("entryPoint") for item in selected if item is not None):
        raise ValueError("both match participants must have executable entry points.")

    running = dict(record)
    running["status"] = "running"
    running["sourceRevision"] = source_revision()
    running["errorMessage"] = None
    running["exitCode"] = None
    running["updatedAt"] = now()
    store.save_matches([running if item.get("id") == record["id"] else item for item in store.matches()])

    command = [
        str(REPO_ROOT / ".venv" / "bin" / "python"),
        str(REPO_ROOT / "labs" / "agent-tuning-studio" / "tools" / "run_isolated_match.py"),
        "--agent",
        str(selected[0]["entryPoint"]),
        "--opponent",
        str(selected[1]["entryPoint"]),
        "--steps",
        str(record["steps"]),
        "--seed",
        str(record["seed"]),
        "--seat",
        str(record["seat"]),
    ]
    try:
        completed = subprocess.run(
            command,
            cwd=REPO_ROOT,
            check=False,
            capture_output=True,
            text=True,
        )
    except OSError as error:
        running["status"] = "failed"
        running["errorMessage"] = f"could not start the local runner: {error}"
        running["exitCode"] = -1
        running["updatedAt"] = now()
        store.save_matches([running if item.get("id") == record["id"] else item for item in store.matches()])
        raise RuntimeError(running["errorMessage"]) from error

    artifact_path = next(
        (
            line.strip()
            for line in completed.stdout.splitlines()
            if line.strip().startswith("labs/agent-tuning-studio/runs/")
        ),
        None,
    )
    error_message = None
    if completed.returncode != 0:
        error_message = next(
            (line.strip() for line in completed.stderr.splitlines() if line.strip()),
            None,
        ) or f"the local runner exited with code {completed.returncode}."

    running["status"] = "succeeded" if completed.returncode == 0 else "failed"
    running["artifactPath"] = artifact_path or running.get("artifactPath")
    running["errorMessage"] = error_message
    running["exitCode"] = completed.returncode
    running["updatedAt"] = now()
    store.save_matches([running if item.get("id") == record["id"] else item for item in store.matches()])
    return running


def run(args: argparse.Namespace) -> None:
    store = catalog()
    if args.resource == "agents":
        records = store.agents()
        if args.command == "list":
            print_json(records)
        elif args.command == "get":
            record = find(records, args.record_id)
            if record is None:
                raise ValueError(f"no agent record exists for {args.record_id!r}.")
            print_json(record)
        elif args.command in {"create", "update"}:
            record_id = args.id if args.command == "create" else args.record_id
            existing = find(records, record_id)
            if args.command == "create" and existing is not None:
                raise ValueError(f"agent record {record_id!r} already exists.")
            if args.command == "update" and existing is None:
                raise ValueError(f"no agent record exists for {record_id!r}.")
            args.id = record_id
            updated = agent_from_args(args, existing)
            store.save_agents([updated if item.get("id") == record_id else item for item in records] if existing else records + [updated])
            print_json(updated)
        else:
            if find(records, args.record_id) is None:
                raise ValueError(f"no agent record exists for {args.record_id!r}.")
            if any(item.get("agent") == args.record_id or item.get("opponent") == args.record_id for item in store.matches()):
                raise ValueError(f"agent record {args.record_id!r} is referenced by a match.")
            store.save_agents([item for item in records if item.get("id") != args.record_id])
            print_json({"deleted": args.record_id})
        return

    records = store.matches()
    if args.command == "list":
        print_json(records)
    elif args.command == "get":
        record = find(records, args.record_id)
        if record is None:
            raise ValueError(f"no match record exists for {args.record_id!r}.")
        print_json(record)
    elif args.command in {"create", "update"}:
        record_id = args.id if args.command == "create" else args.record_id
        existing = find(records, record_id)
        if args.command == "create" and existing is not None:
            raise ValueError(f"match record {record_id!r} already exists.")
        if args.command == "update" and existing is None:
            raise ValueError(f"no match record exists for {record_id!r}.")
        args.id = record_id
        updated = match_from_args(args, existing)
        agent_ids = {item.get("id") for item in store.agents()}
        if updated["agent"] not in agent_ids or updated["opponent"] not in agent_ids:
            raise ValueError("both --agent and --opponent must refer to catalog agent ids.")
        store.save_matches([updated if item.get("id") == record_id else item for item in records] if existing else records + [updated])
        print_json(updated)
    elif args.command == "run":
        record = find(records, args.record_id)
        if record is None:
            raise ValueError(f"no match record exists for {args.record_id!r}.")
        print_json(run_match(record, store))
    else:
        if find(records, args.record_id) is None:
            raise ValueError(f"no match record exists for {args.record_id!r}.")
        store.save_matches([item for item in records if item.get("id") != args.record_id])
        print_json({"deleted": args.record_id})


def main() -> int:
    try:
        run(build_parser().parse_args())
    except (OSError, RuntimeError, ValueError) as error:
        print(f"error: {error}", file=os.sys.stderr)
        return 2
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
