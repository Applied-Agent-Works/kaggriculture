"""Build descriptive evidence from a completed Kaggriculture match."""

from __future__ import annotations

from collections import Counter
import json
from pathlib import Path
from typing import Any, Iterable


def _operation(action: Any) -> str | None:
    if isinstance(action, list) and action and isinstance(action[0], str):
        return action[0]
    if isinstance(action, str):
        return action
    return None


def _quantity(action: list[Any]) -> int:
    if len(action) < 3:
        return 1
    try:
        return max(0, int(action[2]))
    except (TypeError, ValueError):
        return 0


def _tile_at(farm: dict[str, Any], position: Any) -> Any:
    if not isinstance(position, list) or len(position) != 2:
        return None
    x, y = position
    tiles = farm.get("tiles")
    if not isinstance(tiles, list) or not isinstance(y, int) or not isinstance(x, int):
        return None
    if y < 0 or y >= len(tiles) or not isinstance(tiles[y], list):
        return None
    if x < 0 or x >= len(tiles[y]):
        return None
    return tiles[y][x]


def _farm_crop_counts(farm: dict[str, Any]) -> dict[str, int]:
    counts: Counter[str] = Counter()
    for row in farm.get("tiles", []):
        if not isinstance(row, list):
            continue
        for tile in row:
            if isinstance(tile, dict) and tile.get("kind") == "PLANT":
                crop = tile.get("crop")
                if isinstance(crop, str):
                    counts[crop] += 1
    return dict(sorted(counts.items()))


def _action_groups(
    observation: dict[str, Any], action: dict[str, Any]
) -> Iterable[tuple[Any, Any, str]]:
    farm = observation.get("farms", [{}])[observation.get("player", 0)]
    farmer_action = action.get("farmer")
    yield farmer_action, _tile_at(farm, farm.get("farmer")), "farmer"

    hands = farm.get("hands", [])
    hand_actions = action.get("hands", [])
    if not isinstance(hand_actions, list):
        hand_actions = []
    for index, hand_action in enumerate(hand_actions):
        position = hands[index] if index < len(hands) else None
        yield hand_action, _tile_at(farm, position), f"hand-{index}"


def _compact_evidence(observation: dict[str, Any]) -> dict[str, Any]:
    player = observation.get("player")
    farms = observation.get("farms", [])
    own_farm = farms[player] if isinstance(player, int) and player < len(farms) else {}
    opponent_index = 1 - player if isinstance(player, int) and player in (0, 1) else None
    opponent_farm = (
        farms[opponent_index]
        if opponent_index is not None and opponent_index < len(farms)
        else {}
    )
    return {
        "step": observation.get("step"),
        "day": observation.get("day"),
        "hour": observation.get("hour"),
        "own_money": own_farm.get("money"),
        "own_crop_counts": _farm_crop_counts(own_farm),
        "opponent_crop_counts": _farm_crop_counts(opponent_farm),
        "market": observation.get("market"),
        "town": observation.get("town"),
        "private": observation.get("private"),
    }


def _lifecycle_losses(
    steps: list[list[dict[str, Any]]], player: int
) -> dict[str, int]:
    losses: Counter[str] = Counter()
    for index in range(len(steps) - 1):
        before = steps[index][player].get("observation", {})
        after = steps[index + 1][player].get("observation", {})
        farms_before = before.get("farms", [])
        farms_after = after.get("farms", [])
        if player >= len(farms_before) or player >= len(farms_after):
            continue
        before_tiles = farms_before[player].get("tiles", [])
        after_tiles = farms_after[player].get("tiles", [])
        for y, row in enumerate(before_tiles):
            if not isinstance(row, list) or y >= len(after_tiles):
                continue
            for x, before_tile in enumerate(row):
                after_row = after_tiles[y]
                if not isinstance(after_row, list) or x >= len(after_row):
                    continue
                after_tile = after_row[x]
                if (
                    isinstance(before_tile, dict)
                    and before_tile.get("kind") == "PLANT"
                    and isinstance(after_tile, dict)
                    and after_tile.get("kind") == "WEED"
                ):
                    crop = before_tile.get("crop")
                    if isinstance(crop, str):
                        losses[crop] += 1
    return dict(sorted(losses.items()))


def _player_metrics(
    steps: list[list[dict[str, Any]]],
    final_state: dict[str, Any],
    player: int,
    trace_path: Path,
) -> dict[str, Any]:
    operation_counts: Counter[str] = Counter()
    plant_by_crop: Counter[str] = Counter()
    harvest_by_crop: Counter[str] = Counter()
    sale_orders: Counter[str] = Counter()
    purchase_orders: Counter[str] = Counter()
    quoted_sale_value = 0.0
    decision_counts: Counter[str] = Counter()
    trace_records: list[dict[str, Any]] = []

    for step_records in steps:
        if player >= len(step_records):
            continue
        record = step_records[player]
        observation = record.get("observation", {})
        action = record.get("action") or {}
        for unit_action, tile, _ in _action_groups(observation, action):
            operation = _operation(unit_action)
            if operation is None:
                continue
            operation_counts[operation] += 1
            if operation == "PLANT":
                decision_counts["plant"] += 1
                if isinstance(unit_action, list) and len(unit_action) > 1:
                    crop = unit_action[1]
                    if isinstance(crop, str):
                        plant_by_crop[crop] += 1
            elif operation == "PASS":
                decision_counts["pass"] += 1
            elif operation == "HARVEST":
                crop = tile.get("crop") if isinstance(tile, dict) else None
                if isinstance(crop, str):
                    harvest_by_crop[crop] += 1

        market_actions = action.get("market", [])
        if not isinstance(market_actions, list):
            market_actions = []
        prices = observation.get("market", {}).get("prices", {})
        for market_action in market_actions:
            operation = _operation(market_action)
            if not isinstance(market_action, list) or len(market_action) < 2:
                continue
            product = market_action[1]
            if not isinstance(product, str):
                continue
            quantity = _quantity(market_action)
            if operation == "SELL":
                sale_orders[product] += quantity
                quoted_sale_value += float(prices.get(product, 0)) * quantity
            elif operation in {"BUY_PRODUCT", "BUY_SEED", "BUY_ANIMAL"}:
                purchase_orders[product] += quantity

        trace_records.append(
            {
                "step": observation.get("step"),
                "action": action,
                "evidence": _compact_evidence(observation),
            }
        )

    trace_path.parent.mkdir(parents=True, exist_ok=True)
    with trace_path.open("w", encoding="utf-8") as handle:
        for record in trace_records:
            handle.write(json.dumps(record, separators=(",", ":")) + "\n")

    observation = final_state.get("observation", {})
    farms = observation.get("farms", [])
    final_farm = farms[player] if player < len(farms) else {}
    return {
        "reward": final_state.get("reward"),
        "status": final_state.get("status"),
        "operation_counts": dict(sorted(operation_counts.items())),
        "decision_counts": dict(sorted(decision_counts.items())),
        "plant_by_crop": dict(sorted(plant_by_crop.items())),
        "harvest_by_crop": dict(sorted(harvest_by_crop.items())),
        "lost_to_weed_by_crop": _lifecycle_losses(steps, player),
        "active_crops_at_end": _farm_crop_counts(final_farm),
        "requested_sales_by_product": dict(sorted(sale_orders.items())),
        "requested_purchases_by_product": dict(sorted(purchase_orders.items())),
        "quoted_sale_value": quoted_sale_value,
        "realized_sale_value": None,
        "realized_sale_price_available": False,
        "invalid_or_noop_actions": None,
        "trace_path": str(trace_path),
        "trace_records": len(trace_records),
    }


def write_match_evidence(
    steps: list[list[dict[str, Any]]],
    *,
    final_states: list[dict[str, Any]],
    report_directory: Path,
    agent: str,
    opponent: str,
    seed: int | None,
    requested_steps: int,
) -> dict[str, Any]:
    """Write per-player traces and a descriptive match report.

    The simulator exposes requested actions and observations, but not a
    reliable accepted-order audit for every market action. The report marks
    realized sale values and invalid/no-op counts as unavailable rather than
    presenting estimates as facts.
    """
    report_directory.mkdir(parents=True, exist_ok=True)
    players = {}
    for player in (0, 1):
        trace_path = report_directory / "decision-traces" / f"player-{player}.jsonl"
        metrics = _player_metrics(
            steps,
            final_states[player],
            player,
            trace_path,
        )
        metrics["trace_path"] = str(trace_path.relative_to(report_directory))
        players[str(player)] = metrics
    report = {
        "schema": "kaggriculture-match-evidence/v1",
        "agent": agent,
        "opponent": opponent,
        "seed": seed,
        "requested_steps": requested_steps,
        "completed_steps": len(steps),
        "players": players,
        "limitations": [
            "realized sale values are unavailable from the public simulator step records",
            "invalid and no-op action counts are unavailable from the public simulator step records",
            "sale quantities are requested order quantities, not accepted quantities",
        ],
    }
    report_path = report_directory / "report.json"
    report_path.write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")
    return report
