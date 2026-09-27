namespace DecisionNetworkLab.Core;

/// <summary>
/// Provides the three crop networks included in the first lab slice.
/// </summary>
public static class NetworkCatalog
{
    public static IReadOnlyList<DecisionNetworkDefinition> All { get; } =
        new[]
        {
            CreateCarrot(),
            CreateWheat(),
            CreateMelon()
        };

    public static DecisionNetworkDefinition GetById(string id)
    {
        return All.First(network =>
            string.Equals(network.Id, id, StringComparison.OrdinalIgnoreCase));
    }

    private static DecisionNetworkDefinition CreateCarrot()
    {
        return new DecisionNetworkDefinition(
            Id: "carrot",
            Name: "Carrot",
            CropCode: "CARROT",
            Summary: "The richest first example: visible opponent supply and future demand shape a coarse sale-price belief.",
            SeedCost: 20,
            PlannedYield: 3,
            PlannedHarvestDay: 3,
            SeasonDays: 30,
            PricingApproach: PricingApproach.CarrotBeliefModel,
            Nodes: new[]
            {
                new NetworkNode("day", "Day and remaining season", NetworkNodeKind.Evidence, "How much time remains for planting, care, harvest, and sale?", "Hard timing gate: once the harvest no longer fits, planting becomes unavailable."),
                new NetworkNode("market", "Current market quote", NetworkNodeKind.Evidence, "The public carrot price at the time of the decision.", "Price anchor: the quote shifts the sale-price belief and utility."),
                new NetworkNode("opponent", "Visible opponent crops", NetworkNodeKind.Evidence, "Public crop evidence that may predict future supply.", "Supply evidence: more mature opponent crops increase LOW-price pressure."),
                new NetworkNode("demand", "Active and future demand", NetworkNodeKind.Chance, "Known shops and a small future-demand assumption.", "Small demand lever: the current coarse model adjusts HIGH-price pressure modestly."),
                new NetworkNode("supply", "Opponent supply before sale", NetworkNodeKind.Chance, "A belief about whether the opponent will add supply.", "Belief conversion point: evidence becomes a price-pressure probability here."),
                new NetworkNode("price", "Sale-price belief", NetworkNodeKind.Chance, "LOW, NORMAL, and HIGH price categories.", "High-leverage bridge: expected price multiplies expected yield before utility."),
                new NetworkNode("plant", "Plant carrot or PASS", NetworkNodeKind.Decision, "The explicit action comparison.", "Decision boundary: PLANT must beat PASS and satisfy the harvest horizon."),
                new NetworkNode("yield", "Expected cared-for yield", NetworkNodeKind.Outcome, "The expected harvest after care risk is considered.", "Hidden pressure point: care success changes yield value even though carrot lacks a care node in this graph."),
                new NetworkNode("utility", "Expected utility", NetworkNodeKind.Utility, "Expected sale value minus seed opportunity value.", "Final economic pressure point: small changes here can flip PLANT/PASS.")
            },
            Edges: new[]
            {
                new NetworkEdge("day", "demand", "Time limits future demand opportunities."),
                new NetworkEdge("opponent", "supply", "Visible crops are evidence about future supply."),
                new NetworkEdge("market", "price", "The current quote anchors the forecast."),
                new NetworkEdge("demand", "price", "Demand can support a higher sale price."),
                new NetworkEdge("supply", "price", "Supply can create a lower sale price."),
                new NetworkEdge("plant", "yield", "Planting creates the possible crop outcome."),
                new NetworkEdge("yield", "utility", "Yield contributes sale value."),
                new NetworkEdge("price", "utility", "Price determines expected revenue."),
                new NetworkEdge("plant", "utility", "The decision consumes the seed opportunity.")
            },
            FixedFacts: new[]
            {
                new FixedFact("Seed opportunity value", "20 coins"),
                new FixedFact("Planned yield", "3 carrots"),
                new FixedFact("Planned harvest", "Day 3"),
                new FixedFact("Crop pattern", "One-time harvest")
            },
            Parameters: new[]
            {
                new ParameterDefinition("Supply belief", "How strongly visible mature opponent carrots predict a future glut."),
                new ParameterDefinition("Future price multiplier", "A simple policy preference applied after the coarse belief model."),
                new ParameterDefinition("Care success probability", "The chance that the planned watering and harvest work succeeds."),
                new ParameterDefinition("PASS utility", "The value assigned to leaving the decision uncommitted.")
            });
    }

    private static DecisionNetworkDefinition CreateWheat()
    {
        return CreateSimpleOneTimeCrop(
            id: "wheat",
            name: "Wheat",
            cropCode: "WHEAT",
            summary: "A short one-time crop using today's wheat quote as a transparent first price estimate.",
            seedCost: 10,
            plannedYield: 4,
            plannedHarvestDay: 4,
            basePrice: 25,
            shopDescription: "Future wheat-demand shops are documented but not yet probabilistically modeled.");
    }

    private static DecisionNetworkDefinition CreateMelon()
    {
        return CreateSimpleOneTimeCrop(
            id: "melon",
            name: "Melon / watermelon",
            cropCode: "MELON",
            summary: "A long-wait, high-price one-time crop using a point estimate before a fuller glut model is added.",
            seedCost: 80,
            plannedYield: 6,
            plannedHarvestDay: 10,
            basePrice: 250,
            shopDescription: "No shop currently demands melon; future player supply is the main uncertainty.");
    }

    private static DecisionNetworkDefinition CreateSimpleOneTimeCrop(
        string id,
        string name,
        string cropCode,
        string summary,
        int seedCost,
        int plannedYield,
        int plannedHarvestDay,
        int basePrice,
        string shopDescription)
    {
        return new DecisionNetworkDefinition(
            Id: id,
            Name: name,
            CropCode: cropCode,
            Summary: summary,
            SeedCost: seedCost,
            PlannedYield: plannedYield,
            PlannedHarvestDay: plannedHarvestDay,
            SeasonDays: 30,
            PricingApproach: PricingApproach.PointEstimate,
            Nodes: new[]
            {
                new NetworkNode("day", "Day and remaining season", NetworkNodeKind.Evidence, "How much time remains before the season ends?", "Hard timing gate: late planting can make the harvest unreachable."),
                new NetworkNode("market", "Current market quote", NetworkNodeKind.Evidence, "Today's public crop price is the first estimate of the sale quote.", "Price anchor: today's quote directly sets the first sale-price estimate."),
                new NetworkNode("care", "Care feasibility", NetworkNodeKind.Evidence, "The learner states how likely the planned care is to succeed.", "Yield lever: lower care success reduces expected harvest value."),
                new NetworkNode("price", "Future sale-price estimate", NetworkNodeKind.Chance, shopDescription, "Model simplification: this version uses a point estimate rather than a price distribution."),
                new NetworkNode("plant", $"Plant {name} or PASS", NetworkNodeKind.Decision, "The explicit action comparison.", "Decision boundary: planting wins only when utility beats PASS and timing is feasible."),
                new NetworkNode("yield", "Expected cared-for yield", NetworkNodeKind.Outcome, "The planned harvest adjusted by care success.", "Care-to-value bridge: expected yield multiplies the sale-price estimate."),
                new NetworkNode("utility", "Expected utility", NetworkNodeKind.Utility, "Expected sale value minus seed opportunity value.", "Economic pressure point: revenue, care, and seed cost meet here.")
            },
            Edges: new[]
            {
                new NetworkEdge("day", "plant", "A late decision may not reach harvest."),
                new NetworkEdge("market", "price", "Today's quote is the point estimate."),
                new NetworkEdge("care", "yield", "Care risk changes expected yield."),
                new NetworkEdge("plant", "yield", "Planting creates the possible crop outcome."),
                new NetworkEdge("price", "utility", "Price determines expected revenue."),
                new NetworkEdge("yield", "utility", "Yield contributes sale value."),
                new NetworkEdge("plant", "utility", "The decision consumes the seed opportunity.")
            },
            FixedFacts: new[]
            {
                new FixedFact("Seed opportunity value", $"{seedCost} coins"),
                new FixedFact("Planned yield", $"{plannedYield} {name.ToLowerInvariant()} units"),
                new FixedFact("Planned harvest", $"Day {plannedHarvestDay}"),
                new FixedFact("Crop pattern", "One-time harvest"),
                new FixedFact("Base price", $"{basePrice} coins")
            },
            Parameters: new[]
            {
                new ParameterDefinition("Future price multiplier", "A provisional point estimate, not a calibrated probability distribution."),
                new ParameterDefinition("Care success probability", "The chance that the planned watering and harvest work succeeds."),
                new ParameterDefinition("PASS utility", "The value assigned to leaving the decision uncommitted.")
            });
    }
}
