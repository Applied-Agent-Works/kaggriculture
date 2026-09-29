# Phase 3 melon baseline analysis

Date: 2026-09-28

This is a read-only analysis of the agents exposed by the Kaggriculture local
viewer at `http://127.0.0.1:8767`.

## Agent library

| Stable ID | Role | Approximate? |
|---|---|---:|
| `melon_decision_v1` | Standalone Melon/Watermelon point-price PLANT/PASS baseline | No |
| `melon_parity_control_v1` | Standalone parity control for Melon V1 | No |
| `melon_atomic_reactive_supply` | Discounts investment when near-term opponent melons are visible | Yes |
| `melon_atomic_horizon_margin` | Keeps a two-day harvest safety margin | Yes |
| `melon_atomic_glut_risk` | Discounts exposure to active and visible melon supply | Yes |
| `melon_atomic_conservative_margin` | Requires revenue above twice seed opportunity value | Yes |
| `melon_atomic_care_gate` | Gates planting on approximate care capacity | Yes |
| `portfolio_v1` | Multi-crop Carrot/Wheat/Melon portfolio with shared care scheduling | No |

`melon_decision_v1` is the canonical melon baseline. The atomic agents are
controlled hypotheses, not validated replacements.

## Existing saved evidence

The viewer currently exposes 28 records whose display names mention melon.
The most useful direct comparisons are:

| Agent | Opponent | Runs | Mean agent-minus-opponent | Result |
|---|---|---:|---:|---|
| `melon_decision_v1` | `melon_parity_control_v1` | 2 | -40 | 0 wins, 1 loss, 1 draw |
| `portfolio_v1` | `melon_decision_v1` | 2 | +3,011 | 2 wins |
| `melon_decision_v1` | `melon_atomic_care_gate` | 1 | +80 | 1 win |

The parity result is consistent with a control relationship on seed 42, where
the recorded result is a draw. The second parity comparison differs by 80
coins, so parity should be tested with a larger matched seed and seat suite
before we call the implementations equivalent.

The portfolio result is useful as an integration benchmark, but it is not a
pure melon-policy comparison: it can choose Carrot, Wheat, or Melon and shares
care capacity across crops.

There is not enough direct saved evidence to evaluate the other four atomic
variants. One run is not enough to call the care-gate hypothesis improved.

## Controlled suite

The fixed suite was:

- baseline: `melon_decision_v1`;
- control: `melon_parity_control_v1`;
- candidates: the five atomic variants;
- seeds: 42, 43, and 44;
- both player positions;
- 30 simulated days and 720 steps per match.

The suite completed on 2026-09-28 after the launcher was relaunched. It
produced 36 saved matches, all with `DONE` status and no launcher errors.

Results below use the candidate's reward minus the baseline's reward. The
candidate perspective combines both player positions.

| Candidate | Runs | Mean delta | Wins | Losses | Draws |
|---|---:|---:|---:|---:|---:|
| `melon_parity_control_v1` | 6 | 0.00 | 2 | 2 | 2 |
| `melon_atomic_reactive_supply` | 6 | +14,237.67 | 6 | 0 | 0 |
| `melon_atomic_horizon_margin` | 6 | +14,371.00 | 6 | 0 | 0 |
| `melon_atomic_glut_risk` | 6 | +14,237.67 | 6 | 0 | 0 |
| `melon_atomic_conservative_margin` | 6 | +14,237.67 | 6 | 0 | 0 |
| `melon_atomic_care_gate` | 6 | +14,237.67 | 6 | 0 | 0 |

The one-day smoke match used to verify the relaunched server also completed
successfully: `melon_decision_v1` versus `melon_parity_control_v1`, seed 42,
24 steps, both players `DONE`, and equal reward of 1,400.

## Interpretation

The parity control is close but not perfectly identical: the six matched runs
contain two wins, two losses, and two draws from the control's perspective,
with differences no larger than 80 coins. That is evidence of near-parity,
not proof of exact action equivalence.

The five atomic variants all beat `melon_decision_v1` by roughly 14,000 coins.
This result should not yet be interpreted as evidence that one named atomic
hypothesis is superior. All five variants share a structural change that the
baseline does not: their common `_base_investment` rule requires
`can_reach_planned_harvest` before planting. The baseline calls the shared
shed-hugger decision function without that reachability guard.

Therefore, the current result identifies a strong shared reachability effect,
not a trustworthy ranking of opponent-supply, horizon, glut, margin, or care
hypotheses.

## Remaining Phase 3 experiment

The next controlled comparison should add a reachability-only control that
uses the baseline price rule plus the same `can_reach_planned_harvest` guard.
Only after that control is measured should the five atomic variants be ranked
against it. That agent belongs in the external canonical feed/runtime; this
lab should consume its registered ID and saved evidence rather than copy its
Python source.

The earlier launcher failure was an external runtime/cache issue, not model
evidence. It was cleared by relaunching the launcher.

For reference, the earlier failed attempt reported:

```text
cannot import name 'shed_hugger_decision_agent' from crop_agent_support
```

This error was not evidence about the melon models, and this lab did not repair
the sibling runtime implicitly.
