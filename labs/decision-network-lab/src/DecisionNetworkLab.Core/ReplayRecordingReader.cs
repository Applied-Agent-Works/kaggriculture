using System.Text.Json;

namespace DecisionNetworkLab.Core;

/// <summary>
/// Reads replay-file metadata without treating replay files as match history.
/// </summary>
public static class ReplayRecordingReader
{
    public static IReadOnlyList<ReplayRecordingSummary> ReadCatalog(string directory)
    {
        if (!Directory.Exists(directory))
        {
            throw new DirectoryNotFoundException($"Replay directory not found: {directory}");
        }

        return Directory.EnumerateFiles(directory, "*.json")
            .Select(ReadSummary)
            .OrderByDescending(item => item.RecordedAtUtc)
            .ToArray();
    }

    public static ReplayRecordingSummary ReadSummary(string path)
    {
        var file = new FileInfo(path);
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        var root = document.RootElement;
        var configuration = root.TryGetProperty("configuration", out var config) ? config : default;
        var info = root.TryGetProperty("info", out var infoElement) ? infoElement : default;
        var steps = root.TryGetProperty("steps", out var stepsElement) && stepsElement.ValueKind == JsonValueKind.Array
            ? stepsElement.GetArrayLength()
            : 0;
        var turnsPerDay = ReadInt(configuration, "turnsPerDay", 24);
        var seed = ReadInt(info, "seed", ReadInt(configuration, "seed", 0));
        var rewards = ReadDecimalArray(root, "rewards");
        var statuses = ReadStringArray(root, "statuses");
        var names = ReadStringArray(info, "TeamNames");

        return new ReplayRecordingSummary(
            Id: file.Name,
            FileName: file.Name,
            RecordedAtUtc: file.LastWriteTimeUtc,
            SizeBytes: file.Length,
            Agent: names.Count > 0 ? names[0] : $"Replay · {file.Name}",
            Opponent: names.Count > 1 ? names[1] : "Imported replay",
            Days: Math.Max(1, (int)Math.Ceiling((double)steps / Math.Max(1, turnsPerDay))),
            Steps: steps,
            Seed: seed,
            Rewards: rewards,
            Statuses: statuses);
    }

    private static int ReadInt(JsonElement element, string name, int fallback)
    {
        return element.ValueKind == JsonValueKind.Object
            && element.TryGetProperty(name, out var value)
            && value.ValueKind == JsonValueKind.Number
            && value.TryGetInt32(out var result)
            ? result
            : fallback;
    }

    private static IReadOnlyList<decimal> ReadDecimalArray(JsonElement element, string name)
    {
        return element.ValueKind == JsonValueKind.Object
            && element.TryGetProperty(name, out var values)
            && values.ValueKind == JsonValueKind.Array
            ? values.EnumerateArray().Select(value => value.TryGetDecimal(out var number) ? number : 0m).ToArray()
            : Array.Empty<decimal>();
    }

    private static IReadOnlyList<string> ReadStringArray(JsonElement element, string name)
    {
        return element.ValueKind == JsonValueKind.Object
            && element.TryGetProperty(name, out var values)
            && values.ValueKind == JsonValueKind.Array
            ? values.EnumerateArray().Select(value => value.GetString() ?? string.Empty).ToArray()
            : Array.Empty<string>();
    }
}
