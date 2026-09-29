namespace DecisionNetworkLab.Core;

/// <summary>
/// One recorded match listed by the local match-history index.
/// </summary>
public sealed record RecordedMatchSummary(
    string Id,
    string CreatedAt,
    string Agent,
    string Opponent,
    int Days,
    int Steps,
    int Seed,
    IReadOnlyList<decimal> Rewards,
    IReadOnlyList<string> Statuses,
    string? Winner,
    string Source,
    string RecordingPath)
{
    public string? AgentId { get; init; }
    public string? OpponentId { get; init; }

    /// <summary>
    /// Optional metadata describing the agent that produced Player 1.
    /// </summary>
    public AgentMetadata? AgentMetadata { get; init; }

    /// <summary>
    /// Optional metadata describing the opponent that produced Player 2.
    /// </summary>
    public AgentMetadata? OpponentMetadata { get; init; }
}

/// <summary>
/// Descriptive metadata from the local agent catalog. This is never executed.
/// </summary>
public sealed record AgentMetadata(
    string Id,
    string Label,
    string? Description,
    IReadOnlyList<string> Traits,
    string? ExperimentRound,
    bool? Approximate,
    string? SourceKind,
    string? SourcePath)
{
    public bool? Runnable { get; init; }
    public string? Version { get; init; }
    public string? CreatedAt { get; init; }
    public string? UpdatedAt { get; init; }
}

/// <summary>
/// The read-only response returned by the local evidence host.
/// </summary>
public sealed record MatchHistoryCatalog(
    string SourceDirectory,
    string? AgentCatalogPath,
    DateTimeOffset LoadedAtUtc,
    IReadOnlyList<RecordedMatchSummary> Matches);
