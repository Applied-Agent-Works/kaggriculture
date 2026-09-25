"""Strawberry facts and adapters for the shared ongoing-crop lifecycle."""

try:
    from agents.ongoing_crop import (
        DEFAULT_ONGOING_PARAMETERS,
        OngoingCrop,
        OngoingCropParameters,
        conveyor_agent,
        decision_agent,
    )
except ImportError:
    from ..ongoing_crop import (
        DEFAULT_ONGOING_PARAMETERS,
        OngoingCrop,
        OngoingCropParameters,
        conveyor_agent,
        decision_agent,
    )


STRAWBERRY = OngoingCrop(
    name="STRAWBERRY",
    seed_cost=100,
    base_price=120,
    production_ages=(10, 12, 14, 16),
)


def strawberry_conveyor_agent(
    obs: dict,
    parameters: OngoingCropParameters = DEFAULT_ONGOING_PARAMETERS,
) -> dict:
    """Always run one Strawberry crop cycle when its schedule fits the season."""
    return conveyor_agent(obs, STRAWBERRY, parameters)


def strawberry_decision_agent(
    obs: dict,
    parameters: OngoingCropParameters = DEFAULT_ONGOING_PARAMETERS,
) -> dict:
    """Use the ongoing-crop plant-versus-PASS decision for Strawberry."""
    return decision_agent(obs, STRAWBERRY, parameters)
