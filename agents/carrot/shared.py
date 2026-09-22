"""Readable first framework for the Kaggriculture carrot decision network.

There are two callable Kaggle agents in this file:

``carrot_conveyor_agent``
    A deterministic baseline.  It continuously buys, plants, waters, harvests,
    and sells carrots.  It has no probability model.

``agent`` / ``carrot_decision_agent``
    The learning agent.  It uses a small, explicit belief model to choose
    between ``PLANT CARROT`` and ``PASS`` on an empty tile.

This file is intentionally a framework, not an attempt to play the full game
well.  It contains only the carrot chain; wheat, animals, fertilizer, land, and
farm hands are deliberately out of scope for this first experiment.
"""

from dataclasses import dataclass
from math import floor
from typing import Callable, Dict, List, Optional, Tuple


# ---------------------------------------------------------------------------
# 1. Fixed Kaggriculture facts
# ---------------------------------------------------------------------------
#
# These are rules of the default environment.  They are not learned weights.
# If a match loses, we change beliefs or policy parameters—not these facts.

CARROT = "CARROT"
CARROT_SEED_COST = 20
CARROT_FIRST_YIELD_DAY = 2
CARROT_PEAK_YIELD_DAY = 3
CARROT_BASELINE_YIELD = 3  # Water on both bonus-window days, no fertilizer.
CARROT_BASE_PRICE = 35
SEASON_DAYS = 30
MAX_MARKET_ORDERS = 10

# Only these shop types consume carrots.  The number is carrots per day for
# one active shop instance: Pet Café consumes two every four turns (=12/day),
# Farmers Market consumes one every four turns (=6/day).
CARROT_SHOP_DEMAND_PER_DAY = {
    "PET_CAFE": 12,
    "FARMERS_MARKET": 6,
}
TOWN_CENTER_CARROT_DEMAND_PER_DAY = 1


# ---------------------------------------------------------------------------
# 2. Parameters we are allowed to calibrate
# ---------------------------------------------------------------------------
#
# These values are our stated beliefs and preferences.  They live together so
# a seeded experiment can change exactly one value and record the result.

@dataclass(frozen=True)
class CarrotParameters:
    """All non-game-rule knobs for this first carrot model.

    The three opponent-supply probabilities form a simple conditional table:
    they mean P(opponent carrot supply is high before our sale | visible ripe
    or nearly ripe opponent carrot count falls in this bucket).
    """

    supply_high_given_zero_ripe: float = 0.05
    supply_high_given_one_to_three_ripe: float = 0.30
    supply_high_given_four_plus_ripe: float = 0.70

    # Price scenarios are deliberately coarse.  A later agent may replace
    # them with a direct simulation of the market price function.
    low_price_multiplier: float = 0.60
    high_price_multiplier: float = 1.20

    # The model turns supply/demand evidence into a three-outcome price belief.
    # These are policy-model weights, not probabilities by themselves.
    low_price_base: float = 0.15
    low_price_supply_weight: float = 0.65
    high_price_base: float = 0.05
    high_price_demand_weight: float = 0.02
    high_price_supply_penalty: float = 0.30

    # Consuming an already-owned seed has no immediate bank deduction, but the
    # seed is still an asset that could be saved for a later tile.  We initially
    # value that option at its replacement cost.
    seed_opportunity_value: float = 20.0

    # PASS has utility zero in this first local decision.  This is not an extra
    # lower-bound threshold: the decision compares plant utility directly with
    # zero.
    pass_utility: float = 0.0


DEFAULT_PARAMETERS = CarrotParameters()


# ---------------------------------------------------------------------------
# 3. Observation helpers: convert game dictionaries into clear questions
# ---------------------------------------------------------------------------

def _my_farm(obs: dict) -> dict:
    """Return this player's public farm record."""
    return obs["farms"][obs["player"]]


def _tile_at(farm: dict, position: Tuple[int, int]):
    """Return the tile object at an ``(x, y)`` position."""
    x, y = position
    return farm["tiles"][y][x]


def _is_carrot(tile) -> bool:
    """True only for a live carrot plant, not a weed or another crop."""
    return isinstance(tile, dict) and tile.get("kind") == "PLANT" and tile.get("crop") == CARROT


def _carrot_age(obs: dict, tile: dict) -> int:
    """Age in whole in-game days for a carrot tile."""
    return obs["day"] - tile["planted_day"]


def _can_still_reach_first_carrot_yield(obs: dict) -> bool:
    """Whether a carrot planted today can first yield before the season ends."""
    return obs["day"] + CARROT_FIRST_YIELD_DAY < SEASON_DAYS


def _is_ripe_or_nearly_ripe_carrot(obs: dict, tile) -> bool:
    """Evidence that an opponent carrot may add supply soon.

    A carrot at age one is one day from its first yield; age two or older is
    already harvestable.  Newly planted carrots are intentionally ignored by
    this first belief model because they are weaker near-term supply evidence.
    """
    return _is_carrot(tile) and _carrot_age(obs, tile) >= 1


def visible_opponent_ripe_carrots(obs: dict) -> int:
    """Count public evidence of near-term opponent carrot production."""
    opponent = obs["farms"][1 - obs["player"]]
    return sum(
        1
        for row in opponent["tiles"]
        for tile in row
        if _is_ripe_or_nearly_ripe_carrot(obs, tile)
    )


def active_carrot_demand_per_day(obs: dict) -> int:
    """Return only demand that is already known from active town buildings."""
    shop_demand = sum(
        CARROT_SHOP_DEMAND_PER_DAY.get(shop, 0)
        for shop in obs["town"]["unlocked_shops"]
    )
    return TOWN_CENTER_CARROT_DEMAND_PER_DAY + shop_demand


def _next_shop_can_unlock_before_peak_harvest(obs: dict) -> bool:
    """A small time-horizon gate for the future-shop chance node.

    Shops unlock every three days under the default rules.  This approximation
    asks whether one such unlock could happen before a carrot planted now is
    harvested at its peak on day ``current_day + 3``.  We will replace this
    approximation after characterizing exact turn-processing order.
    """
    peak_harvest_day = obs["day"] + CARROT_PEAK_YIELD_DAY
    next_unlock_day = ((obs["day"] // 3) + 1) * 3
    return next_unlock_day <= peak_harvest_day


def expected_new_shop_carrot_demand_per_day(obs: dict) -> float:
    """Expected extra daily carrot demand from one relevant future shop draw.

    P(Pet Café)=1/8 and it adds 12 carrots/day.
    P(Farmers Market)=1/8 and it adds 6 carrots/day.
    The six other shop types add zero carrot demand.
    """
    if not _next_shop_can_unlock_before_peak_harvest(obs):
        return 0.0
    return (1.0 / 8.0) * 12.0 + (1.0 / 8.0) * 6.0


# ---------------------------------------------------------------------------
# 4. Belief network: evidence -> chance nodes -> expected price
# ---------------------------------------------------------------------------

def probability_of_high_opponent_carrot_supply(
    obs: dict, parameters: CarrotParameters = DEFAULT_PARAMETERS
) -> float:
    """Evaluate the first explicit conditional-probability table.

    This function implements:

        P(HighOpponentCarrotSupply | visible ripe opponent carrot count)

    It returns a probability, not a fact.  The count is public evidence; the
    opponent's private shed and their decision to sell remain hidden.
    """
    ripe_count = visible_opponent_ripe_carrots(obs)
    if ripe_count == 0:
        return parameters.supply_high_given_zero_ripe
    if ripe_count <= 3:
        return parameters.supply_high_given_one_to_three_ripe
    return parameters.supply_high_given_four_plus_ripe


def carrot_price_distribution(
    obs: dict, parameters: CarrotParameters = DEFAULT_PARAMETERS
) -> Dict[str, float]:
    """Return P(LOW), P(NORMAL), and P(HIGH) for carrot price at sale time.

    This is the intentionally small conditional-probability model.  Known
    active demand and expected relevant future-shop demand push probability
    toward HIGH.  High opponent supply pushes probability toward LOW.

    The returned values always sum to one.  They are not yet learned from data;
    they are the transparent initial weights that future seeded experiments can
    calibrate.
    """
    high_supply = probability_of_high_opponent_carrot_supply(obs, parameters)
    known_demand = active_carrot_demand_per_day(obs)
    future_demand = expected_new_shop_carrot_demand_per_day(obs)
    demand_signal = known_demand + future_demand

    probability_low = parameters.low_price_base + parameters.low_price_supply_weight * high_supply
    probability_high = (
        parameters.high_price_base
        + parameters.high_price_demand_weight * demand_signal
        - parameters.high_price_supply_penalty * high_supply
    )

    # Keep each probability meaningful while our first coarse model is small.
    probability_low = min(0.95, max(0.02, probability_low))
    probability_high = min(0.95, max(0.02, probability_high))

    # If LOW and HIGH together would exceed one, preserve their relative size
    # but reserve at least 2% probability for NORMAL.
    total_extremes = probability_low + probability_high
    if total_extremes > 0.98:
        scale = 0.98 / total_extremes
        probability_low *= scale
        probability_high *= scale

    return {
        "LOW": probability_low,
        "NORMAL": 1.0 - probability_low - probability_high,
        "HIGH": probability_high,
    }


def expected_carrot_sale_price(
    obs: dict, parameters: CarrotParameters = DEFAULT_PARAMETERS
) -> float:
    """Convert the price distribution into one expected carrot sale price."""
    current_price = obs["market"]["prices"].get(CARROT, CARROT_BASE_PRICE)
    distribution = carrot_price_distribution(obs, parameters)
    low_price = max(1.0, current_price * parameters.low_price_multiplier)
    normal_price = float(current_price)
    high_price = current_price * parameters.high_price_multiplier
    return (
        distribution["LOW"] * low_price
        + distribution["NORMAL"] * normal_price
        + distribution["HIGH"] * high_price
    )


def carrot_plant_utility(
    obs: dict, parameters: CarrotParameters = DEFAULT_PARAMETERS
) -> float:
    """Expected utility of consuming one already-owned carrot seed now.

    The seed was bought earlier, so its 20 coins are not subtracted as a second
    immediate cash payment.  Instead, ``seed_opportunity_value`` represents
    the value of retaining the seed for a future planting opportunity.
    """
    expected_revenue = CARROT_BASELINE_YIELD * expected_carrot_sale_price(obs, parameters)
    return expected_revenue - parameters.seed_opportunity_value


def should_plant_carrot(
    obs: dict, parameters: CarrotParameters = DEFAULT_PARAMETERS
) -> bool:
    """The entire first economic decision: PLANT CARROT versus PASS.

    There is no extra lower-bound threshold.  ``PASS`` has utility zero, so we
    plant only when the carrot's expected utility is strictly greater.
    """
    return carrot_plant_utility(obs, parameters) > parameters.pass_utility


def explain_carrot_decision(
    obs: dict, parameters: CarrotParameters = DEFAULT_PARAMETERS
) -> dict:
    """Return inspectable evidence for local experiments and replay analysis."""
    distribution = carrot_price_distribution(obs, parameters)
    utility = carrot_plant_utility(obs, parameters)
    return {
        "visible_opponent_ripe_carrots": visible_opponent_ripe_carrots(obs),
        "high_opponent_supply_probability": probability_of_high_opponent_carrot_supply(obs, parameters),
        "known_carrot_demand_per_day": active_carrot_demand_per_day(obs),
        "expected_new_shop_carrot_demand_per_day": expected_new_shop_carrot_demand_per_day(obs),
        "price_distribution": distribution,
        "expected_sale_price": expected_carrot_sale_price(obs, parameters),
        "plant_utility": utility,
        "pass_utility": parameters.pass_utility,
        "recommendation": "PLANT CARROT" if utility > parameters.pass_utility else "PASS",
    }


# ---------------------------------------------------------------------------
# 5. Deterministic farm-care policy shared by both agents
# ---------------------------------------------------------------------------
#
# Care is not a probability decision in this first experiment.  A missed
# watering can destroy a crop, so care has priority over economic investment.

def _nearest_tile(
    farm: dict,
    origin: Tuple[int, int],
    predicate: Callable[[object], bool],
) -> Optional[Tuple[int, int]]:
    """Find the closest tile for which ``predicate(tile)`` is true."""
    candidates: List[Tuple[int, int, int]] = []
    for y, row in enumerate(farm["tiles"]):
        for x, tile in enumerate(row):
            if predicate(tile):
                distance = abs(origin[0] - x) + abs(origin[1] - y)
                candidates.append((distance, x, y))
    return min(candidates)[1:] if candidates else None


def _move_toward(origin: Tuple[int, int], destination: Optional[Tuple[int, int]]) -> List[str]:
    """Take one Manhattan-distance step, or PASS when no destination exists."""
    if destination is None:
        return ["PASS"]
    x, y = origin
    target_x, target_y = destination
    if x < target_x:
        return ["EAST"]
    if x > target_x:
        return ["WEST"]
    if y < target_y:
        return ["SOUTH"]
    if y > target_y:
        return ["NORTH"]
    return ["PASS"]


def _carrot_needs_water(tile) -> bool:
    """A carrot needs at most one WATER action each in-game day."""
    return _is_carrot(tile) and not tile.get("watered_today", False)


def _carrot_ready_at_peak(obs: dict, tile) -> bool:
    """Harvest only at the baseline plan's peak day, rather than at first yield."""
    return (
        _is_carrot(tile)
        and _carrot_age(obs, tile) >= CARROT_PEAK_YIELD_DAY
        and tile.get("yield_units", 0) > 0
    )


def _farm_action(obs: dict, plant_when_empty: bool) -> List[str]:
    """Choose one farmer action while giving safety-critical work priority."""
    farm = _my_farm(obs)
    position = tuple(farm["farmer"])
    tile = _tile_at(farm, position)

    # Actions on the current tile are always preferable to movement.
    if _carrot_needs_water(tile):
        return ["WATER"]
    if _carrot_ready_at_peak(obs, tile):
        return ["HARVEST"]

    # The local economic decision only occurs on an empty current tile.
    if tile is None and plant_when_empty and obs["private"]["seeds"].get(CARROT, 0) > 0:
        return ["PLANT", CARROT]

    # Move to care work first, then to harvest work, then to an empty planting
    # tile.  This gives the baseline and decision agent identical field skills.
    water_target = _nearest_tile(farm, position, _carrot_needs_water)
    if water_target is not None:
        return _move_toward(position, water_target)

    harvest_target = _nearest_tile(
        farm, position, lambda candidate: _carrot_ready_at_peak(obs, candidate)
    )
    if harvest_target is not None:
        return _move_toward(position, harvest_target)

    if plant_when_empty:
        empty_target = _nearest_tile(farm, position, lambda candidate: candidate is None)
        return _move_toward(position, empty_target)

    return ["PASS"]


# ---------------------------------------------------------------------------
# 6. Market helpers and the two comparable agents
# ---------------------------------------------------------------------------

def _empty_unlocked_tile_count(farm: dict) -> int:
    """Count plantable tiles.  ``None`` represents an empty unlocked tile."""
    return sum(tile is None for row in farm["tiles"] for tile in row)


def _carrot_market_orders(obs: dict, invest_in_carrot: bool) -> List[List[object]]:
    """Buy seeds for the policy and sell carrots already in the shed.

    This first framework sells all shed carrots at the observed current price.
    It intentionally does not yet model batch price slippage or sell-versus-hold
    decisions; those belong to the next market decision network.
    """
    farm = _my_farm(obs)
    private = obs["private"]
    orders: List[List[object]] = []

    carrot_in_shed = private["shed"].get(CARROT, 0)
    if carrot_in_shed > 0:
        orders.append(["SELL", CARROT, carrot_in_shed])

    if not invest_in_carrot:
        return orders

    empty_tiles = _empty_unlocked_tile_count(farm)
    seeds_owned = private["seeds"].get(CARROT, 0)
    seeds_needed = max(0, min(10, empty_tiles - seeds_owned))
    affordable = floor(farm["money"] / CARROT_SEED_COST)
    if seeds_needed > 0 and affordable > 0:
        orders.append(["BUY_SEED", CARROT, min(seeds_needed, affordable)])

    return orders[:MAX_MARKET_ORDERS]


def carrot_conveyor_agent(obs: dict) -> dict:
    """Baseline agent: continuously grow carrots whenever a tile is available.

    It never evaluates probabilities.  Use it as the comparison point for the
    decision-network agent on an identical fixed-seed match suite.
    """
    return {
        "farmer": _farm_action(obs, plant_when_empty=True),
        "hands": [],
        "market": _carrot_market_orders(obs, invest_in_carrot=True),
    }


def carrot_decision_agent(
    obs: dict, parameters: CarrotParameters = DEFAULT_PARAMETERS
) -> dict:
    """Carrot agent whose only economic branch is PLANT CARROT versus PASS."""
    farm = _my_farm(obs)
    can_invest = _can_still_reach_first_carrot_yield(obs) and should_plant_carrot(obs, parameters)
    return {
        "farmer": _farm_action(obs, plant_when_empty=can_invest),
        "hands": [],
        "market": _carrot_market_orders(obs, invest_in_carrot=can_invest),
    }


# Kaggle looks for a function called ``agent`` when this file is submitted.
# Keeping this alias means the decision-network version is runnable directly.
agent = carrot_decision_agent
