"""Kaggriculture entry point for the provisional wheat planting decision."""

try:
    from .shared import wheat_decision_agent
except ImportError:
    from shared import wheat_decision_agent


def agent(obs):
    """Plant wheat when its point-estimated value beats PASS."""
    return wheat_decision_agent(obs)
