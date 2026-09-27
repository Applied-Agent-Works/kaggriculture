namespace DecisionNetworkLab.Core;

/// <summary>
/// Runs the narrow, deterministic simulator used by the browser lab.
///
/// This is an educational one-crop lifecycle, not a replacement for the
/// Python Kaggriculture simulator. The Python simulator remains the source of
/// batch-match evidence; this class gives the learner immediate feedback.
/// </summary>
public static class InteractiveSimulator
{
    public static SimulationRun Run(
        DecisionNetworkDefinition network,
        DecisionScenario scenario,
        int seed)
    {
        var decision = DecisionEvaluator.Evaluate(network, scenario);
        var steps = new List<SimulationStep>
        {
            new(
                Order: 1,
                Label: "Scenario",
                Detail: $"Seed {seed}; starting on day {scenario.Day}.",
                NodeId: "day",
                Kind: DecisionTraceKind.Simulation),
            new(
                Order: 2,
                Label: "Decision",
                Detail: $"The model chose {decision.Recommendation}.",
                NodeId: "plant",
                Kind: DecisionTraceKind.Decision)
        };

        if (decision.Recommendation == "PASS")
        {
            steps.Add(new SimulationStep(
                Order: steps.Count + 1,
                Label: "Finished",
                Detail: "No crop was planted, so the simulator preserves the PASS utility.",
                NodeId: "utility",
                Kind: DecisionTraceKind.Utility));

            return new SimulationRun(
                Seed: seed,
                Decision: decision,
                Steps: steps,
                Outcome: "PASS",
                FinalReward: decision.PassUtility,
                ActualYield: 0m,
                ActualPrice: 0m);
        }

        var random = new Random(seed);
        var price = SamplePrice(decision.PriceBelief, random.NextDouble());
        var careSucceeded = random.NextDouble() < (double)Clamp(scenario.CareSuccessProbability, 0m, 1m);

        steps.Add(new SimulationStep(
            Order: steps.Count + 1,
            Label: "Price state",
            Detail: $"The seeded run sampled {price.Category} at {price.Value:0.##} coins per unit.",
            NodeId: "price",
            Kind: DecisionTraceKind.Belief));

        steps.Add(new SimulationStep(
            Order: steps.Count + 1,
            Label: "Care",
            Detail: careSucceeded
                ? "The crop received the planned care."
                : "Care failed, so the planned harvest was lost.",
            NodeId: "care",
            Kind: DecisionTraceKind.Simulation));

        if (!careSucceeded)
        {
            steps.Add(new SimulationStep(
                Order: steps.Count + 1,
                Label: "Finished",
                Detail: $"The seed opportunity cost was {network.SeedCost} coins.",
                NodeId: "utility",
                Kind: DecisionTraceKind.Utility));

            return new SimulationRun(
                Seed: seed,
                Decision: decision,
                Steps: steps,
                Outcome: "CARE FAILED",
                FinalReward: -network.SeedCost,
                ActualYield: 0m,
                ActualPrice: price.Value);
        }

        var actualYield = network.PlannedYield;
        var finalReward = actualYield * price.Value - network.SeedCost;

        steps.Add(new SimulationStep(
            Order: steps.Count + 1,
            Label: "Harvest",
            Detail: $"Harvested {actualYield} units on the planned day.",
            NodeId: "yield",
            Kind: DecisionTraceKind.Outcome));

        steps.Add(new SimulationStep(
            Order: steps.Count + 1,
            Label: "Finished",
            Detail: $"Sold the harvest for {finalReward:0.##} coins after seed cost.",
            NodeId: "utility",
            Kind: DecisionTraceKind.Utility));

        return new SimulationRun(
            Seed: seed,
            Decision: decision,
            Steps: steps,
            Outcome: "HARVESTED",
            FinalReward: finalReward,
            ActualYield: actualYield,
            ActualPrice: price.Value);
    }

    private static (string Category, decimal Value) SamplePrice(PriceBelief belief, double roll)
    {
        var lowLimit = (double)belief.LowProbability;
        var normalLimit = lowLimit + (double)belief.NormalProbability;

        if (roll < lowLimit)
        {
            return ("LOW", belief.LowPrice);
        }

        return roll < normalLimit
            ? ("NORMAL", belief.NormalPrice)
            : ("HIGH", belief.HighPrice);
    }

    private static decimal Clamp(decimal value, decimal minimum, decimal maximum)
    {
        return Math.Min(Math.Max(value, minimum), maximum);
    }
}
