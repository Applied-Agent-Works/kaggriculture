"""Tomato facts and adapters for the shared ongoing-crop lifecycle."""

try:
    from agents.ongoing_crop import OngoingCrop, conveyor_agent, decision_agent
except ImportError:
    from ..ongoing_crop import OngoingCrop, conveyor_agent, decision_agent


TOMATO = OngoingCrop(
    name="TOMATO",
    seed_cost=50,
    base_price=60,
    production_ages=(8, 9, 10, 11),
)


def tomato_conveyor_agent(obs: dict) -> dict:
    """Always run one Tomato crop cycle when its schedule fits the season."""
    return conveyor_agent(obs, TOMATO)


def tomato_decision_agent(obs: dict) -> dict:
    """Use the ongoing-crop plant-versus-PASS decision for Tomato."""
    return decision_agent(obs, TOMATO)
