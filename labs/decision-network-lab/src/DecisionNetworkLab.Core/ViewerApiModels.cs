namespace DecisionNetworkLab.Core;

/// <summary>
/// Normalized response from the existing Kaggriculture local viewer service.
/// </summary>
public sealed record ViewerMatchCatalog(
    string SourceDirectory,
    DateTimeOffset LoadedAtUtc,
    IReadOnlyList<RecordedMatchSummary> Matches);

public sealed record ViewerAgentCatalog(
    string BaseUrl,
    DateTimeOffset LoadedAtUtc,
    IReadOnlyList<AgentMetadata> Agents);
