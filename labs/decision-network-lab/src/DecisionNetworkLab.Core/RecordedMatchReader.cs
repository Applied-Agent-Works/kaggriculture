using System.Globalization;
using System.Text.Json;

namespace DecisionNetworkLab.Core;

/// <summary>
/// Converts one large JSON recording into small day-and-turn review models.
/// </summary>
public static class RecordedMatchReader
{
    public static RecordedMatchDetails Read(
        string json,
        RecordedMatchSummary summary,
        string? rawReplayJson = null,
        string? viewerUrl = null)
    {
        using var document = JsonDocument.Parse(json);
        if (!document.RootElement.TryGetProperty("steps", out var stepsElement))
        {
            throw new RecordedMatchReadException("The recording has no steps collection.");
        }

        var turns = new List<RecordedTurn>();
        var stepNumber = 0;
        foreach (var step in stepsElement.EnumerateArray())
        {
            var players = new List<RecordedPlayerStep>();
            if (step.ValueKind == JsonValueKind.Array)
            {
                var playerNumber = 0;
                foreach (var player in step.EnumerateArray())
                {
                    players.Add(ReadPlayer(player, playerNumber));
                    playerNumber++;
                }
            }

            var first = players.FirstOrDefault();
            turns.Add(new RecordedTurn(
                Step: stepNumber,
                Day: first?.Day() ?? stepNumber / 24,
                Hour: first?.Hour() ?? stepNumber % 24,
                Players: players));
            stepNumber++;
        }

        var days = turns
            .GroupBy(turn => turn.Day)
            .OrderBy(group => group.Key)
            .Select(group => new RecordedDay(group.Key, group.ToArray()))
            .ToArray();

        return new RecordedMatchDetails(summary, days)
        {
            RawReplayJson = rawReplayJson,
            ViewerUrl = viewerUrl
        };
    }

    private static RecordedPlayerStep ReadPlayer(JsonElement player, int playerNumber)
    {
        var observation = player.TryGetProperty("observation", out var observationElement)
            ? observationElement
            : default;
        var day = ReadInt(observation, "day", 0);
        var hour = ReadInt(observation, "hour", 0);
        var action = player.TryGetProperty("action", out var actionElement)
            ? actionElement.GetRawText()
            : "unknown";

        return new RecordedPlayerStep(
            Player: playerNumber,
            Action: action,
            Reward: ReadDecimal(player, "reward"),
            Status: ReadString(player, "status") ?? "unknown",
            ObservationSummary: ObservationSummary(observation, day, hour));
    }

    private static string ObservationSummary(JsonElement observation, int day, int hour)
    {
        var values = new List<string> { $"day={day}", $"hour={hour}" };

        if (observation.ValueKind == JsonValueKind.Object
            && observation.TryGetProperty("farms", out var farms)
            && farms.ValueKind == JsonValueKind.Array)
        {
            var money = farms.EnumerateArray()
                .Select(farm => farm.TryGetProperty("money", out var moneyElement)
                    ? moneyElement.ToString()
                    : null)
                .Where(value => value is not null)
                .ToArray();
            if (money.Length > 0)
            {
                values.Add($"farm money={string.Join(", ", money!)}");
            }
        }

        if (observation.ValueKind == JsonValueKind.Object
            && observation.TryGetProperty("market", out var market)
            && market.TryGetProperty("prices", out var prices)
            && prices.ValueKind == JsonValueKind.Object)
        {
            var priceText = prices.EnumerateObject()
                .Take(5)
                .Select(property => $"{property.Name}={property.Value}");
            values.Add($"prices {string.Join(", ", priceText)}");
        }

        return string.Join(" · ", values);
    }

    private static int ReadInt(JsonElement element, string name, int fallback)
    {
        return element.ValueKind == JsonValueKind.Object
            && element.TryGetProperty(name, out var value)
            && value.TryGetInt32(out var result)
            ? result
            : fallback;
    }

    private static decimal ReadDecimal(JsonElement element, string name)
    {
        return element.ValueKind == JsonValueKind.Object
            && element.TryGetProperty(name, out var value)
            && value.TryGetDecimal(out var result)
            ? result
            : 0m;
    }

    private static string? ReadString(JsonElement element, string name)
    {
        return element.ValueKind == JsonValueKind.Object
            && element.TryGetProperty(name, out var value)
            ? value.GetString()
            : null;
    }

    private static int Day(this RecordedPlayerStep step) => ParseInt(step.ObservationSummary, "day") ?? 0;

    private static int Hour(this RecordedPlayerStep step) => ParseInt(step.ObservationSummary, "hour") ?? 0;

    private static int? ParseInt(string text, string name)
    {
        var prefix = name + "=";
        var value = text.Split(" · ", StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault(part => part.StartsWith(prefix, StringComparison.Ordinal));
        return value is not null
            && int.TryParse(value[prefix.Length..], NumberStyles.Integer, CultureInfo.InvariantCulture, out var result)
            ? result
            : null;
    }
}

public sealed class RecordedMatchReadException : Exception
{
    public RecordedMatchReadException(string message) : base(message) { }
}
