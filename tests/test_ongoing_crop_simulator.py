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


def test_tomato_schedule_in_the_simulator():
    assert _harvest_days("agents/tomato/conveyor.py", "TOMATO") == [8, 9, 10, 11]


def test_strawberry_schedule_in_the_simulator():
    assert _harvest_days("agents/strawberry/conveyor.py", "STRAWBERRY") == [10, 12, 14, 16]
