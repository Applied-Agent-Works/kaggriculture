"""Small reusable loop for the one-time wheat and melon teaching agents.

This module manages at most one active crop of its configured type. Keeping a
single crop in flight makes its daily watering and harvest schedule visible
while we learn each crop's decision network. It is not a general farm planner.
"""

from dataclasses import dataclass
from typing import Dict, Optional, Tuple


DEFAULT_SEASON_DAYS = 30


@dataclass(frozen=True)
class OneTimeCrop:
    """Fixed game facts that differ between wheat and melon."""

    name: str
    seed_cost: int
    base_price: int
    first_yield_day: int
    bonus_window_start: int
    max_yield_day: int
    planned_harvest_day: int
    planned_yield: int


@dataclass(frozen=True)
class PointPriceParameters:
    """Provisional deterministic price assumption for the decision example.

    A multiplier of 1.0 means: use today's observed sale quote as the point
    estimate for the sale quote at harvest. This is an explicit placeholder,
    not a calibrated forecast or a probability distribution.
    """

    future_price_multiplier: float = 1.0
    pass_utility: float = 0.0
    season_days: int = DEFAULT_SEASON_DAYS


DEFAULT_POINT_PRICE_PARAMETERS = PointPriceParameters()


def _my_farm(obs: dict) -> dict:
    """Return the public farm belonging to the observing player."""
    return obs["farms"][obs["player"]]


def _positions_of_crop(farm: dict, crop_name: str):
    """Yield crop locations in stable row-major order."""
    for y, row in enumerate(farm["tiles"]):
        for x, tile in enumerate(row):
            if (
                isinstance(tile, dict)
                and tile.get("kind") == "PLANT"
                and tile.get("crop") == crop_name
            ):
                yield (x, y), tile


def _first_empty_position(farm: dict) -> Optional[Tuple[int, int]]:
    """Find the first empty unlocked tile; locked cells are marked separately."""
    for y, row in enumerate(farm["tiles"]):
        for x, tile in enumerate(row):
            if tile is None:
                return (x, y)
    return None


def _move_toward(current: Tuple[int, int], target: Tuple[int, int]):
    """Take one predictable cardinal step toward a target tile."""
    x, y = current
    target_x, target_y = target
    if x < target_x:
        return ["EAST"]
    if x > target_x:
        return ["WEST"]
    if y < target_y:
        return ["SOUTH"]
    if y > target_y:
        return ["NORTH"]
    return ["PASS"]


def can_reach_planned_harvest(
    obs: dict,
    crop: OneTimeCrop,
    parameters: PointPriceParameters = DEFAULT_POINT_PRICE_PARAMETERS,
) -> bool:
    """Check that the default season includes the planned harvest day."""
    return obs["day"] + crop.planned_harvest_day < parameters.season_days


def expected_crop_value(
    obs: dict,
    crop: OneTimeCrop,
    parameters: PointPriceParameters = DEFAULT_POINT_PRICE_PARAMETERS,
) -> float:
    """Return the simple point-estimate value of planting one crop.

    The observation supplies today's sale quote. We temporarily treat that
    quote as the expected quote at harvest, multiply by the planned yield, and
    subtract the seed's replacement value (its opportunity cost).
    """
    current_quote = obs["market"]["prices"].get(crop.name, crop.base_price)
    estimated_sale_quote = current_quote * parameters.future_price_multiplier
    return crop.planned_yield * estimated_sale_quote - crop.seed_cost


def should_plant_crop(
    obs: dict,
    crop: OneTimeCrop,
    parameters: PointPriceParameters = DEFAULT_POINT_PRICE_PARAMETERS,
) -> bool:
    """Choose planting only when estimated value beats the PASS baseline."""
    farm = _my_farm(obs)
    seeds = obs["private"]["seeds"].get(crop.name, 0)
    can_acquire_seed = seeds > 0 or farm["money"] >= crop.seed_cost
    has_empty_tile = _first_empty_position(farm) is not None
    return (
        can_acquire_seed
        and has_empty_tile
        and can_reach_planned_harvest(obs, crop, parameters)
        and expected_crop_value(obs, crop, parameters) > parameters.pass_utility
    )


def _farm_action(obs: dict, crop: OneTimeCrop, invest: bool):
    """Care for one existing crop, or move to plant one new crop.

    Daily watering has priority over speculative planting. The agent only
    creates a new crop after all crops of this type have been harvested.
    """
    farm = _my_farm(obs)
    current = tuple(farm["farmer"])
    active = list(_positions_of_crop(farm, crop.name))

    if active:
        # This teaching policy creates one crop at a time. If a farm already
        # has several, it chooses the first row-major crop and may not care for
        # the others; multi-tile scheduling is a later network.
        target, tile = active[0]
        age = obs["day"] - tile["planted_day"]
        if current != target:
            return _move_toward(current, target)
        if not tile["watered_today"]:
            return ["WATER"]
        if age >= crop.planned_harvest_day and tile.get("yield_units", 0) > 0:
            return ["HARVEST"]
        return ["PASS"]

    if not invest or not can_reach_planned_harvest(obs, crop):
        return ["PASS"]

    target = _first_empty_position(farm)
    if target is None:
        return ["PASS"]
    if current != target:
        return _move_toward(current, target)
    if obs["private"]["seeds"].get(crop.name, 0) > 0:
        return ["PLANT", crop.name]
    # A seed purchase is issued as a market order. Plant on a later turn after
    # the market has delivered it, then water on the next turn that same day.
    return ["PASS"]


def _market_orders(obs: dict, crop: OneTimeCrop, invest: bool):
    """Sell stored crop and, when investing, buy at most one seed."""
    farm = _my_farm(obs)
    private = obs["private"]
    orders = []

    held_crop = private["shed"].get(crop.name, 0)
    if held_crop > 0:
        orders.append(["SELL", crop.name, held_crop])

    has_active_crop = next(_positions_of_crop(farm, crop.name), None) is not None
    has_seed = private["seeds"].get(crop.name, 0) > 0
    if (
        invest
        and not has_active_crop
        and not has_seed
        and _first_empty_position(farm) is not None
        and farm["money"] >= crop.seed_cost
        and can_reach_planned_harvest(obs, crop)
    ):
        orders.append(["BUY_SEED", crop.name, 1])

    return orders


def conveyor_agent(obs: dict, crop: OneTimeCrop) -> dict:
    """Always run this crop's one-tile cycle when its harvest fits the season."""
    invest = can_reach_planned_harvest(obs, crop)
    return {
        "farmer": _farm_action(obs, crop, invest),
        "hands": [],
        "market": _market_orders(obs, crop, invest),
    }


def decision_agent(
    obs: dict,
    crop: OneTimeCrop,
    parameters: PointPriceParameters = DEFAULT_POINT_PRICE_PARAMETERS,
) -> dict:
    """Use the current-price point estimate for this plant-versus-PASS choice."""
    invest = should_plant_crop(obs, crop, parameters)
    return {
        "farmer": _farm_action(obs, crop, invest),
        "hands": [],
        "market": _market_orders(obs, crop, invest),
    }
