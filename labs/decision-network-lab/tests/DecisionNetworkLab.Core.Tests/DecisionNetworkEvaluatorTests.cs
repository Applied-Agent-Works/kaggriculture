using DecisionNetworkLab.Core;

namespace DecisionNetworkLab.Core.Tests;

/// <summary>
/// Tests the first three crop graphs without starting a browser or simulator.
/// These tests protect the small, teachable calculations used by the UI.
/// </summary>
[TestClass]
public sealed class DecisionNetworkEvaluatorTests
{
    [TestMethod]
    public void CatalogContainsTheThreeInitialOneTimeCropNetworks()
    {
        var ids = NetworkCatalog.All.Select(network => network.Id).ToArray();

        CollectionAssert.AreEquivalent(
            new[] { "carrot", "wheat", "melon" },
            ids);
    }

    [TestMethod]
    public void EveryCatalogNetworkHasACompleteGraph()
    {
        foreach (var network in NetworkCatalog.All)
        {
            var nodeIds = network.Nodes.Select(node => node.Id).ToHashSet();

            Assert.IsNotEmpty(network.Nodes, network.Name);
            Assert.IsNotEmpty(network.Edges, network.Name);
            Assert.IsNotEmpty(network.FixedFacts, network.Name);
            Assert.IsNotEmpty(network.Parameters, network.Name);
            Assert.IsFalse(
                network.Nodes.Any(node => string.IsNullOrWhiteSpace(node.PressurePoint)),
                $"Missing pressure-point guidance in {network.Name}.");

            foreach (var edge in network.Edges)
            {
                Assert.Contains(edge.FromNodeId, nodeIds, $"Unknown source node: {edge.FromNodeId}");
                Assert.Contains(edge.ToNodeId, nodeIds, $"Unknown target node: {edge.ToNodeId}");
            }
        }
    }

    [TestMethod]
    [DataRow("carrot", "PLANT")]
    [DataRow("wheat", "PLANT")]
    [DataRow("melon", "PLANT")]
    public void DefaultScenarioRecommendsPlanting(string networkId, string expectedRecommendation)
    {
        var network = NetworkCatalog.GetById(networkId);
        var result = DecisionEvaluator.Evaluate(network, DefaultScenario(network));

        Assert.AreEqual(expectedRecommendation, result.Recommendation);
        Assert.IsTrue(result.CanReachHarvest);
        Assert.AreEqual(0m, result.PassUtility);
        Assert.IsGreaterThan(result.PassUtility, result.PlantUtility);
    }

    [TestMethod]
    public void CarrotDefaultBeliefHasThreePriceCategoriesThatSumToOne()
    {
        var network = NetworkCatalog.GetById("carrot");
        var result = DecisionEvaluator.Evaluate(network, DefaultScenario(network));
        var belief = result.PriceBelief;

        Assert.AreEqual(0.1825m, belief.LowProbability);
        Assert.AreEqual(0.7825m, belief.NormalProbability);
        Assert.AreEqual(0.035m, belief.HighProbability);
        Assert.AreEqual(1m, belief.LowProbability + belief.NormalProbability + belief.HighProbability);
    }

    [TestMethod]
    public void MoreVisibleMatureOpponentCarrotsLowerTheCarrotPriceBelief()
    {
        var network = NetworkCatalog.GetById("carrot");
        var lowSupplyEvidence = DefaultScenario(network);
        var highSupplyEvidence = DefaultScenario(network);
        highSupplyEvidence.VisibleOpponentMatureCrops = 5;

        var lowSupplyResult = DecisionEvaluator.Evaluate(network, lowSupplyEvidence);
        var highSupplyResult = DecisionEvaluator.Evaluate(network, highSupplyEvidence);

        Assert.IsLessThan(
            lowSupplyResult.PriceBelief.ExpectedPrice,
            highSupplyResult.PriceBelief.ExpectedPrice);
        Assert.IsGreaterThan(
            lowSupplyResult.PriceBelief.LowProbability,
            highSupplyResult.PriceBelief.LowProbability);
    }

    [TestMethod]
    public void WheatPointEstimateUsesTheScenarioMultiplier()
    {
        var network = NetworkCatalog.GetById("wheat");
        var scenario = DefaultScenario(network);
        scenario.FuturePriceMultiplier = 1.10m;

        var result = DecisionEvaluator.Evaluate(network, scenario);

        Assert.AreEqual(27.5m, result.PriceBelief.ExpectedPrice);
        Assert.AreEqual(100m, result.PlantUtility);
    }

    [TestMethod]
    public void LowCareSuccessCanMakePlantingWheatUnattractive()
    {
        var network = NetworkCatalog.GetById("wheat");
        var scenario = DefaultScenario(network);
        scenario.CareSuccessProbability = 0m;

        var result = DecisionEvaluator.Evaluate(network, scenario);

        Assert.AreEqual(0m, result.ExpectedYield);
        Assert.AreEqual("PASS", result.Recommendation);
        Assert.IsLessThan(result.PassUtility, result.PlantUtility);
    }

    [TestMethod]
    public void MelonPassesWhenThereIsNoTimeToReachItsHarvest()
    {
        var network = NetworkCatalog.GetById("melon");
        var scenario = DefaultScenario(network);
        scenario.Day = 20;

        var result = DecisionEvaluator.Evaluate(network, scenario);

        Assert.IsFalse(result.CanReachHarvest);
        Assert.AreEqual("PASS", result.Recommendation);
        StringAssert.Contains(result.Explanation.Last(), "PASS");
    }

    [TestMethod]
    public void EvaluationExposesActionsAndAcausalTraceForTheUi()
    {
        var network = NetworkCatalog.GetById("carrot");
        var result = DecisionEvaluator.Evaluate(network, DefaultScenario(network));

        Assert.HasCount(2, result.Actions);
        Assert.AreEqual("PLANT", result.Actions.Single(action => action.IsRecommended).Action);
        Assert.IsNotEmpty(result.Trace);
        Assert.AreEqual("price", result.Trace.Single(step => step.NodeId == "price").NodeId);
        Assert.AreEqual("utility", result.Trace.Last().NodeId);
    }

    [TestMethod]
    public void InteractiveSimulationIsRepeatableForTheSameSeed()
    {
        var network = NetworkCatalog.GetById("carrot");
        var scenario = DefaultScenario(network);

        var first = InteractiveSimulator.Run(network, scenario, seed: 42);
        var second = InteractiveSimulator.Run(network, scenario, seed: 42);

        Assert.AreEqual(first.Outcome, second.Outcome);
        Assert.AreEqual(first.FinalReward, second.FinalReward);
        CollectionAssert.AreEqual(first.Steps.ToArray(), second.Steps.ToArray());
    }

    [TestMethod]
    public void InteractiveSimulationFinishesWithPassWhenTheDecisionCannotReachHarvest()
    {
        var network = NetworkCatalog.GetById("melon");
        var scenario = DefaultScenario(network);
        scenario.Day = 20;

        var run = InteractiveSimulator.Run(network, scenario, seed: 7);

        Assert.AreEqual("PASS", run.Outcome);
        Assert.AreEqual(scenario.PassUtility, run.FinalReward);
        Assert.IsTrue(run.Steps.Any(step => step.Label == "Finished"));
    }

    [TestMethod]
    public void EvidencePackageReaderBuildsPairedEvidenceFromJsonArtifacts()
    {
        var files = EvidencePackageFiles();

        var evidence = EvidencePackageReader.Read(files);

        Assert.AreEqual("example-v1", evidence.ExperimentId);
        Assert.AreEqual(1, evidence.PairedRuns);
        Assert.AreEqual(12m, evidence.BaselineMeanReward);
        Assert.AreEqual(15m, evidence.CandidateMeanReward);
        Assert.AreEqual(3m, evidence.MeanDelta);
        Assert.AreEqual("abc123", evidence.SourceRevision);
        Assert.AreEqual("candidate", evidence.CandidatePolicy);
        Assert.AreEqual(15m, evidence.Pairs[0].CandidateReward);
        StringAssert.Contains(evidence.Pairs[0].Trace.SourceLabel, "not loaded");
    }

    [TestMethod]
    public void EvidencePackageReaderRejectsAnUnsupportedSchema()
    {
        var files = EvidencePackageFiles().ToDictionary(pair => pair.Key, pair => pair.Value.Replace("\"1.0\"", "\"9.0\""));

        var exception = Assert.Throws<EvidencePackageReadException>(() => EvidencePackageReader.Read(files));

        StringAssert.Contains(exception.Message, "Unsupported evidence schema");
    }

    [TestMethod]
    public void EvidencePackageReaderRejectsAnIncompletePair()
    {
        var files = EvidencePackageFiles();
        files["manifest.json"] = files["manifest.json"].Replace("\"candidate\"", "\"missing-candidate\"");

        var exception = Assert.Throws<EvidencePackageReadException>(() => EvidencePackageReader.Read(files));

        StringAssert.Contains(exception.Message, "manifest.policies.candidate");
    }

    [TestMethod]
    public void MatchHistoryReaderReadsTheRecordedMatchCatalog()
    {
        var matches = MatchHistoryReader.Read("""
        {
          "matches": [
            {
              "id": "match-1",
              "createdAt": "2026-09-28T02:10:32+00:00",
              "agent": "Carrot Decision v1",
              "opponent": "Pass",
              "days": 30,
              "steps": 720,
              "seed": 1044665101,
              "rewards": [9726.0, 6093.0],
              "statuses": ["DONE", "DONE"],
              "winner": "Player 1",
              "source": "local",
              "path": "match-1.json"
            }
          ]
        }
        """);

        Assert.HasCount(1, matches);
        Assert.AreEqual("Carrot Decision v1", matches[0].Agent);
        Assert.AreEqual("Pass", matches[0].Opponent);
        Assert.AreEqual("Player 1", matches[0].Winner);
        Assert.AreEqual("local", matches[0].Source);
        Assert.AreEqual(1044665101, matches[0].Seed);
        Assert.AreEqual(720, matches[0].Steps);
        Assert.AreEqual("match-1.json", matches[0].RecordingPath);
    }

    [TestMethod]
    public void AgentMetadataReaderExtractsDescriptionsAndTraitsWithoutExecutingPython()
    {
        var metadata = AgentMetadataReader.Read("""
        def agent_specs():
            specs = {
                "market_broker": {
                    "label": "New · Market Broker (approximate)",
                    "description": "Mixed economic policy using prices.",
                    "traits": ["mixed crops", "price thresholds"],
                    "approximate": True,
                    "experiment_round": "R2",
                    "source_kind": "local_archetype",
                    "source_path": "opponent-experiment/market_broker.py",
                },
            }
        """);

        Assert.HasCount(1, metadata);
        Assert.AreEqual("market_broker", metadata[0].Id);
        Assert.AreEqual("Mixed economic policy using prices.", metadata[0].Description);
        CollectionAssert.AreEqual(new[] { "mixed crops", "price thresholds" }, metadata[0].Traits.ToArray());
        Assert.IsTrue(metadata[0].Approximate);
        Assert.AreEqual("R2", metadata[0].ExperimentRound);
        Assert.AreEqual("opponent-experiment/market_broker.py", metadata[0].SourcePath);
    }

    [TestMethod]
    public void RecordedMatchReaderGroupsTurnsIntoDays()
    {
        var summary = new RecordedMatchSummary(
            "match-1", "2026-09-28", "Agent", "Opponent", 2, 2, 42,
            new[] { 10m, 8m }, new[] { "DONE", "DONE" }, "Player 1", "local", "match-1.json");

        var details = RecordedMatchReader.Read("""
        {
          "steps": [
            [{"action":{"farmer":["PASS"]},"reward":0,"status":"ACTIVE","observation":{"day":0,"hour":0,"farms":[{"money":3000},{"money":3000}],"market":{"prices":{"CARROT":35}}}}],
            [{"action":{"farmer":["HARVEST"]},"reward":10,"status":"DONE","observation":{"day":1,"hour":0,"farms":[{"money":3010},{"money":3000}],"market":{"prices":{"CARROT":36}}}}]
          ]
        }
        """, summary);

        Assert.HasCount(2, details.Days);
        Assert.HasCount(1, details.Days[0].Turns);
        StringAssert.Contains(details.Days[1].Turns[0].Players[0].Action, "HARVEST");
        StringAssert.Contains(details.Days[1].Turns[0].Players[0].ObservationSummary, "farm money=3010");
    }

    [TestMethod]
    public void ReplayRecordingReaderReadsReplayMetadata()
    {
        var directory = Path.Combine(Path.GetTempPath(), "decision-network-replay-test-" + Guid.NewGuid());
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "sample.json");

        try
        {
            File.WriteAllText(path, """
            {
              "configuration": {"turnsPerDay": 24, "seed": null},
              "info": {"seed": 42},
              "steps": [[], []],
              "rewards": [12, 10],
              "statuses": ["DONE", "DONE"]
            }
            """);

            var replay = ReplayRecordingReader.ReadSummary(path);

            Assert.AreEqual("sample.json", replay.FileName);
            Assert.AreEqual(42, replay.Seed);
            Assert.AreEqual(2, replay.Steps);
            Assert.AreEqual("DONE", replay.Statuses[0]);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static DecisionScenario DefaultScenario(DecisionNetworkDefinition network)
    {
        return new DecisionScenario
        {
            CurrentPrice = network.CropCode switch
            {
                "CARROT" => 35m,
                "WHEAT" => 25m,
                "MELON" => 250m,
                _ => 1m
            }
        };
    }

    private static Dictionary<string, string> EvidencePackageFiles()
    {
        return new Dictionary<string, string>
        {
            ["manifest.json"] = """
            {
              "schema_version": "1.0",
              "experiment_id": "example-v1",
              "purpose": "Example comparison",
              "source": { "git_revision": "abc123" },
              "controls": { "seeds": [42], "opponent": "pass", "episode_steps": 24, "game": "kaggriculture" },
              "policies": { "baseline": "baseline", "candidate": "candidate" },
              "artifacts": { "decision_traces": ["decision-traces/candidate-seed-42-seat-0.json.gz"] }
            }
            """,
            ["comparison-summary.json"] = """
            {
              "mean_delta": 3.0,
              "pairs": [{ "seed": 42, "decision_player": 0 }]
            }
            """,
            ["run-summaries.jsonl"] = """
            {"policy":"baseline","seed":42,"decision_player":0,"policy_reward":12.0}
            {"policy":"candidate","seed":42,"decision_player":0,"policy_reward":15.0}
            """
        };
    }
}
