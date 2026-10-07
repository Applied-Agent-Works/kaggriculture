# Temporary Memory — AI Learning Through Kaggriculture

## User intent

The user is revisiting classic symbolic/probabilistic AI after roughly twenty
years, using *Artificial Intelligence: A Modern Approach* (first edition,
ISBN `0-13-103805-2`) as inspiration. They are not pursuing runtime ML or LLM
agents. The aim is readable symbolic, probabilistic, and decision-network
agents whose explicit assumptions/weights can be inspected and adjusted.

Longer term, Microsoft Foundry LLMs may act *offline* as analysts that propose
and explain parameter changes. They should not replace the explicit decision
policy. Candidate changes should be evaluated on fixed-seed local runs.

## Existing learning documents

- [`kaggriculture-ai-notes.md`](../learning-notes/kaggriculture-ai-notes.md): logic notation, probability, conditional
  probability, and initial decision-network notes.
- [`carrot-decision-network.md`](../decision-networks/carrot-decision-network.md): focused carrot influence diagram, PASS
  baseline, experiment axes, and calibration ideas.

The Markdown preview is expected to render Mermaid and TeX/LaTex math.

## Key concepts already discussed

### Logic notation

`∀` means “for all”; `∃` means “there exists.” In a statement such as

```tex
\forall p\; Symptom(p, Toothache) \Rightarrow Disease(p, Cavity)
```

`p` is a person variable, not probability. The universe is the domain of
persons.

### Conditional probability

```tex
P(A\mid B) = \frac{P(A \land B)}{P(B)}
```

This is not a logical division operator. Conditioning restricts attention to
the B cases, then rescales their probability mass. `∧` corresponds to set
intersection. The product identity is generally:

```tex
P(A \land B) = P(A\mid B)P(B)
```

The simpler `P(A∧B)=P(A)P(B)` requires independence.

### Expected utility

A rational agent chooses the action with highest expected utility: evaluate
each possible outcome's utility, multiply it by that outcome's probability,
and sum. “Averaged over all possible outcomes” means this probability-weighted
average, not a simple equal-weight average.

## Game/domain facts used for the carrot model

- Default game: 24 turns/day, 30 days, starting $3000; NW 5×5 quadrant open.
- Carrot seed cost: $20. First yield day 2, max yield day 3, one-time crop.
- Planting day counts as an unwatered day; water on planting day to keep it
  alive overnight.
- Carrot bonus-watering window is days 2–3. Watering both days produces the
  baseline yield of 3 carrots; fertilizer can raise the cap to 4.
- Base carrot price is $35. Products in shed can be sold; carrots cannot be
  bought back from the market.
- Town center consumes one carrot per day.
- Future shop draws are random with replacement. Relevant carrot shops:
  `PET_CAFE` consumes 2 carrots every 4 turns; `FARMERS_MARKET` consumes 1
  carrot every 4 turns.
- Market state at time `t` is a sufficient public ledger state for its next
  transition. Opponent shed inventory is hidden, but visible opponent crops
  are evidence about likely future supply.

## Carrot decision-network design

Scope: one empty tile and one carrot-planting decision.

Observed evidence:

- day / remaining season
- market carrot inventory and price
- unlocked town shops
- visible opponent carrot tiles
- own seed/money/tile state

Chance nodes:

- future shop demand
- opponent future carrot supply
- future carrot sale-price category (LOW / NORMAL / HIGH)

Decision:

- `PLANT_CARROT` versus `PASS`

Utility:

```tex
U(PLANT) = E[yield \times sale\ price] - seed\ opportunity\ value
```

```tex
U(PASS)=0
```

The policy plants only when `U(PLANT) > U(PASS)`; equality means PASS.

## Current carrot agents

- Conveyor: always attempts the carrot cycle. It is a baseline, not a belief
  network.
- Decision: estimates price category from demand, visible opponent crop supply,
  and market inventory; it plants only when expected utility clears PASS.

The tunable assumptions live in the `CarrotParameters` dataclass in
`agents/carrot/shared.py`, including opponent-supply priors and low/high price
multipliers. Game facts (seed cost, crop timing, official price rules) should
not be tuned to make experiments look better.

## Evaluation plan

Use fixed seeds. Begin with conveyor vs `pass`, then conveyor vs `random`,
then decision vs conveyor on identical seeds. Track both final bank/reward and
probability calibration (for example Brier score after a predicted price
category resolves). Change one tunable assumption per experiment.
