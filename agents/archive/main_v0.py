"""A small symbolic/probabilistic Kaggriculture agent.

The rule layer handles things that are known exactly (water before a crop
dies, harvest when ripe, and shortest-path movement).  The decision layer is
an AIMA-style decision network in miniature: future shop instances and the
opponent's unobserved stock are chance variables; crop choice is a decision;
and final cash is the utility.  It uses hand-written priors, not training.
"""

from math import sqrt


CROPS = {
    "WHEAT": (10, 2, 4, 4),
    "CARROT": (20, 2, 3, 3),
    "TOMATO": (50, 8, 11, 4),
    "STRAWBERRY": (100, 10, 16, 4),
    "MELON": (80, 10, 12, 6),
}
BASE_PRICE = {"WHEAT": 25, "CARROT": 35, "TOMATO": 60,
              "STRAWBERRY": 120, "MELON": 250, "EGG": 50,
              "MILK": 160, "WOOL": 200, "FERTILIZER": 100}
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
    """P(product demanded by one uniformly drawn future shop instance)."""
    return sum(items.count(product) for items in SHOP_PRODUCTS.values()) / 8.0


def _future_shop_slots(obs):
    """Number of still-random shop draws before season end (default rules)."""
    known = len(obs["town"]["unlocked_shops"])
    # Unlocks occur every 3 days, up to eight instances.  Days are an observed
    # clock, therefore this is a prior only over the *identity* of each shop.
    total_by_finish = min(8, max(0, 29 // 3))
    return max(0, total_by_finish - known)


def _opponent_supply_mean(obs, product):
    """Visible-tile evidence for otherwise hidden opponent inventory/sales.

    This deliberately returns an expectation, rather than pretending that the
    opponent's shed is observable.  A planted crop is weak evidence of future
    sales; an animal is stronger evidence of continuing production.
    """
    other = obs["farms"][1 - obs["player"]]
    crop_to_product = {name: name for name in CROPS}
    animal_to_product = {"GOOSE": "EGG", "COW": "MILK", "SHEEP": "WOOL"}
    evidence = 0.0
    for row in other["tiles"]:
        for tile in row:
            if not isinstance(tile, dict):
                continue
            if tile.get("kind") == "PLANT" and crop_to_product.get(tile.get("crop")) == product:
                evidence += 2.0
            elif animal_to_product.get(tile.get("animal")) == product:
                evidence += 3.0
    return evidence


def _belief(obs, product, own_units=0):
    """Return a conservative price estimate and its uncertainty.

    The binomial term is exact for independent uniform future shop draws.
    The opponent term is a stated, deliberately broad prior.  We use a lower
    confidence bound for revenue: a risk-sensitive expected-utility policy.
    """
    days_left = max(1, 30 - obs["day"])
    market = obs["market"]
    current_price = market["prices"].get(product, BASE_PRICE[product])
    active = obs["town"]["unlocked_shops"]
    active_rate = sum(SHOP_PRODUCTS.get(shop, ()).count(product) for shop in active)
    p = _shop_probability(product)
    draws = _future_shop_slots(obs)
    # Each shop consumes its listed item six times per day (24 / 4).
    expected_demand = days_left * 6.0 * (active_rate + draws * p)
    demand_sd = days_left * 6.0 * sqrt(draws * p * (1.0 - p))
    opponent_mean = _opponent_supply_mean(obs, product)
    # Inventory is observed.  Demand raises price; sales (ours/opponent's) lower
    # it.  A local linear approximation is enough for ranking choices.
    scarcity_pressure = (expected_demand - opponent_mean - own_units) / 10000.0
    mean = max(1.0, current_price * (1.0 + 0.65 * scarcity_pressure))
    sd = current_price * (0.12 + 0.65 * demand_sd / 10000.0)
    return mean, sd


def _choose_crop(obs):
    """Maximise conservative expected net value per occupied day."""
    best, best_value = "WHEAT", float("-inf")
    for crop, (seed_cost, first_day, finish_day, yield_units) in CROPS.items():
        if obs["day"] + first_day >= 30:
            continue
        mean, sd = _belief(obs, crop, yield_units)
        # Lower 80%-ish bound: a symbolic risk preference, not a classifier.
        safe_price = max(1.0, mean - 0.85 * sd)
        value = (yield_units * safe_price - seed_cost) / max(1, finish_day)
        if value > best_value:
            best, best_value = crop, value
    return best


def _move_toward(source, target):
    x, y = source
    tx, ty = target
    if x < tx: return ["EAST"]
    if x > tx: return ["WEST"]
    if y < ty: return ["SOUTH"]
    if y > ty: return ["NORTH"]
    return ["PASS"]


def _nearest(farm, origin, predicate):
    found = []
    for y, row in enumerate(farm["tiles"]):
        for x, tile in enumerate(row):
            if predicate(tile):
                found.append((abs(origin[0] - x) + abs(origin[1] - y), x, y))
    return min(found)[1:] if found else None


def _market_orders(obs, crop):
    private, me = obs["private"], obs["farms"][obs["player"]]
    orders = []
    # A seed purchased this turn is usable next turn.  Keep enough to fill the
    # currently empty unlocked tiles, while retaining cash for operations.
    empty = sum(tile is None for row in me["tiles"] for tile in row)
    seed_cost = CROPS[crop][0]
    wanted = max(0, min(10, empty - private["seeds"].get(crop, 0)))
    affordable = int(max(0, me["money"] - 100) // seed_cost)
    if wanted and affordable:
        orders.append(["BUY_SEED", crop, min(wanted, affordable)])

    # Sell only when today's price is at least our conservative forecast, or
    # storage is becoming scarce.  This is the action selected by the decision
    # node after observing the current price evidence.
    stored = sum(private["shed"].values())
    for item, quantity in private["shed"].items():
        if item not in BASE_PRICE or quantity <= 0:
            continue
        mean, sd = _belief(obs, item, quantity)
        if obs["market"]["prices"].get(item, 0) >= mean - 0.35 * sd or stored >= 80:
            orders.append(["SELL", item, quantity])
    return orders[:10]