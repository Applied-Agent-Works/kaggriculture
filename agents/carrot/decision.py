"""Kaggriculture entry point for the probabilistic carrot decision policy.

Run this file against another agent with the local runner, or submit it as an
agent module.  The implementation lives in ``carrot_agent.py`` so the two
carrot policies use the same documented game facts and helper functions.
"""

try:
    # Works when imported as ``agents.carrot.decision`` by main.py.
    from .shared import carrot_decision_agent
except ImportError:
    # Works when the runner loads this file directly by path.
    from shared import carrot_decision_agent


def agent(obs):
    """Choose an action using the belief-based plant-versus-pass decision."""
    return carrot_decision_agent(obs)
