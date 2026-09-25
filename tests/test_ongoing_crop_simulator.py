from kaggle_environments import make


def _harvest_days(agent_path: str, crop_name: str):
    env = make(
        "kaggriculture",
        configuration={"episodeSteps": 720, "seed": 42},
        debug=True,
    )
    env.run([agent_path, "pass"])
    player_steps = [step[0] for step in env.steps]
    harvest_steps = [
        index
        for index, state in enumerate(player_steps)
        if state["action"]["farmer"][0] == "HARVEST"
    ]
    harvest_days = [player_steps[index]["observation"]["day"] for index in harvest_steps]

    assert len(harvest_steps) >= 4
    assert all(
        player_steps[index]["observation"]["farms"][0]["tiles"][0][0]["crop"]
        == crop_name
        for index in harvest_steps[:4]
    )
    # The first harvest must leave the ongoing plant in place for later ticks.
    after_first = player_steps[harvest_steps[0] + 1]["observation"]
    assert after_first["farms"][0]["tiles"][0][0]["kind"] == "PLANT"
    # Once the four scheduled productions are complete, the game eventually
    # decays the crop into a weed rather than leaving it permanently active.
    assert player_steps[-1]["observation"]["farms"][0]["tiles"][0][0]["kind"] == "WEED"
    return harvest_days[:4]


def _strawberry_steps():
    env = make(
        "kaggriculture",
        configuration={"episodeSteps": 720, "seed": 42},
        debug=True,
    )
    env.run(["agents/strawberry/conveyor.py", "pass"])
    return [step[0] for step in env.steps]


def test_tomato_schedule_in_the_simulator():
    assert _harvest_days("agents/tomato/conveyor.py", "TOMATO") == [8, 9, 10, 11]


def test_strawberry_schedule_in_the_simulator():
    assert _harvest_days("agents/strawberry/conveyor.py", "STRAWBERRY") == [10, 12, 14, 16]


def test_strawberry_waters_daily_through_fourth_production():
    steps = _strawberry_steps()
    strawberry_water_days = {
        state["observation"]["day"]
        for state in steps
        if state["action"]["farmer"][0] == "WATER"
    }
    assert strawberry_water_days == set(range(17))

    harvest_states = [
        state
        for state in steps
        if state["action"]["farmer"][0] == "HARVEST"
    ]
    assert [state["observation"]["day"] for state in harvest_states[:4]] == [
        10,
        12,
        14,
        16,
    ]
    fourth_tile = harvest_states[3]["observation"]["farms"][0]["tiles"][0][0]
    assert fourth_tile["kind"] == "PLANT"
    # The fourth production is scheduled for age 16 and the engine marks the
    # first post-production decay step at the start of day 17.
    assert fourth_tile["max_lifespan_step"] == 17 * 24


def test_strawberry_trace_is_deterministic_for_fixed_seed():
    first = _strawberry_steps()
    second = _strawberry_steps()
    trace_fields = lambda steps: [
        (
            state["observation"]["step"],
            state["observation"]["day"],
            tuple(state["action"]["farmer"]),
            tuple(tuple(order) for order in state["action"]["market"]),
        )
        for state in steps
    ]
    assert trace_fields(first) == trace_fields(second)
