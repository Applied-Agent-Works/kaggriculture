# Tomato Decision Network

This is the first ongoing-crop decision graph. Its narrow question is:

> Given one empty unlocked tile and a Tomato seed or enough money to buy one,
> should the farmer plant Tomato now or choose `PASS`?

Tomato remains on the tile after each harvest and produces on four scheduled
production ages.

## Fixed Tomato facts

| Fact | Default value |
|---|---:|
| Seed cost | 50 |
| Base market price | 60 |
| Production ages | 8, 9, 10, 11 |
| Scheduled productions | 4 |
| Base yield per production | 1 |
| Fertilized and watered production yield | 2 |
| Care | Water every day |
| After the fourth production | Plant begins decay |

These are game rules, not tunable policy weights.

## Influence diagram

```mermaid
flowchart LR
    State[Observed day, tile, seed, money]
    Market[Observed Tomato market price and inventory]
    Demand[Observed town demand and opponent crop evidence]
    Care[Belief: care remains feasible]
    Schedule[Fixed schedule: ages 8, 9, 10, 11]
    Production[Chance: four production outcomes]
    Price[Chance: future sale-price estimate]
    Plant[Decision: PLANT TOMATO or PASS]
    Utility{{Utility: expected net value}}
    Decay[Fixed: decay after fourth production]

    State --> Plant
    State --> Schedule
    Market --> Price
    Demand --> Price
    Care --> Production
    Schedule --> Production
    Plant --> Production
    Production --> Utility
    Price --> Utility
    Plant --> Utility
    Decay --> Utility
```

The schedule is deterministic. Uncertainty initially comes from care
feasibility and future sale price, not from pretending the game rules are
random.

## Utility

The first model uses a transparent point estimate for future sale price:

$$
E[Yield_i] = P(CareSuccess) \times 1
$$

$$
U(PlantTomato) =
\sum_{i=1}^{4} E[Yield_i \times SalePrice_i]
 - SeedOpportunityValue
$$

$$
U(PASS)=0
$$

The decision plants only when:

$$
U(PlantTomato) > U(PASS)
$$

The initial code uses today's Tomato quote multiplied by a named
`future_price_multiplier`. A later graph version can replace that point
estimate with a `LOW`/`NORMAL`/`HIGH` price distribution.

## Care and harvest semantics

The farmer waters the active Tomato every day. On a production day, a
fertilized and watered plant can produce two units; otherwise the base
production is one unit. `HARVEST` collects available units but does not remove
the plant, so later scheduled production remains possible. After the fourth
scheduled production, the plant begins the game's decay process.

## Known limitations

- One farmer and one active Tomato are modeled; multi-tile scheduling is out
  of scope.
- Fertilizer acquisition and market timing are not decision nodes yet.
- Future price is a point estimate, not a calibrated probability distribution.
- The first utility model does not value crop-choice portfolios or animal-feed
  production chains.
- A later integration test must verify exact production and decay timing.
