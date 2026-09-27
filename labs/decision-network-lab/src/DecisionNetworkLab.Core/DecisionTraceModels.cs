namespace DecisionNetworkLab.Core;

/// <summary>
/// Describes the role a value plays in a decision trace.
/// </summary>
public enum DecisionTraceKind
{
    Evidence,
    Belief,
    Decision,
    Outcome,
    Utility,
    Simulation
}

/// <summary>
/// One visible step in the reasoning path from scenario input to decision.
/// </summary>
public sealed record DecisionTraceStep(
    int Order,
    string NodeId,
    DecisionTraceKind Kind,
    string Label,
    string Value,
    string Explanation);

/// <summary>
/// The utility and availability of one action in the decision comparison.
/// </summary>
public sealed record DecisionActionValue(
    string Action,
    decimal Utility,
    bool IsAvailable,
    bool IsRecommended,
    string Explanation);

/// <summary>
/// One state transition from the narrow local simulator.
/// </summary>
public sealed record SimulationStep(
    int Order,
    string Label,
    string Detail,
    string? NodeId,
    DecisionTraceKind Kind);

/// <summary>
/// The complete result of one deterministic, educational simulation run.
/// </summary>
public sealed record SimulationRun(
    int Seed,
    DecisionResult Decision,
    IReadOnlyList<SimulationStep> Steps,
    string Outcome,
    decimal FinalReward,
    decimal ActualYield,
    decimal ActualPrice);
