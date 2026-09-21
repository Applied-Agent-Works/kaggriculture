"""A commented symbolic-probabilistic Kaggriculture agent.

This is deliberately not a machine-learning agent.  It has three parts:

1. Rules for facts that are fully known from the observation.  For example, a
   crop that needs watering is watered; this is ordinary symbolic reasoning.
2. A small probability model for facts we cannot see: future random shops and
   the opponent's private shed.
3. A decision rule that selects the crop with the best *conservative* expected
   profit.  In AIMA terms, crop choice is a decision node and money is utility.

The Kaggle runner calls ``agent(observation)`` once every turn.
"""

from math import sqrt


# Each crop is represented as:
#     seed price, first harvest day, typical finishing day, harvest units
# These are known game rules, so this table belongs to the symbolic part.
CROPS = {
    "WHEAT": (10, 2, 4, 4),
    "CARROT": (20, 2, 3, 3),
    "TOMATO": (50, 8, 11, 4),
    "STRAWBERRY": (100, 10, 16, 4),
    "MELON": (80, 10, 12, 6),
}

# Base prices are fallback values.  We normally use the current observed price
# at obs["market"]["prices"], but a fallback makes the belief code robust if a
# custom environment leaves a price out of the observation.
BASE_PRICE = {
    "WHEAT": 25, "CARROT": 35, "TOMATO": 60, "STRAWBERRY": 120,
    "MELON": 250, "EGG": 50, "MILK": 160, "WOOL": 200,
    "FERTILIZER": 100,
}

# A shop name maps to the products it consumes every market refresh.  WOOL and
# CARROT appear twice for the two single-product shops because those shops
# consume double quantity.  This makes counting demand simple below.
SHOP_PRODUCTS = {
    "BAKERY": ("EGG", "WHEAT"),
    "PIZZA_SHOP": ("MILK", "TOMATO", "WHEAT"),
    "BRUNCH_SPOT": ("EGG", "WHEAT", "STRAWBERRY"),
    "YARN_STORE": ("WOOL", "WOOL"),
    "ICE_CREAM_SHOP": ("STRAWBERRY", "MILK", "WHEAT"),
    "PET_CAFE": ("CARROT", "CARROT"),
    "SMOOTHIE_SHOP": ("STRAWBERRY", "MILK"),
    "FARMERS_MARKET": ("WHEAT", "CARROT", "TOMATO", "STRAWBERRY"),
}


def _shop_probability(product):
    """Return P(a future random shop demands ``product``).

    Each future shop is drawn uniformly from the eight shop types.  For
    example, wheat appears in four of the eight entries, hence its probability
    is 4 / 8.  This is a prior: before a shop is actually revealed, every shop
    type has equal probability.
    """
    number_of_demanding_shop_types = sum(
        items.count(product) for items in SHOP_PRODUCTS.values()
    )
    return number_of_demanding_shop_types / len(SHOP_PRODUCTS)


def _future_shop_slots(obs):
    """Count shop draws whose *type* is still unknown.

    Under the default rules, a shop can unlock every three days and at most
    eight shop instances exist.  The current list of unlocked shops is an
    observed fact; only the identities of future draws are uncertain.
    """
    already_known = len(obs["town"]["unlocked_shops"])
    total_unlocks_by_season_end = min(8, 29 // 3)
    return max(0, total_unlocks_by_season_end - already_known)


def _opponent_supply_mean(obs, product):
    """Estimate how much future supply the opponent is likely to sell.

    We cannot see the opponent's shed.  We *can* see their tiles, which are
    evidence: an observed planted crop suggests some future supply, and an
    observed animal suggests recurring supply.  The weights 2 and 3 are simple
    stated priors, not facts discovered from data.  Adjust them if matches show
    that opponents harvest or sell more/less aggressively.
    """
    opponent_index = 1 - obs["player"]
    opponent_farm = obs["farms"][opponent_index]
    crop_to_product = {crop: crop for crop in CROPS}
    animal_to_product = {"GOOSE": "EGG", "COW": "MILK", "SHEEP": "WOOL"}

    expected_supply = 0.0
    for row in opponent_farm["tiles"]:
        for tile in row:
            # Empty squares, locked squares, and weeds are strings/None, not
            # object dictionaries, and provide no production evidence.
            if not isinstance(tile, dict):
                continue

            if (
                tile.get("kind") == "PLANT"
                and crop_to_product.get(tile.get("crop")) == product
            ):
                expected_supply += 2.0
            elif animal_to_product.get(tile.get("animal")) == product:
                expected_supply += 3.0

    return expected_supply


def _belief(obs, product, own_units=0):
    """Estimate a future price as ``(mean, standard_deviation)``.

    This is the uncertain-reasoning core.  It does not claim to know tomorrow's
    price.  Instead, it represents a price estimate and how uncertain it is.

    - Known evidence: today's price, today\'s market, unlocked shops, the day.
    - Chance variable: identity of each future shop draw.
    - Hidden variable: opponent stock/sales, estimated from visible tiles.

    ``standard_deviation`` is larger when many future shop draws are unknown.
    A later function uses it to prefer safer choices when two crops have close
    expected returns.
    """
    days_left = max(1, 30 - obs["day"])
    market = obs["market"]
    current_price = market["prices"].get(product, BASE_PRICE[product])

    # Demand from shops that have already been revealed is no longer random.
    active_shops = obs["town"]["unlocked_shops"]
    known_demand_per_refresh = sum(
        SHOP_PRODUCTS.get(shop, ()).count(product) for shop in active_shops
    )

    # For an unrevealed shop, p is the Bernoulli probability that it demands
    # product.  With n independent draws, E[count] = n*p and
    # SD[count] = sqrt(n*p*(1-p)); this is the binomial distribution.
    probability = _shop_probability(product)
    unknown_draws = _future_shop_slots(obs)
    expected_unknown_demand = unknown_draws * probability
    unknown_demand_sd = sqrt(
        unknown_draws * probability * (1.0 - probability)
    )

    # A shop consumes its listed product six times per day (24 turns / 4).
    # This converts a number of shops into expected units of seasonal demand.
    expected_demand = days_left * 6.0 * (
        known_demand_per_refresh + expected_unknown_demand
    )
    demand_sd = days_left * 6.0 * unknown_demand_sd

    opponent_mean = _opponent_supply_mean(obs, product)

    # More demand makes a product scarcer and tends to raise price.  Sales by
    # us or the opponent do the opposite.  The game\'s real price curve is
    # nonlinear; this small local approximation is only used to rank choices.
    scarcity_pressure = (expected_demand - opponent_mean - own_units) / 10000.0
    mean_price = max(1.0, current_price * (1.0 + 0.65 * scarcity_pressure))

    # 12% is a deliberately broad baseline uncertainty.  The second term adds
    # uncertainty from the unknown shop identities.
    price_sd = current_price * (0.12 + 0.65 * demand_sd / 10000.0)
    return mean_price, price_sd


def _choose_crop(obs):
    """Choose the crop with the highest risk-adjusted profit per tile-day."""
    chosen_crop = "WHEAT"
    best_value = float("-inf")

    for crop, (seed_cost, first_yield_day, finish_day, yield_units) in CROPS.items():
        # Do not start a crop that cannot produce before the season ends.
        if obs["day"] + first_yield_day >= 30:
            continue

        mean_price, price_sd = _belief(obs, crop, own_units=yield_units)

        # Rather than act on the mean alone, use a cautious price: mean minus
        # 0.85 standard deviations.  This is the agent\'s risk preference.  A
        # higher number makes it more cautious; zero makes it risk-neutral.
        cautious_price = max(1.0, mean_price - 0.85 * price_sd)

        # Utility approximation: revenue minus seed cost, divided by the time
        # for which the tile is occupied.  The division avoids always choosing
        # a slow expensive crop simply because its total sale price is high.
        value_per_tile_day = (
            yield_units * cautious_price - seed_cost
        ) / max(1, finish_day)

        if value_per_tile_day > best_value:
            chosen_crop = crop
            best_value = value_per_tile_day

    return chosen_crop


def _move_toward(source, target):
    """Return one legal step that reduces Manhattan distance to ``target``."""
    x, y = source
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


def _nearest(farm, origin, predicate):
    """Find the closest tile satisfying ``predicate``, or return ``None``."""
    candidates = []
    for y, row in enumerate(farm["tiles"]):
        for x, tile in enumerate(row):
            if predicate(tile):
                manhattan_distance = abs(origin[0] - x) + abs(origin[1] - y)
                candidates.append((manhattan_distance, x, y))

    # Tuples sort by distance, then x, then y.  This gives deterministic ties.
    return min(candidates)[1:] if candidates else None


def _market_orders(obs, crop):
    """Create the market portion of this turn's action dictionary."""
    private = obs["private"]
    my_farm = obs["farms"][obs["player"]]
    orders = []

    # Buy enough of the selected crop to fill empty tiles, at most ten at once.
    # Retaining $100 prevents the agent from spending every coin on seeds.
    empty_tiles = sum(tile is None for row in my_farm["tiles"] for tile in row)
    seed_cost = CROPS[crop][0]
    seeds_already_owned = private["seeds"].get(crop, 0)
    seeds_wanted = max(0, min(10, empty_tiles - seeds_already_owned))
    seeds_affordable = int(max(0, my_farm["money"] - 100) // seed_cost)
    if seeds_wanted and seeds_affordable:
        orders.append(["BUY_SEED", crop, min(seeds_wanted, seeds_affordable)])

    # Decide whether to sell every kind of product in the shed.  We sell now
    # when today's observed price beats a moderately cautious future forecast,
    # or when the 100-item shed is close to full and storage loss is a danger.
    total_stored_items = sum(private["shed"].values())
    for item, quantity in private["shed"].items():
        if item not in BASE_PRICE or quantity <= 0:
            continue

        mean_price, price_sd = _belief(obs, item, own_units=quantity)
        sell_threshold = mean_price - 0.35 * price_sd
        current_price = obs["market"]["prices"].get(item, 0)
        if current_price >= sell_threshold or total_stored_items >= 80:
            orders.append(["SELL", item, quantity])

    # Kaggriculture silently drops orders after its per-turn limit.  Limiting
    # them ourselves makes the agent's behavior explicit and predictable.
    return orders[:10]


def agent(obs):
    """Return one farmer action plus this turn's market orders."""
    player = obs["player"]
    my_farm = obs["farms"][player]
    farmer_position = tuple(my_farm["farmer"])
    current_tile = my_farm["tiles"][farmer_position[1]][farmer_position[0]]

    # This is the decision-node output: which new crop is worth planting now?
    preferred_crop = _choose_crop(obs)

    # Priority 1: known, irreversible maintenance.  Watering comes before new
    # investments because failing to water can turn a crop into a weed.
    if isinstance(current_tile, dict) and current_tile.get("kind") == "PLANT":
        crop_age = obs["day"] - current_tile["planted_day"]
        first_yield_day = CROPS[current_tile["crop"]][1]
        if crop_age >= first_yield_day and current_tile.get("yield_units", 0) > 0:
            farmer_action = ["HARVEST"]
        elif not current_tile.get("watered_today", False):
            farmer_action = ["WATER"]
        else:
            farmer_action = ["PASS"]

    # Priority 2: on an empty square, plant the crop selected above if a seed
    # was bought on a previous turn.
    elif current_tile is None and obs["private"]["seeds"].get(preferred_crop, 0) > 0:
        farmer_action = ["PLANT", preferred_crop]

    else:
        # Priority 3: walk to the closest unwatered plant.  If every plant is
        # safe, walk to an empty square so the next turn can plant a seed.
        urgent_plant = _nearest(
            my_farm,
            farmer_position,
            lambda tile: (
                isinstance(tile, dict)
                and tile.get("kind") == "PLANT"
                and not tile.get("watered_today", False)
            ),
        )
        empty_tile = _nearest(my_farm, farmer_position, lambda tile: tile is None)
        destination = urgent_plant or empty_tile
        farmer_action = (
            _move_toward(farmer_position, destination)
            if destination is not None
            else ["PASS"]
        )

    # We do not hire hands in this first version.  They are an independent
    # extension: add a utility estimate for labour saved versus hire cost.
    return {
        "farmer": farmer_action,
        "hands": [],
        "market": _market_orders(obs, preferred_crop),
    }
