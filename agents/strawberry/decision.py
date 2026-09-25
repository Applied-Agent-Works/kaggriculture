"""Kaggriculture entry point for the Strawberry decision policy."""

try:
    from agents.strawberry.shared import strawberry_decision_agent
except (ImportError, KeyError):
    from .shared import strawberry_decision_agent


def agent(obs):
    """Choose Strawberry planting from the expected-utility gate."""
    return strawberry_decision_agent(obs)
