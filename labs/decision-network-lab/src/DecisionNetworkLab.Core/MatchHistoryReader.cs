using System.Text.Json;
using System.Text.Json.Serialization;

namespace DecisionNetworkLab.Core;

/// <summary>
/// Reads the catalog file produced by the local match-history utility.
/// </summary>
public static class MatchHistoryReader
{
    public static IReadOnlyList<RecordedMatchSummary> Read(string json)
    {
        try
        {
            var document = JsonSerializer.Deserialize<IndexDocument>(json)
                ?? throw new JsonException("The index is empty.");

            if (document.Matches is null)
            {
                throw new JsonException("The index has no matches collection.");
            }

            return document.Matches.Select(match => new RecordedMatchSummary(
                Id: Required(match.Id, "id"),
                CreatedAt: Required(match.CreatedAt, "createdAt"),
                Agent: Required(match.Agent, "agent"),
                Opponent: Required(match.Opponent, "opponent"),
                Days: match.Days,
                Steps: match.Steps,
                Seed: match.Seed,
                Rewards: match.Rewards ?? Array.Empty<decimal>(),
                Statuses: match.Statuses ?? Array.Empty<string>(),
                Winner: match.Winner,
                Source: match.Source ?? "unknown",
                RecordingPath: Required(match.Path, "path")))
                .ToArray();
        }
        catch (JsonException exception)
        {
            throw new MatchHistoryReadException($"Invalid match-history index: {exception.Message}", exception);
        }
    }

    private static string Required(string? value, string field)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new JsonException($"A match is missing '{field}'.");
        }

        return value;
    }

    private sealed class IndexDocument
    {
        [JsonPropertyName("matches")]
        public List<MatchDocument>? Matches { get; set; }
    }

    private sealed class MatchDocument
    {
        [JsonPropertyName("id")] public string? Id { get; set; }
        [JsonPropertyName("createdAt")] public string? CreatedAt { get; set; }
        [JsonPropertyName("agent")] public string? Agent { get; set; }
        [JsonPropertyName("opponent")] public string? Opponent { get; set; }
        [JsonPropertyName("days")] public int Days { get; set; }
        [JsonPropertyName("steps")] public int Steps { get; set; }
        [JsonPropertyName("seed")] public int Seed { get; set; }
        [JsonPropertyName("rewards")] public decimal[]? Rewards { get; set; }
        [JsonPropertyName("statuses")] public string[]? Statuses { get; set; }
        [JsonPropertyName("winner")] public string? Winner { get; set; }
        [JsonPropertyName("source")] public string? Source { get; set; }
        [JsonPropertyName("path")] public string? Path { get; set; }
    }
}

public sealed class MatchHistoryReadException : Exception
{
    public MatchHistoryReadException(string message, Exception innerException)
        : base(message, innerException) { }
}
