"""Kaggriculture entry point for the unconditional Strawberry baseline."""

try:
    from .shared import strawberry_conveyor_agent
except (ImportError, KeyError):
    from shared import strawberry_conveyor_agent


def agent(obs):
    """Run the Strawberry cycle without an investment decision."""
    return strawberry_conveyor_agent(obs)
