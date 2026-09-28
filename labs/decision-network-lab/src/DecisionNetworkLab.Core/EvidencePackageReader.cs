using System.Text.Json;
using System.Text.Json.Serialization;

namespace DecisionNetworkLab.Core;

/// <summary>
/// Reads the small JSON part of a frozen Phase 2 evidence package.
///
/// This class does not know anything about Blazor or browser file inputs. The
/// client supplies file names and text; this class validates and normalizes
/// the evidence for the UI.
/// </summary>
public static class EvidencePackageReader
{
    private const string SupportedSchemaVersion = "1.0";

    public static ExperimentEvidenceSummary Read(
        IReadOnlyDictionary<string, string> files)
        => Read(files, new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase));

    public static ExperimentEvidenceSummary Read(
        IReadOnlyDictionary<string, string> files,
        IReadOnlyDictionary<string, byte[]> traceFiles)
    {
        var manifest = ReadJson<ManifestDocument>(files, "manifest.json");
        if (!string.Equals(manifest.SchemaVersion, SupportedSchemaVersion, StringComparison.OrdinalIgnoreCase))
        {
            throw new EvidencePackageReadException(
                $"Unsupported evidence schema '{manifest.SchemaVersion ?? "missing"}'. Expected {SupportedSchemaVersion}.");
        }

        var comparison = ReadJson<ComparisonDocument>(files, "comparison-summary.json");
        var summaries = ReadSummaryLines(files);
        var baselinePolicy = Required(manifest.Policies?.Baseline, "manifest.policies.baseline");
        var candidatePolicy = Required(manifest.Policies?.Candidate, "manifest.policies.candidate");
        var pairs = BuildPairs(
            comparison.Pairs,
            summaries,
            baselinePolicy,
            candidatePolicy,
            manifest.Artifacts?.DecisionTraces ?? Array.Empty<string>(),
            traceFiles);

        var baselineRewards = summaries
            .Where(row => row.Policy == baselinePolicy)
            .Select(row => row.PolicyReward)
            .ToArray();
        var candidateRewards = summaries
            .Where(row => row.Policy == candidatePolicy)
            .Select(row => row.PolicyReward)
            .ToArray();

        if (baselineRewards.Length == 0 || candidateRewards.Length == 0)
        {
            throw new EvidencePackageReadException("The package has no baseline or candidate run summaries.");
        }

        var controls = manifest.Controls;
        var controlsSummary = controls is null
            ? null
            : $"{controls.Game ?? "unknown game"} · {controls.EpisodeSteps} steps · "
              + $"seeds {string.Join(", ", controls.Seeds ?? Array.Empty<int>())} · "
              + $"opponent {controls.Opponent ?? "unknown"}";

        return new ExperimentEvidenceSummary(
            ExperimentId: Required(manifest.ExperimentId, "manifest.experiment_id"),
            Description: Required(manifest.Purpose, "manifest.purpose"),
            PairedRuns: pairs.Count,
            TraceCount: manifest.Artifacts?.DecisionTraces?.Length ?? 0,
            BaselineMeanReward: baselineRewards.Average(),
            CandidateMeanReward: candidateRewards.Average(),
            MeanDelta: comparison.MeanDelta ?? pairs.Average(pair => pair.CandidateMinusBaseline),
            Pairs: pairs,
            SourceRevision: manifest.Source?.GitRevision,
            ControlsSummary: controlsSummary,
            BaselinePolicy: baselinePolicy,
            CandidatePolicy: candidatePolicy);
    }

    private static List<ExperimentEvidencePair> BuildPairs(
        IReadOnlyList<ComparisonPairDocument>? comparisonPairs,
        IReadOnlyList<RunSummaryDocument> summaries,
        string baselinePolicy,
        string candidatePolicy,
        IReadOnlyList<string> tracePaths,
        IReadOnlyDictionary<string, byte[]> traceFiles)
    {
        if (comparisonPairs is null || comparisonPairs.Count == 0)
        {
            throw new EvidencePackageReadException("comparison-summary.json contains no paired runs.");
        }

        var result = new List<ExperimentEvidencePair>();
        foreach (var pair in comparisonPairs)
        {
            var baseline = summaries.SingleOrDefault(row =>
                row.Policy == baselinePolicy
                && row.Seed == pair.Seed
                && row.DecisionPlayer == pair.DecisionPlayer);
            var candidate = summaries.SingleOrDefault(row =>
                row.Policy == candidatePolicy
                && row.Seed == pair.Seed
                && row.DecisionPlayer == pair.DecisionPlayer);

            if (baseline is null || candidate is null)
            {
                throw new EvidencePackageReadException(
                    $"Missing baseline or candidate summary for seed {pair.Seed}, seat {pair.DecisionPlayer}.");
            }

            var tracePath = tracePaths.FirstOrDefault(path =>
                path.Replace('\\', '/').EndsWith(
                    $"{candidatePolicy}-seed-{pair.Seed}-seat-{pair.DecisionPlayer}.json.gz",
                    StringComparison.OrdinalIgnoreCase));

            result.Add(new ExperimentEvidencePair(
                Seed: pair.Seed,
                DecisionPlayer: pair.DecisionPlayer,
                BaselineReward: baseline.PolicyReward,
                CandidateReward: candidate.PolicyReward,
                CandidateMinusBaseline: candidate.PolicyReward - baseline.PolicyReward,
                Trace: ReadTracePreview(
                    tracePath,
                    traceFiles,
                    candidatePolicy,
                    pair.Seed,
                    pair.DecisionPlayer)));
        }

        return result;
    }

    private static ExperimentTracePreview ReadTracePreview(
        string? path,
        IReadOnlyDictionary<string, byte[]> traceFiles,
        string policy,
        int seed,
        int decisionPlayer)
    {
        if (path is null || !TryGetTrace(traceFiles, path, out var compressed))
        {
            return PendingTrace(policy, seed, decisionPlayer);
        }

        try
        {
            using var input = new MemoryStream(compressed);
            using var gzip = new System.IO.Compression.GZipStream(input, System.IO.Compression.CompressionMode.Decompress);
            using var reader = new StreamReader(gzip);
            using var document = JsonDocument.Parse(reader.ReadToEnd());
            var first = document.RootElement.ValueKind == JsonValueKind.Array
                ? document.RootElement.EnumerateArray().FirstOrDefault()
                : default;
            var evidence = first.TryGetProperty("evidence", out var evidenceElement) ? evidenceElement : default;
            var belief = first.TryGetProperty("belief", out var beliefElement) ? beliefElement : default;
            var utility = first.TryGetProperty("utility", out var utilityElement) ? utilityElement : default;

            return new ExperimentTracePreview(
                Policy: policy,
                Seed: seed,
                DecisionPlayer: decisionPlayer,
                Step: ReadInt(first, "step"),
                Day: ReadInt(first, "day"),
                Decision: ReadString(first, "decision") ?? "Unknown decision",
                ExpectedSalePrice: ReadDecimal(belief, "expected_sale_price"),
                PlantUtility: ReadDecimal(utility, "plant_carrot"),
                PassUtility: ReadDecimal(utility, "pass"),
                VisibleOpponentRipeCarrots: ReadInt(evidence, "visible_opponent_ripe_carrots"),
                SourceLabel: "Decompressed action-time trace");
        }
        catch (Exception)
        {
            return PendingTrace(policy, seed, decisionPlayer);
        }
    }

    private static ExperimentTracePreview PendingTrace(string policy, int seed, int decisionPlayer) =>
        new(policy, seed, decisionPlayer, 0, 0, "Trace unavailable", 0m, 0m, 0m, 0,
            "Compressed trace not loaded");

    private static bool TryGetTrace(IReadOnlyDictionary<string, byte[]> files, string path, out byte[] value)
    {
        var normalized = path.Replace('\\', '/');
        var match = files.FirstOrDefault(file => file.Key.Replace('\\', '/')
            .Equals(normalized, StringComparison.OrdinalIgnoreCase));
        value = match.Value ?? Array.Empty<byte>();
        return match.Key is not null;
    }

    private static int ReadInt(JsonElement element, string name) =>
        element.ValueKind == JsonValueKind.Object
        && element.TryGetProperty(name, out var value)
        && value.TryGetInt32(out var result) ? result : 0;

    private static decimal ReadDecimal(JsonElement element, string name) =>
        element.ValueKind == JsonValueKind.Object
        && element.TryGetProperty(name, out var value)
        && value.TryGetDecimal(out var result) ? result : 0m;

    private static string? ReadString(JsonElement element, string name) =>
        element.ValueKind == JsonValueKind.Object
        && element.TryGetProperty(name, out var value) ? value.GetString() : null;

    private static List<RunSummaryDocument> ReadSummaryLines(
        IReadOnlyDictionary<string, string> files)
    {
        var jsonLines = RequiredFile(files, "run-summaries.jsonl");
        var summaries = new List<RunSummaryDocument>();
        var lineNumber = 0;

        foreach (var line in jsonLines.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            lineNumber++;
            try
            {
                var summary = JsonSerializer.Deserialize<RunSummaryDocument>(line.Trim());
                if (summary is null || string.IsNullOrWhiteSpace(summary.Policy))
                {
                    throw new JsonException("The summary has no policy name.");
                }

                summaries.Add(summary);
            }
            catch (JsonException exception)
            {
                throw new EvidencePackageReadException(
                    $"Invalid run summary on JSONL line {lineNumber}: {exception.Message}",
                    exception);
            }
        }

        return summaries;
    }

    private static T ReadJson<T>(IReadOnlyDictionary<string, string> files, string fileName)
    {
        try
        {
            return JsonSerializer.Deserialize<T>(RequiredFile(files, fileName))
                ?? throw new JsonException("The file is empty.");
        }
        catch (JsonException exception)
        {
            throw new EvidencePackageReadException(
                $"Invalid JSON in {fileName}: {exception.Message}",
                exception);
        }
    }

    private static string RequiredFile(IReadOnlyDictionary<string, string> files, string fileName)
    {
        var match = files.FirstOrDefault(file =>
            string.Equals(file.Key, fileName, StringComparison.OrdinalIgnoreCase));

        if (string.IsNullOrWhiteSpace(match.Key))
        {
            throw new EvidencePackageReadException($"The selected package is missing {fileName}.");
        }

        return match.Value;
    }

    private static string Required(string? value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new EvidencePackageReadException($"The package is missing {fieldName}.");
        }

        return value;
    }

    private sealed class ManifestDocument
    {
        [JsonPropertyName("schema_version")] public string? SchemaVersion { get; set; }
        [JsonPropertyName("experiment_id")] public string? ExperimentId { get; set; }
        [JsonPropertyName("purpose")] public string? Purpose { get; set; }
        [JsonPropertyName("source")] public SourceDocument? Source { get; set; }
        [JsonPropertyName("controls")] public ControlsDocument? Controls { get; set; }
        [JsonPropertyName("policies")] public PoliciesDocument? Policies { get; set; }
        [JsonPropertyName("artifacts")] public ArtifactsDocument? Artifacts { get; set; }
    }

    private sealed class SourceDocument
    {
        [JsonPropertyName("git_revision")] public string? GitRevision { get; set; }
    }

    private sealed class ControlsDocument
    {
        [JsonPropertyName("seeds")] public int[]? Seeds { get; set; }
        [JsonPropertyName("opponent")] public string? Opponent { get; set; }
        [JsonPropertyName("episode_steps")] public int EpisodeSteps { get; set; }
        [JsonPropertyName("game")] public string? Game { get; set; }
    }

    private sealed class PoliciesDocument
    {
        [JsonPropertyName("baseline")] public string? Baseline { get; set; }
        [JsonPropertyName("candidate")] public string? Candidate { get; set; }
    }

    private sealed class ArtifactsDocument
    {
        [JsonPropertyName("decision_traces")] public string[]? DecisionTraces { get; set; }
    }

    private sealed class ComparisonDocument
    {
        [JsonPropertyName("mean_delta")] public decimal? MeanDelta { get; set; }
        [JsonPropertyName("pairs")] public List<ComparisonPairDocument>? Pairs { get; set; }
    }

    private sealed class ComparisonPairDocument
    {
        [JsonPropertyName("seed")] public int Seed { get; set; }
        [JsonPropertyName("decision_player")] public int DecisionPlayer { get; set; }
    }

    private sealed class RunSummaryDocument
    {
        [JsonPropertyName("policy")] public string? Policy { get; set; }
        [JsonPropertyName("seed")] public int Seed { get; set; }
        [JsonPropertyName("decision_player")] public int DecisionPlayer { get; set; }
        [JsonPropertyName("policy_reward")] public decimal PolicyReward { get; set; }
    }
}

public sealed class EvidencePackageReadException : Exception
{
    public EvidencePackageReadException(string message) : base(message) { }

    public EvidencePackageReadException(string message, Exception innerException)
        : base(message, innerException) { }
}
