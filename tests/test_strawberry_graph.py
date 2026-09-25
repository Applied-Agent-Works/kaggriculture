from agents.ongoing_crop import OngoingCropParameters, expected_ongoing_crop_value, production_due
from agents.strawberry.shared import STRAWBERRY


def test_strawberry_uses_explicit_production_ages():
    assert STRAWBERRY.production_ages == (10, 12, 14, 16)
    assert STRAWBERRY.final_production_age == 16


def test_strawberry_produces_every_other_day():
    assert [age for age in range(1, 18) if production_due(STRAWBERRY, age)] == [10, 12, 14, 16]


def test_default_seed_opportunity_value_uses_strawberry_seed_cost():
    obs = {"day": 0, "market": {"prices": {"STRAWBERRY": 120}}}
    parameters = OngoingCropParameters(care_success_probability=1.0)
    assert expected_ongoing_crop_value(obs, STRAWBERRY, parameters) == 4 * 120 - 100
