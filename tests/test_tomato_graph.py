from agents.ongoing_crop import OngoingCrop, OngoingCropParameters, production_due
from agents.tomato.shared import TOMATO


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
