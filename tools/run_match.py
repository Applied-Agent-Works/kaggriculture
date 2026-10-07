#!/usr/bin/env python3
"""Run a Kaggriculture game locally on this computer.

Kaggle will eventually run your submitted agent against other players' agents.
This small program lets you practice that same basic loop before submitting
anything: it starts the Kaggriculture game, gives each player a turn, and
prints the result at the end.

Before running this file, activate this project's virtual environment:

    source .venv/bin/activate

Then try a short practice game between two built-in random players:

    ./tools/run_match.py

When you have written your own agent in main.py, play it against the built-in
random player for a complete 30-day season (720 turns):

    ./tools/run_match.py --agent main.py --opponent random --steps 720 --seed 42

"""

# This import makes modern type annotations work consistently.  It does not
# affect how the game plays.
from __future__ import annotations

# argparse is part of Python's standard library.  It reads options that we
# type after the command, such as --steps 720.
import argparse

# kaggle_environments is the package that contains the local game simulator.
# "make" creates one game environment for us to run.
from pathlib import Path
import tempfile
from kaggle_environments import make
from match_evidence import write_match_evidence


REPO_ROOT = Path(__file__).resolve().parents[1]
BUILTIN_AGENTS = {"pass", "random", "starter"}


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


def _agent_reference(value: str, directory: Path, role: str) -> str:
    if value in BUILTIN_AGENTS:
        return value

    module_name = value[:-3].replace("/", ".") if value.endswith(".py") else ""
    wrapper = directory / f"{role}_entry.py"
    if module_name and all(part.isidentifier() for part in module_name.split(".")):
        source = (
            "import sys\n"
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
    return str(wrapper)


def main() -> None:
    # Create the command-line interface.  The text here appears when you run
    # "./tools/run_match.py --help".
    parser = argparse.ArgumentParser(description="Run a local Kaggriculture match.")

    # --agent is player 0: normally this will be your file, main.py.  For now,
    # the default "random" is a built-in agent that makes legal random moves.
    parser.add_argument(
        "--agent",
        default="random",
        help="Your agent: a Python file such as main.py, or a built-in agent name (default: random).",
    )

    # --opponent is player 1.  It can be another Python agent file or one of
    # Kaggle's built-in agents, including "random", "pass", and "starter".
    parser.add_argument(
        "--opponent",
        default="random",
        help="Opponent Python file or built-in agent name (default: random).",
    )

    # Kaggriculture has 24 turns per in-game day and normally lasts 30 days,
    # so a real season is 24 * 30 = 720 turns.  We default to 24 because a
    # one-day test finishes quickly while you are learning.
    parser.add_argument(
        "--steps",
        type=int,
        default=24,
        help="Turns to play; use 720 for a full 30-day season (default: 24).",
    )

    # A fixed seed makes the game's random shop draws and weed spawning
    # repeatable.  It is essential when comparing one policy parameter at a
    # time.  Omitting it preserves the environment's normal random behavior.
    parser.add_argument(
        "--seed",
        type=int,
        default=None,
        help="Optional deterministic episode seed for reproducible comparisons.",
    )
    parser.add_argument(
        "--report-dir",
        type=Path,
        default=None,
        help="Optional directory for descriptive match evidence and decision traces.",
    )

    # Turn the command-line text into Python values.  For example,
    # "--steps 720" becomes args.steps == 720.
    args = parser.parse_args()

    try:
        agent = _repository_file(args.agent, "agent")
        opponent = _repository_file(args.opponent, "opponent")
    except ValueError as error:
        parser.error(str(error))

    # Build a new Kaggriculture game.  "debug=True" asks the simulator to
    # expose useful errors while developing instead of hiding them.
    configuration = {"episodeSteps": args.steps}
    if args.seed is not None:
        configuration["seed"] = args.seed
    env = make("kaggriculture", configuration=configuration, debug=True)

    # Kaggle's local runner executes file arguments as source text, without a
    # package context. Generated wrappers make repository-relative imports
    # deterministic while keeping the live agent source untouched.
    with tempfile.TemporaryDirectory(prefix="kaggriculture-match-") as directory:
        wrapper_directory = Path(directory)
        agent_reference = _agent_reference(agent, wrapper_directory, "agent")
        opponent_reference = _agent_reference(opponent, wrapper_directory, "opponent")

        # Run the game. The list is in player order: the first agent is player
        # 0 and the second is player 1.
        env.run([agent_reference, opponent_reference])

    # env.steps contains the complete turn-by-turn history.  The final entry
    # is the state after the last turn, so it holds the final result for both
    # players.
    seed_label = env.info.get("seed", "random")
    print(f"Played {args.steps} turns (seed {seed_label}): {agent} vs {opponent}")
    for player, state in enumerate(env.steps[-1]):
        # "reward" is the final score in this game (normally the coins in the
        # bank), and "status" confirms whether the agent finished normally.
        print(f"Player {player}: reward={state.reward}, status={state.status}")

    if args.report_dir is not None:
        report = write_match_evidence(
            env.steps,
            final_states=env.steps[-1],
            report_directory=args.report_dir,
            agent=args.agent,
            opponent=args.opponent,
            seed=args.seed,
            requested_steps=args.steps,
        )
        print(f"Evidence report: {args.report_dir / 'report.json'}")
        print(f"Evidence trace records: {sum(player['trace_records'] for player in report['players'].values())}")


# Python sets __name__ to "__main__" only when this file is run directly.
# This lets another Python file import this runner later without starting a
# game by accident.
if __name__ == "__main__":
    main()
