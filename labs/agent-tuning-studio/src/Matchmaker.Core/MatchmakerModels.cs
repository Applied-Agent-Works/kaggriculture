namespace Matchmaker.Core;

public sealed record MatchmakerAgent(
    string Id,
    string Label,
    string Description,
    IReadOnlyList<string> Traits,
    bool Available,
    string Kind = "repository",
    string? EntryPoint = null,
    DateTimeOffset? CreatedAt = null,
    DateTimeOffset? UpdatedAt = null);

public sealed record MatchmakerMatch(
    string Id,
    string Agent,
    string Opponent,
    int Seed,
    int Steps,
    int Seat,
    string Status,
    string? ArtifactPath = null,
    string? SourceRevision = null,
    IReadOnlyDictionary<string, object?>? Configuration = null,
    string? ErrorMessage = null,
    int? ExitCode = null,
    DateTimeOffset? CreatedAt = null,
    DateTimeOffset? UpdatedAt = null);

public sealed record MatchmakerMatchSummary(
    string Id,
    string Agent,
    string Opponent,
    int Seed,
    int Steps,
    int Seat,
    string Status,
    string? ArtifactPath = null,
    string? ErrorMessage = null,
    DateTimeOffset? UpdatedAt = null);

public sealed record MatchmakerStatus(
    string Phase,
    string Title,
    string Message,
    bool MatchExecutionAvailable,
    bool DisplayLaunchAvailable);

public sealed record MatchmakerCatalog(
    IReadOnlyList<MatchmakerAgent> Agents,
    IReadOnlyList<MatchmakerMatchSummary> Matches,
    string Message);

public sealed record AgentUpsertRequest(
    string Id,
    string Label,
    string Description,
    IReadOnlyList<string>? Traits,
    string Kind,
    string? EntryPoint,
    bool Available = true);

public sealed record MatchUpsertRequest(
    string Id,
    string Agent,
    string Opponent,
    int Seed,
    int Steps,
    int Seat,
    string Status = "planned",
    string? ArtifactPath = null,
    IReadOnlyDictionary<string, object?>? Configuration = null);