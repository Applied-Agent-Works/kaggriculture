from agents.ongoing_crop import (
    OngoingCropParameters,
    expected_ongoing_crop_value,
    production_due,
)
from agents.strawberry.shared import STRAWBERRY, strawberry_decision_agent


def test_strawberry_uses_explicit_production_ages():
    assert STRAWBERRY.production_ages == (10, 12, 14, 16)
    assert STRAWBERRY.final_production_age == 16


def test_strawberry_produces_every_other_day():
    assert [age for age in range(1, 18) if production_due(STRAWBERRY, age)] == [10, 12, 14, 16]


def test_default_seed_opportunity_value_uses_strawberry_seed_cost():
    obs = {"day": 0, "market": {"prices": {"STRAWBERRY": 120}}}
    parameters = OngoingCropParameters(care_success_probability=1.0)
    assert expected_ongoing_crop_value(obs, STRAWBERRY, parameters) == 4 * 120 - 100


def test_strawberry_adapter_passes_explicit_parameters_to_shared_policy():
    obs = {
        "day": 25,
        "player": 0,
        "farms": [{
            "money": 100,
            "farmer": [0, 0],
            "tiles": [[None]],
        }],
        "private": {"seeds": {}, "shed": {}},
        "market": {"prices": {"STRAWBERRY": 120}},
    }
    parameters = OngoingCropParameters(season_days=50)
    action = strawberry_decision_agent(obs, parameters)

    assert action["market"] == [["BUY_SEED", "STRAWBERRY", 1]]
    # The seed order is processed after the farmer action, so this turn cannot
    # plant yet.  The custom horizon is nevertheless honored by the adapter.
    assert action["farmer"] == ["PASS"]


def test_zero_care_probability_selects_pass():
    obs = {
        "day": 0,
        "player": 0,
        "farms": [{
            "money": 100,
            "farmer": [0, 0],
            "tiles": [[None]],
        }],
        "private": {"seeds": {"STRAWBERRY": 1}, "shed": {}},
        "market": {"prices": {"STRAWBERRY": 120}},
    }
    parameters = OngoingCropParameters(care_success_probability=0.0)

    action = strawberry_decision_agent(obs, parameters)

    assert action["farmer"] == ["PASS"]
    assert action["market"] == []
