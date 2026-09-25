"""Shared lifecycle and decision helpers for ongoing crops.

This is deliberately smaller than a general farm planner.  It supports the
one-tile teaching graphs for Tomato and Strawberry: one active crop, one
farmer, daily watering, scheduled production, harvest without removing the
plant, and a plant-versus-PASS investment decision.
"""

from dataclasses import dataclass
from typing import Optional, Tuple


DEFAULT_SEASON_DAYS = 30


@dataclass(frozen=True)
class OngoingCrop:
    """Fixed game facts that define an ongoing crop's lifecycle."""

    name: str
    seed_cost: int
    base_price: int
    production_ages: Tuple[int, ...]
    base_yield_per_production: int = 1
    fertilized_yield_per_production: int = 2

    @property
    def first_production_age(self) -> int:
        return self.production_ages[0]

    @property
    def final_production_age(self) -> int:
        return self.production_ages[-1]


@dataclass(frozen=True)
class OngoingCropParameters:
    """Beliefs and preferences; these are not game mechanics.

    The initial graph uses today's quote as a transparent point estimate.  A
    later version can replace ``future_price_multiplier`` with a price
    distribution without changing the crop lifecycle helper.
    """

    future_price_multiplier: float = 1.0
    care_success_probability: float = 1.0
    # None means use the crop's replacement cost. A caller can override this
    # explicitly for a controlled policy experiment.
    seed_opportunity_value: Optional[float] = None
    pass_utility: float = 0.0
    season_days: int = DEFAULT_SEASON_DAYS


DEFAULT_ONGOING_PARAMETERS = OngoingCropParameters()


def _my_farm(obs: dict) -> dict:
    return obs["farms"][obs["player"]]


def _positions_of_crop(farm: dict, crop_name: str):
    for y, row in enumerate(farm["tiles"]):
        for x, tile in enumerate(row):
            if (
                isinstance(tile, dict)
                and tile.get("kind") == "PLANT"
                and tile.get("crop") == crop_name
            ):
                yield (x, y), tile


def _first_empty_position(farm: dict) -> Optional[Tuple[int, int]]:
    for y, row in enumerate(farm["tiles"]):
        for x, tile in enumerate(row):
            if tile is None:
                return (x, y)
    return None


def _move_toward(current: Tuple[int, int], target: Tuple[int, int]):
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


def crop_age(obs: dict, tile: dict) -> int:
    """Return the crop age in whole in-game days."""
    return obs["day"] - tile["planted_day"]


def production_due(crop: OngoingCrop, age: int) -> bool:
    """Whether ``age`` is one of the crop's explicit production ages."""
    return age in crop.production_ages


def can_reach_final_production(
    obs: dict,
    crop: OngoingCrop,
    parameters: OngoingCropParameters = DEFAULT_ONGOING_PARAMETERS,
) -> bool:
    """Whether planting now can reach the final scheduled production."""
    return obs["day"] + crop.final_production_age < parameters.season_days


def expected_ongoing_crop_value(
    obs: dict,
    crop: OngoingCrop,
    parameters: OngoingCropParameters = DEFAULT_ONGOING_PARAMETERS,
) -> float:
    """Estimate the value of planting one crop on an empty tile.

    This first implementation keeps the production schedule deterministic and
    represents care risk with one named probability.  Sale-price uncertainty
    remains a point estimate so the graph can be validated before adding a
    full price distribution.
    """
    current_quote = obs["market"]["prices"].get(crop.name, crop.base_price)
    expected_price = current_quote * parameters.future_price_multiplier
    expected_units = (
        len(crop.production_ages)
        * crop.base_yield_per_production
        * parameters.care_success_probability
    )
    seed_value = (
        crop.seed_cost
        if parameters.seed_opportunity_value is None
        else parameters.seed_opportunity_value
    )
    return expected_units * expected_price - seed_value


def should_plant_ongoing_crop(
    obs: dict,
    crop: OngoingCrop,
    parameters: OngoingCropParameters = DEFAULT_ONGOING_PARAMETERS,
) -> bool:
    """Choose planting only when the explicit estimate beats PASS."""
    farm = _my_farm(obs)
    seeds = obs["private"]["seeds"].get(crop.name, 0)
    can_acquire_seed = seeds > 0 or farm["money"] >= crop.seed_cost
    return (
        can_acquire_seed
        and _first_empty_position(farm) is not None
        and can_reach_final_production(obs, crop, parameters)
        and expected_ongoing_crop_value(obs, crop, parameters)
        > parameters.pass_utility
    )


def _farm_action(
    obs: dict,
    crop: OngoingCrop,
    invest: bool,
    parameters: OngoingCropParameters = DEFAULT_ONGOING_PARAMETERS,
):
    """Care for one active crop, harvest available units, or plant one."""
    farm = _my_farm(obs)
    current = tuple(farm["farmer"])
    active = list(_positions_of_crop(farm, crop.name))

    if active:
        # The one-tile teaching scope deliberately services one crop only.
        target, tile = active[0]
        if current != target:
            return _move_toward(current, target)
        if not tile["watered_today"]:
            return ["WATER"]
        # Unlike a one-time crop, HARVEST does not remove this plant. It only
        # collects currently available units and leaves future production intact.
        if tile.get("yield_units", 0) > 0:
            return ["HARVEST"]
        return ["PASS"]

    if not invest or not can_reach_final_production(obs, crop, parameters):
        return ["PASS"]

    target = _first_empty_position(farm)
    if target is None:
        return ["PASS"]
    if current != target:
        return _move_toward(current, target)
    if obs["private"]["seeds"].get(crop.name, 0) > 0:
        return ["PLANT", crop.name]
    return ["PASS"]


def _market_orders(
    obs: dict,
    crop: OngoingCrop,
    invest: bool,
    parameters: OngoingCropParameters = DEFAULT_ONGOING_PARAMETERS,
):
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
        and can_reach_final_production(obs, crop, parameters)
    ):
        orders.append(["BUY_SEED", crop.name, 1])
    return orders


def conveyor_agent(obs: dict, crop: OngoingCrop) -> dict:
    """Run the one-tile ongoing crop cycle without a utility decision."""
    invest = can_reach_final_production(obs, crop)
    return {
        "farmer": _farm_action(obs, crop, invest),
        "hands": [],
        "market": _market_orders(obs, crop, invest),
    }


def decision_agent(
    obs: dict,
    crop: OngoingCrop,
    parameters: OngoingCropParameters = DEFAULT_ONGOING_PARAMETERS,
) -> dict:
    """Run the ongoing crop loop using plant-versus-PASS utility."""
    invest = should_plant_ongoing_crop(obs, crop, parameters)
    return {
        "farmer": _farm_action(obs, crop, invest, parameters),
        "hands": [],
        "market": _market_orders(obs, crop, invest, parameters),
    }
