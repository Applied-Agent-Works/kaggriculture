namespace DecisionNetworkLab.Core;

/// <summary>
/// A compact, UI-friendly view of one frozen experiment comparison.
/// </summary>
public sealed record ExperimentEvidenceSummary(
    string ExperimentId,
    string Description,
    int PairedRuns,
    int TraceCount,
    decimal BaselineMeanReward,
    decimal CandidateMeanReward,
    decimal MeanDelta,
    IReadOnlyList<ExperimentEvidencePair> Pairs,
    string? SourceRevision = null,
    string? ControlsSummary = null,
    string? BaselinePolicy = null,
    string? CandidatePolicy = null);

/// <summary>
/// A baseline-versus-candidate result for one fixed seed and player seat.
/// </summary>
public sealed record ExperimentEvidencePair(
    int Seed,
    int DecisionPlayer,
    decimal BaselineReward,
    decimal CandidateReward,
    decimal CandidateMinusBaseline,
    ExperimentTracePreview Trace);

/// <summary>
/// A small preview of action-time evidence from one decision trace.
/// </summary>
public sealed record ExperimentTracePreview(
    string Policy,
    int Seed,
    int DecisionPlayer,
    int Step,
    int Day,
    string Decision,
    decimal ExpectedSalePrice,
    decimal PlantUtility,
    decimal PassUtility,
    int VisibleOpponentRipeCarrots,
    string SourceLabel);
