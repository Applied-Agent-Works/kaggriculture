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
}
