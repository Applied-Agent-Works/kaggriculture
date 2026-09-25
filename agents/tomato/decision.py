"""Kaggriculture entry point for the Tomato decision policy."""

try:
    from .shared import tomato_decision_agent
except (ImportError, KeyError):
    from shared import tomato_decision_agent


def agent(obs):
    """Choose Tomato planting from the explicit expected-utility gate."""
    return tomato_decision_agent(obs)
