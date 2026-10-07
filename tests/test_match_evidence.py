"""Tests for descriptive evidence emitted by the local match runner."""

import json
from pathlib import Path

from tools.match_evidence import write_match_evidence


def _record(step, action, tile=None, money=3000.0):
    farm = {
        "money": money,
        "farmer": [0, 0],
        "hands": [],
        "tiles": [[tile]],
    }
    observation = {
        "step": step,
        "day": 0,
        "hour": step,
        "player": 0,
        "farms": [farm, {"money": 3000.0, "tiles": [[None]], "farmer": [0, 0], "hands": []}],
        "market": {"prices": {"CARROT": 35}, "inventory": {"CARROT": 10000}},
        "town": {"unlocked_shops": []},
        "private": {"shed": {}, "seeds": {}},
    }
    return {
        "action": action,
        "reward": money,
        "status": "ACTIVE",
        "observation": observation,
    }


def test_evidence_records_decisions_lifecycle_and_trace(tmp_path: Path):
    plant = {"kind": "PLANT", "crop": "CARROT"}
    steps = [
        [_record(0, {"farmer": ["PLANT", "CARROT"], "hands": [], "market": []}), _record(0, {"farmer": ["PASS"], "hands": [], "market": []})],
        [_record(1, {"farmer": ["WATER"], "hands": [], "market": [["SELL", "CARROT", 2]]}, plant, 3040.0), _record(1, {"farmer": ["PASS"], "hands": [], "market": []})],
        [_record(2, {"farmer": ["PASS"], "hands": [], "market": []}, {"kind": "WEED"}), _record(2, {"farmer": ["PASS"], "hands": [], "market": []})],
    ]
    final_states = [steps[-1][0], steps[-1][1]]
    report = write_match_evidence(
        steps,
        final_states=final_states,
        report_directory=tmp_path,
        agent="baseline.py",
        opponent="pass",
        seed=42,
        requested_steps=3,
    )

    metrics = report["players"]["0"]
    assert metrics["decision_counts"] == {"pass": 1, "plant": 1}
    assert metrics["plant_by_crop"] == {"CARROT": 1}
    assert metrics["lost_to_weed_by_crop"] == {"CARROT": 1}
    assert metrics["requested_sales_by_product"] == {"CARROT": 2}
    assert metrics["realized_sale_value"] is None
    assert metrics["invalid_or_noop_actions"] is None
    trace_path = tmp_path / metrics["trace_path"]
    assert trace_path.is_file()
    assert len(trace_path.read_text(encoding="utf-8").splitlines()) == 3


def test_evidence_report_is_json_serializable(tmp_path: Path):
    record = _record(0, {"farmer": ["PASS"], "hands": [], "market": []})
    report = write_match_evidence(
        [[record, record]],
        final_states=[record, record],
        report_directory=tmp_path,
        agent="a.py",
        opponent="b.py",
        seed=1,
        requested_steps=1,
    )
    json.dumps(report)
