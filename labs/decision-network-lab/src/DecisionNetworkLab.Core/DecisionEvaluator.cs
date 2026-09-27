namespace DecisionNetworkLab.Core;

/// <summary>
/// Performs the small, transparent calculation used by the first lab slice.
/// The UI calls this class; it does not reproduce these calculations itself.
/// </summary>
public static class DecisionEvaluator
{
    public static DecisionResult Evaluate(
        DecisionNetworkDefinition network,
        DecisionScenario scenario)
    {
        var careProbability = Clamp(scenario.CareSuccessProbability, 0m, 1m);
        var priceMultiplier = Math.Max(0m, scenario.FuturePriceMultiplier);
        var currentPrice = Math.Max(0m, scenario.CurrentPrice);
        var expectedYield = network.PlannedYield * careProbability;
        var canReachHarvest = scenario.Day + network.PlannedHarvestDay < network.SeasonDays;
        var priceBelief = CreatePriceBelief(network, scenario, currentPrice, priceMultiplier);

        var plantUtility = expectedYield * priceBelief.ExpectedPrice - network.SeedCost;
        var recommendation = canReachHarvest && plantUtility > scenario.PassUtility
            ? "PLANT"
            : "PASS";

        var explanation = BuildExplanation(
            network,
            scenario,
            canReachHarvest,
            expectedYield,
            priceBelief,
            plantUtility,
            recommendation);

        return new DecisionResult(
            Network: network,
            Scenario: scenario,
            CanReachHarvest: canReachHarvest,
            ExpectedYield: expectedYield,
            PriceBelief: priceBelief,
            PlantUtility: plantUtility,
            PassUtility: scenario.PassUtility,
            Recommendation: recommendation,
            Explanation: explanation);
    }

    private static PriceBelief CreatePriceBelief(
        DecisionNetworkDefinition network,
        DecisionScenario scenario,
        decimal currentPrice,
        decimal priceMultiplier)
    {
        if (network.PricingApproach == PricingApproach.PointEstimate)
        {
            var estimatedPrice = currentPrice * priceMultiplier;
            return new PriceBelief(0m, 1m, 0m, estimatedPrice, estimatedPrice, estimatedPrice);
        }

        // These are the first carrot teaching values from the existing
        // decision-network work. They are intentionally visible and coarse.
        var supplyProbability = scenario.VisibleOpponentMatureCrops switch
        {
            <= 0 => 0.05m,
            <= 3 => 0.30m,
            _ => 0.70m
        };

        var lowProbability = 0.15m + (supplyProbability * 0.65m);
        var highProbability = 0.05m
            + (Clamp(scenario.ActiveDemandSources, 0, 2) * 0.02m)
            - (supplyProbability * 0.30m);

        lowProbability = Clamp(lowProbability, 0m, 0.95m);
        highProbability = Clamp(highProbability, 0m, 0.95m);

        if (lowProbability + highProbability > 0.95m)
        {
            var total = lowProbability + highProbability;
            lowProbability = lowProbability / total * 0.95m;
            highProbability = highProbability / total * 0.95m;
        }

        var normalProbability = 1m - lowProbability - highProbability;
        var normalPrice = currentPrice * priceMultiplier;

        return new PriceBelief(
            LowProbability: lowProbability,
            NormalProbability: normalProbability,
            HighProbability: highProbability,
            LowPrice: normalPrice * 0.60m,
            NormalPrice: normalPrice,
            HighPrice: normalPrice * 1.20m);
    }

    private static IReadOnlyList<string> BuildExplanation(
        DecisionNetworkDefinition network,
        DecisionScenario scenario,
        bool canReachHarvest,
        decimal expectedYield,
        PriceBelief priceBelief,
        decimal plantUtility,
        string recommendation)
    {
        var explanation = new List<string>
        {
            $"The planned yield is {expectedYield:0.##} units after applying the {scenario.CareSuccessProbability:P0} care-success assumption.",
            $"The expected sale price is {priceBelief.ExpectedPrice:0.##} coins per unit.",
            $"Plant utility is {plantUtility:0.##} coins versus PASS utility of {scenario.PassUtility:0.##}."
        };

        if (!canReachHarvest)
        {
            explanation.Add($"The crop cannot reach its planned day-{network.PlannedHarvestDay} harvest before the {network.SeasonDays}-day season ends.");
        }

        if (network.PricingApproach == PricingApproach.CarrotBeliefModel)
        {
            explanation.Add($"The carrot belief assigns {priceBelief.LowProbability:P0} LOW, {priceBelief.NormalProbability:P0} NORMAL, and {priceBelief.HighProbability:P0} HIGH price probability.");
        }
        else
        {
            explanation.Add("This first version uses today's quote as a point estimate; it does not yet forecast a full price distribution.");
        }

        explanation.Add(recommendation == "PLANT"
            ? "The model recommends PLANT because the decision clears PASS and the harvest fits the horizon."
            : "The model recommends PASS because the utility or timing constraint does not support planting.");

        return explanation;
    }

    private static decimal Clamp(decimal value, decimal minimum, decimal maximum)
    {
        return Math.Min(Math.Max(value, minimum), maximum);
    }

    private static int Clamp(int value, int minimum, int maximum)
    {
        return Math.Min(Math.Max(value, minimum), maximum);
    }
}
