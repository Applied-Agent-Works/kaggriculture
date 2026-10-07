"""Melon facts and adapters for the shared one-time crop loop."""

try:
    from agents.one_time_crop import OneTimeCrop, conveyor_agent, decision_agent
except ImportError:
    from ..one_time_crop import OneTimeCrop, conveyor_agent, decision_agent


MELON = OneTimeCrop(
    name="MELON",
    seed_cost=80,
    base_price=250,
    first_yield_day=10,
    bonus_window_start=6,
    max_yield_day=12,
    planned_harvest_day=10,
    planned_yield=6,
)


def melon_conveyor_agent(obs: dict) -> dict:
    """Always run one melon crop cycle when enough season remains."""
    return conveyor_agent(obs, MELON)


def melon_decision_agent(obs: dict) -> dict:
    """Use current melon price as a provisional sale-price estimate."""
    return decision_agent(obs, MELON)
