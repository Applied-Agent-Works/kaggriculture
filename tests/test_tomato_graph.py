from agents.ongoing_crop import (
    OngoingCrop,
    OngoingCropParameters,
    decision_agent,
    production_due,
)
from agents.tomato.shared import TOMATO, tomato_decision_agent


def test_tomato_uses_explicit_production_ages():
    assert TOMATO.production_ages == (8, 9, 10, 11)
    assert TOMATO.final_production_age == 11


def test_production_schedule_is_not_an_interval_guess():
    assert [age for age in range(1, 17) if production_due(TOMATO, age)] == [8, 9, 10, 11]


def test_expected_value_uses_four_production_events():
    obs = {"day": 0, "market": {"prices": {"TOMATO": 60}}}
    parameters = OngoingCropParameters(care_success_probability=1.0)
    from agents.ongoing_crop import expected_ongoing_crop_value

    assert expected_ongoing_crop_value(obs, TOMATO, parameters) == 4 * 60 - 50


def test_decision_passes_custom_horizon_to_action_helpers():
    obs = {
        "day": 25,
        "player": 0,
        "farms": [{
            "money": 100,
            "farmer": [0, 0],
            "tiles": [[None]],
        }],
        "private": {"seeds": {}, "shed": {}},
        "market": {"prices": {"TOMATO": 60}},
    }
    parameters = OngoingCropParameters(season_days=50)
    action = decision_agent(obs, TOMATO, parameters)
    assert action["market"] == [["BUY_SEED", "TOMATO", 1]]
    # The market order delivers the seed for a later turn; planting cannot
    # consume it before that order has been processed.
    assert action["farmer"] == ["PASS"]


def test_tomato_decision_has_explicit_plant_and_pass_branches():
    base_obs = {
        "day": 0,
        "player": 0,
        "farms": [{
            "money": 100,
            "farmer": [0, 0],
            "tiles": [[None]],
        }],
        "private": {"seeds": {"TOMATO": 1}, "shed": {}},
        "market": {"prices": {"TOMATO": 60}},
    }

    plant_action = decision_agent(base_obs, TOMATO)
    assert plant_action["farmer"] == ["PLANT", "TOMATO"]

    cautious = OngoingCropParameters(future_price_multiplier=0.1)
    pass_action = decision_agent(base_obs, TOMATO, cautious)
    assert pass_action["farmer"] == ["PASS"]
    assert pass_action["market"] == []


def test_tomato_adapter_passes_explicit_parameters():
    obs = {
        "day": 25,
        "player": 0,
        "farms": [{
            "money": 100,
            "farmer": [0, 0],
            "tiles": [[None]],
        }],
        "private": {"seeds": {}, "shed": {}},
        "market": {"prices": {"TOMATO": 60}},
    }
    parameters = OngoingCropParameters(season_days=50)

    action = tomato_decision_agent(obs, parameters)

    assert action["market"] == [["BUY_SEED", "TOMATO", 1]]
    # Market orders are processed after the farmer action, so planting waits
    # for the next turn even though the custom horizon permits investment.
    assert action["farmer"] == ["PASS"]
