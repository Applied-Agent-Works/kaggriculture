"""Kaggriculture entry point for the always-plant carrot baseline.

This is deliberately simpler than the decision agent: it continuously plants
carrots when it can.  Comparing its results with the decision policy tells us
whether the belief network earns its complexity.
"""

try:
    # Works when imported as ``agents.carrot.conveyor`` by main.py.
    from .shared import carrot_conveyor_agent
except ImportError:
    # Works when the runner loads this file directly by path.
    from shared import carrot_conveyor_agent


def agent(obs):
    """Choose an action using the unconditional carrot conveyor policy."""
    return carrot_conveyor_agent(obs)
