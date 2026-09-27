namespace DecisionNetworkLab.Core;

/// <summary>
/// The kinds of nodes that can appear in a small influence diagram.
/// </summary>
public enum NetworkNodeKind
{
    Evidence,
    Chance,
    Decision,
    Outcome,
    Utility
}

/// <summary>
/// Identifies the small pricing model used by a crop's first lab example.
/// </summary>
public enum PricingApproach
{
    /// <summary>Use today's quote as a transparent point estimate.</summary>
    PointEstimate,

    /// <summary>Use the first coarse carrot supply and demand belief table.</summary>
    CarrotBeliefModel
}

/// <summary>
/// A node is a named idea in a decision network.
/// </summary>
public sealed record NetworkNode(
    string Id,
    string Label,
    NetworkNodeKind Kind,
    string Description,
    string PressurePoint = "No pressure point documented yet.");

/// <summary>
/// An arrow explains which node supplies information to another node.
/// </summary>
public sealed record NetworkEdge(
    string FromNodeId,
    string ToNodeId,
    string Description);

/// <summary>
/// A game fact that the lab treats as fixed rather than tunable.
/// </summary>
public sealed record FixedFact(string Label, string Value);

/// <summary>
/// A named assumption or preference that the learner may explore.
/// </summary>
public sealed record ParameterDefinition(string Name, string Description);

/// <summary>
/// The descriptive and mechanical information needed to show one crop network.
/// </summary>
public sealed record DecisionNetworkDefinition(
    string Id,
    string Name,
    string CropCode,
    string Summary,
    int SeedCost,
    int PlannedYield,
    int PlannedHarvestDay,
    int SeasonDays,
    PricingApproach PricingApproach,
    IReadOnlyList<NetworkNode> Nodes,
    IReadOnlyList<NetworkEdge> Edges,
    IReadOnlyList<FixedFact> FixedFacts,
    IReadOnlyList<ParameterDefinition> Parameters);

/// <summary>
/// The values a learner can change for a what-if calculation.
/// </summary>
public sealed class DecisionScenario
{
    public int Day { get; set; }
    public decimal CurrentPrice { get; set; }
    public int MarketInventory { get; set; }
    public int VisibleOpponentMatureCrops { get; set; }
    public int ActiveDemandSources { get; set; }
    public decimal FuturePriceMultiplier { get; set; } = 1.0m;
    public decimal CareSuccessProbability { get; set; } = 1.0m;
    public decimal PassUtility { get; set; }
}

/// <summary>
/// The coarse price belief shown to the learner before utility is calculated.
/// </summary>
public sealed record PriceBelief(
    decimal LowProbability,
    decimal NormalProbability,
    decimal HighProbability,
    decimal LowPrice,
    decimal NormalPrice,
    decimal HighPrice)
{
    public decimal ExpectedPrice =>
        LowProbability * LowPrice
        + NormalProbability * NormalPrice
        + HighProbability * HighPrice;
}

/// <summary>
/// Everything the UI needs to explain one evaluation.
/// </summary>
public sealed record DecisionResult(
    DecisionNetworkDefinition Network,
    DecisionScenario Scenario,
    bool CanReachHarvest,
    decimal ExpectedYield,
    PriceBelief PriceBelief,
    decimal PlantUtility,
    decimal PassUtility,
    string Recommendation,
    IReadOnlyList<string> Explanation)
{
    /// <summary>
    /// The two actions shown by the evaluation panel.
    /// </summary>
    public IReadOnlyList<DecisionActionValue> Actions { get; init; } = Array.Empty<DecisionActionValue>();

    /// <summary>
    /// The compact causal path used by the interactive UI.
    /// </summary>
    public IReadOnlyList<DecisionTraceStep> Trace { get; init; } = Array.Empty<DecisionTraceStep>();
}
