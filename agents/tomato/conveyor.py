"""Kaggriculture entry point for the unconditional Tomato baseline."""

try:
    from .shared import tomato_conveyor_agent
except (ImportError, KeyError):
    from shared import tomato_conveyor_agent


def agent(obs):
    """Run the Tomato cycle without a probabilistic investment decision."""
    return tomato_conveyor_agent(obs)
