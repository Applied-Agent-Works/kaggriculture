# Kaggriculture: Getting Started

This guide walks you through building an agent, testing it locally, and
submitting it to the Kaggriculture competition on Kaggle.

For the complete game rules, object tables, action definitions, price function,
and turn-processing order, see [README.md](README.md).

## Game Overview

Kaggriculture is a two-player farming simulation. Each player manages a farm
and competes to earn the most coins by buying seeds and livestock, planting,
watering, harvesting, raising animals, hiring help, and trading on a dynamic
market over a fixed season.

- **Farm** — each player has a `boardSize` × `boardSize` grid (default 10 ×
  10) divided into four 5 × 5 quadrants. Only NW is unlocked initially; NE,
  SW, and SE cost $1k, $2k, and $4k through `BUY_LAND`.
- **Starting bank** — `startingMoney` defaults to $3000.
- **Farmer and hands** — one main farmer plus hands hired per day. The next
  hire cost is `farmHandCostMult * fib(n)` for hires already made that day;
  the default series is `1, 1, 2, 3, 5, 8, ...`, resetting daily.
- **Crops** — Wheat, Carrot, Tomato, Strawberry, and Melon have different
  costs, growth, yield curves, and prices.
- **Watering bonus** — watering one-time crops in their bonus window increases
  yield; fertilizer doubles that bonus for three days. Ongoing crops produce
  more when both fertilized and watered on their production day.
- **Animals** — Goose, Cow, and Sheep require a coop or pasture and daily
  wheat feed. `CARE` banks a bonus for the next production, and
  `COLLECT_FERTILIZER` gathers one fertilizer per animal per day.
- **Care failure** — two consecutive missed end-of-day refreshes turn plants
  into weeds or make animals escape. Planting day counts as unwatered.
- **Shed** — non-seed inventory is capped at 100 items. Seeds are separate and
  consumed directly by `PLANT`.
- **Market** — seed, animal, and `BUY_PRODUCT` prices are fixed; sales prices
  vary with shared inventory. Only wheat and fertilizer can be bought back.
- **Town** — the town center consumes each non-fertilizer product once per
  day. Further shops unlock randomly with replacement and consume on their
  own schedules.
- **Season** — the default is 24 turns/day × 30 days = 720 turns.

## Agent Interface

An agent receives an observation and returns an action dictionary:

```py
{
  "farmer": [op, ...args],
  "hands": [[op, ...args], ...],
  "market": [[op, ...args], ...],
}
```

The observation contains the player index, turn/day/hour, both public farms,
the player's private shed/seeds/carried inventories, shared market state, and
active town shops. Both farms are visible, but only the current player's
private state is visible. Refer to [README.md](README.md) for complete field
and operation definitions.

## Example: Wheat Loop

For wheat (`first_yield_day = 2`, `max_yield_day = 4`), plant, water during
the bonus window, and harvest once it yields:

```python
def agent(obs):
    player = obs["player"]
    me = obs["farms"][player]
    private = obs["private"]
    fx, fy = me["farmer"]
    tile = me["tiles"][fy][fx]

    market = []
    if private["seeds"].get("WHEAT", 0) == 0 and me["money"] >= 10:
        market.append(["BUY_SEED", "WHEAT", 1])
    wheat_in_shed = private["shed"].get("WHEAT", 0)
    if wheat_in_shed > 0:
        market.append(["SELL", "WHEAT", wheat_in_shed])

    if tile is None and private["seeds"].get("WHEAT", 0) > 0:
        return {"farmer": ["PLANT", "WHEAT"], "hands": [], "market": market}
    if isinstance(tile, dict) and tile.get("kind") == "PLANT":
        crop_age = obs["day"] - tile["planted_day"]
        if crop_age >= 2:
            return {"farmer": ["HARVEST"], "hands": [], "market": market}
        if not tile["watered_today"]:
            return {"farmer": ["WATER"], "hands": [], "market": market}

    return {"farmer": ["PASS"], "hands": [], "market": market}
```

## Test Locally

Install the environment:

```bash
pip install -U kaggle-environments
```

Run a match from Python, a notebook, or an agent file:

```python
from kaggle_environments import make

env = make("kaggriculture", configuration={"episodeSteps": 720}, debug=True)
env.run(["main.py", "random"])

for i, state in enumerate(env.steps[-1]):
    print(f"Player {i}: reward={state.reward}, status={state.status}")

env.render(mode="ipython", width=1200, height=800)
```

The repository also includes a lightweight runner:

```bash
.venv/bin/python run_match.py --agent main.py --opponent random --steps 720 --seed 42
```

The built-in agents are `pass`, `random`, and `starter`.

## Set Up the Kaggle CLI

Install the CLI:

```bash
pip install kaggle
```

Create a Kaggle account, then obtain an API token from
<https://www.kaggle.com/settings/api>. A recommended setup is:

```bash
mkdir -p ~/.kaggle
# Paste the token from the Kaggle settings UI into this file.
nano ~/.kaggle/access_token
chmod 600 ~/.kaggle/access_token
```

Alternative authentication methods are `kaggle auth login` and the
`KAGGLE_API_TOKEN` environment variable. Verify the CLI with:

```bash
kaggle competitions list -s "kaggriculture"
```

## Competition Workflow

Find the competition and read its page:

```bash
kaggle competitions list -s "kaggriculture"
kaggle competitions pages kaggriculture
kaggle competitions pages kaggriculture --content
```

Before submitting, accept the competition rules on the Kaggle website. Then
download competition data if needed:

```bash
kaggle competitions download kaggriculture -p kaggriculture-data
```

Submissions require a root `main.py` containing an `agent` function:

```bash
kaggle competitions submit kaggriculture -f main.py -m "Carrot decision v1"
```

For multiple files, bundle `main.py` at the archive root:

```bash
tar -czf submission.tar.gz main.py helper.py model_weights.pkl
kaggle competitions submit kaggriculture -f submission.tar.gz -m "Multi-file agent v1"
```

Monitor submissions and inspect episodes, replays, logs, and the leaderboard:

```bash
kaggle competitions submissions kaggriculture
kaggle competitions episodes <SUBMISSION_ID>
kaggle competitions replay <EPISODE_ID> -p ./replays
kaggle competitions logs <EPISODE_ID> 0 -p ./logs
kaggle competitions leaderboard kaggriculture -s
```

## Typical Workflow

```bash
# Test locally with a reproducible seed.
.venv/bin/python run_match.py --agent main.py --opponent random --steps 720 --seed 42

# Submit after accepting the competition rules.
kaggle competitions submit kaggriculture -f main.py -m "v1"

# Inspect results.
kaggle competitions submissions kaggriculture
kaggle competitions episodes <SUBMISSION_ID>
```
