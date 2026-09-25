"""Strawberry facts and adapters for the shared ongoing-crop lifecycle."""

try:
    from agents.ongoing_crop import OngoingCrop, conveyor_agent, decision_agent
except ImportError:
    from ..ongoing_crop import OngoingCrop, conveyor_agent, decision_agent


STRAWBERRY = OngoingCrop(
    name="STRAWBERRY",
    seed_cost=100,
    base_price=120,
    production_ages=(10, 12, 14, 16),
)


def strawberry_conveyor_agent(obs: dict) -> dict:
    """Always run one Strawberry crop cycle when its schedule fits the season."""
    return conveyor_agent(obs, STRAWBERRY)


def strawberry_decision_agent(obs: dict) -> dict:
    """Use the ongoing-crop plant-versus-PASS decision for Strawberry."""
    return decision_agent(obs, STRAWBERRY)
