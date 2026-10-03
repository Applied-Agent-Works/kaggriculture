"""Tomato-specific entry-point and deterministic-run regressions."""

from pathlib import Path
import subprocess
import sys

from agents.tomato import conveyor, decision


def test_tomato_entry_points_expose_agent_functions():
    """Both crop-owned files are importable without ambient module state."""
    assert callable(conveyor.agent)
    assert callable(decision.agent)


def test_tomato_file_loads_through_local_runner_deterministically():
    """The local runner can load the file path and reproduce a fixed seed."""
    root = Path(__file__).resolve().parents[1]
    command = [
        sys.executable,
        "tools/run_match.py",
        "--agent",
        "agents/tomato/conveyor.py",
        "--opponent",
        "pass",
        "--steps",
        "240",
        "--seed",
        "42",
    ]

    first = subprocess.run(
        command,
        cwd=root,
        check=True,
        capture_output=True,
        text=True,
    )
    second = subprocess.run(
        command,
        cwd=root,
        check=True,
        capture_output=True,
        text=True,
    )

    assert first.stdout == second.stdout
    assert "agents/tomato/conveyor.py vs pass" in first.stdout
    assert "status=DONE" in first.stdout
