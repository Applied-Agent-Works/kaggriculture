"""Kaggriculture entry point for the provisional melon planting decision."""

try:
    from .shared import melon_decision_agent
except ImportError:
    from shared import melon_decision_agent


def agent(obs):
    """Plant melon when its point-estimated value beats PASS."""
    return melon_decision_agent(obs)
