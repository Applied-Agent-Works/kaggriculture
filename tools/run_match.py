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
from kaggle_environments import make


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

    # Turn the command-line text into Python values.  For example,
    # "--steps 720" becomes args.steps == 720.
    args = parser.parse_args()

    # Build a new Kaggriculture game.  "debug=True" asks the simulator to
    # expose useful errors while developing instead of hiding them.
    configuration = {"episodeSteps": args.steps}
    if args.seed is not None:
        configuration["seed"] = args.seed
    env = make("kaggriculture", configuration=configuration, debug=True)

    # Run the game.  The list is in player order: the first agent is player 0
    # and the second is player 1.  On each turn, the simulator calls each
    # agent's agent(observation) function and applies the returned actions.
    env.run([args.agent, args.opponent])

    # env.steps contains the complete turn-by-turn history.  The final entry
    # is the state after the last turn, so it holds the final result for both
    # players.
    seed_label = env.info.get("seed", "random")
    print(f"Played {args.steps} turns (seed {seed_label}): {args.agent} vs {args.opponent}")
    for player, state in enumerate(env.steps[-1]):
        # "reward" is the final score in this game (normally the coins in the
        # bank), and "status" confirms whether the agent finished normally.
        print(f"Player {player}: reward={state.reward}, status={state.status}")


# Python sets __name__ to "__main__" only when this file is run directly.
# This lets another Python file import this runner later without starting a
# game by accident.
if __name__ == "__main__":
    main()
