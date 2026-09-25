"""Wheat facts and adapters for the shared one-time crop loop."""

try:
    from agents.one_time_crop import OneTimeCrop, conveyor_agent, decision_agent
except ImportError:
    from ..one_time_crop import OneTimeCrop, conveyor_agent, decision_agent


WHEAT = OneTimeCrop(
    name="WHEAT",
    seed_cost=10,
    base_price=25,
    first_yield_day=2,
    bonus_window_start=2,
    max_yield_day=4,
    planned_harvest_day=4,
    planned_yield=4,
)


def wheat_conveyor_agent(obs: dict) -> dict:
    """Always run one wheat crop cycle when enough season remains."""
    return conveyor_agent(obs, WHEAT)


def wheat_decision_agent(obs: dict) -> dict:
    """Use current wheat price as a provisional sale-price estimate."""
    return decision_agent(obs, WHEAT)
