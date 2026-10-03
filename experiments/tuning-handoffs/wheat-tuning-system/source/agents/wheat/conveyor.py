"""Kaggriculture entry point for the unconditional wheat-cycle baseline."""

try:
    from .shared import wheat_conveyor_agent
except ImportError:
    from shared import wheat_conveyor_agent


def agent(obs):
    """Care for, harvest, and replant wheat without a price comparison."""
    return wheat_conveyor_agent(obs)
