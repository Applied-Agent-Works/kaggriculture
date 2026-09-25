"""Kaggriculture entry point for the unconditional melon-cycle baseline."""

try:
    from .shared import melon_conveyor_agent
except ImportError:
    from shared import melon_conveyor_agent


def agent(obs):
    """Care for, harvest, and replant melon without a price comparison."""
    return melon_conveyor_agent(obs)
