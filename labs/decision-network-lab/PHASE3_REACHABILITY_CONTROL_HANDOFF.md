# Phase 3 reachability-control handoff

The first melon comparison suite is complete, but its result is confounded:
all five atomic candidates add `can_reach_planned_harvest` before planting,
while `melon_decision_v1` does not.

## Required external agent

Add one registered canonical-feed agent with a stable ID such as
`melon_reachability_control_v1`.

Its behavior should be:

1. Use the same shed-hugger executor as `melon_decision_v1`.
2. Use the same point-price planting rule as `melon_decision_v1`.
3. Add only the `can_reach_planned_harvest` guard.
4. Do not add opponent-supply, horizon-margin, glut-risk, conservative-margin,
   or care-capacity logic.

The agent metadata should identify it as an exact control for the shared
reachability guard, not as a validated improvement.

## Follow-up suite

After the external viewer exposes the new stable ID, rerun the same matched
design used in [`PHASE3_MELON_BASELINE_ANALYSIS.md`](./PHASE3_MELON_BASELINE_ANALYSIS.md):

- 30 simulated days;
- seeds 42, 43, and 44;
- both player positions;
- baseline versus reachability control;
- each atomic variant versus reachability control.

Do not promote an atomic variant until its result is separated from the common
reachability effect.

## Boundary

This lab does not copy or execute the external Python agent source. It will
consume the registered stable ID and saved match evidence through the local
launcher on port 8767.
