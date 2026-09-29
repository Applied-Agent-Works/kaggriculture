namespace DecisionNetworkLab.Core;

/// <summary>
/// One player's action and compact observation at one recorded simulator step.
/// </summary>
public sealed record RecordedPlayerStep(
    int Player,
    string Action,
    decimal Reward,
    string Status,
    string ObservationSummary);

/// <summary>
/// Both players' records for one simulator step.
/// </summary>
public sealed record RecordedTurn(
    int Step,
    int Day,
    int Hour,
    IReadOnlyList<RecordedPlayerStep> Players);

/// <summary>
/// One day of a recorded match, containing its simulator turns.
/// </summary>
public sealed record RecordedDay(
    int Day,
    IReadOnlyList<RecordedTurn> Turns);

/// <summary>
/// A full recording opened from a match-history or replay source.
/// </summary>
public sealed record RecordedMatchDetails(
    RecordedMatchSummary Summary,
    IReadOnlyList<RecordedDay> Days)
{
    public string? RawReplayJson { get; init; }
    public string? ViewerUrl { get; init; }
}

/// <summary>
/// One replay file listed by the separate replay-recordings source group.
/// </summary>
public sealed record ReplayRecordingSummary(
    string Id,
    string FileName,
    DateTimeOffset RecordedAtUtc,
    long SizeBytes,
    string Agent,
    string Opponent,
    int Days,
    int Steps,
    int Seed,
    IReadOnlyList<decimal> Rewards,
    IReadOnlyList<string> Statuses);

public sealed record ReplayCatalog(
    string SourceDirectory,
    DateTimeOffset LoadedAtUtc,
    IReadOnlyList<ReplayRecordingSummary> Recordings);
