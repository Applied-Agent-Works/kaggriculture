using DecisionNetworkLab.Core;
using Microsoft.Extensions.FileProviders;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var configuredMatchHistoryDirectory = RequireDirectorySetting(builder.Configuration, "MatchHistory:Directory");
var agentCatalogFile = builder.Configuration["MatchHistory:AgentCatalogFile"];
var replayDirectory = RequireDirectorySetting(builder.Configuration, "Replay:Directory");
var evidencePackageDirectory = RequireDirectorySetting(builder.Configuration, "Evidence:PackageDirectory");
var clientWebRoot = Path.GetFullPath(Path.Combine(
    app.Environment.ContentRootPath,
    "..",
    "DecisionNetworkLab.Client",
    "bin",
    builder.Environment.IsDevelopment() ? "Debug" : "Release",
    "net10.0",
    "publish",
    "wwwroot"));

if (!Directory.Exists(clientWebRoot))
{
    throw new DirectoryNotFoundException($"The client output was not found at '{clientWebRoot}'. Build the Server project first.");
}

var clientFileProvider = new PhysicalFileProvider(clientWebRoot);
app.UseDefaultFiles(new DefaultFilesOptions { FileProvider = clientFileProvider });
app.UseStaticFiles(new StaticFileOptions { FileProvider = clientFileProvider });

app.MapGet("/api/match-history", () =>
{
    try
    {
        var matchHistoryDirectory = ResolveMatchHistoryDirectory(configuredMatchHistoryDirectory);
        var indexPath = Path.Combine(matchHistoryDirectory, "index.json");
        return Results.Ok(BuildMatchHistoryCatalog(matchHistoryDirectory, indexPath, agentCatalogFile));
    }
    catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or MatchHistoryReadException)
    {
        return Results.Problem(
            detail: $"The configured local match history could not be read: {exception.Message}",
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }
});

app.MapGet("/api/match-history/{id}", (string id) =>
{
    try
    {
        var matchHistoryDirectory = ResolveMatchHistoryDirectory(configuredMatchHistoryDirectory);
        var catalog = BuildMatchHistoryCatalog(
            matchHistoryDirectory,
            Path.Combine(matchHistoryDirectory, "index.json"),
            agentCatalogFile);
        var summary = catalog.Matches.FirstOrDefault(match => string.Equals(match.Id, id, StringComparison.OrdinalIgnoreCase));
        if (summary is null)
        {
            return Results.NotFound();
        }

        var recordingPath = ResolveSafeFile(matchHistoryDirectory, summary.RecordingPath);
        return Results.Ok(RecordedMatchReader.Read(File.ReadAllText(recordingPath), summary));
    }
    catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or MatchHistoryReadException or RecordedMatchReadException)
    {
        return Results.Problem(
            detail: $"The selected match recording could not be read: {exception.Message}",
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }
});

app.MapGet("/api/replays", () =>
{
    try
    {
        return Results.Ok(new ReplayCatalog(
            SourceDirectory: replayDirectory,
            LoadedAtUtc: DateTimeOffset.UtcNow,
            Recordings: ReplayRecordingReader.ReadCatalog(replayDirectory)));
    }
    catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or DirectoryNotFoundException or JsonException)
    {
        return Results.Problem(
            detail: $"The configured replay directory could not be read: {exception.Message}",
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }
});

app.MapGet("/api/replays/{id}", (string id) =>
{
    try
    {
        var replayPath = ResolveSafeFile(replayDirectory, id);
        var replay = ReplayRecordingReader.ReadSummary(replayPath);
        var summary = new RecordedMatchSummary(
            Id: replay.Id,
            CreatedAt: replay.RecordedAtUtc.ToString("O"),
            Agent: replay.Agent,
            Opponent: replay.Opponent,
            Days: replay.Days,
            Steps: replay.Steps,
            Seed: replay.Seed,
            Rewards: replay.Rewards,
            Statuses: replay.Statuses,
            Winner: WinnerFor(replay.Rewards),
            Source: "replay",
            RecordingPath: replay.FileName);
        return Results.Ok(RecordedMatchReader.Read(File.ReadAllText(replayPath), summary));
    }
    catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or FileNotFoundException or JsonException or RecordedMatchReadException)
    {
        return Results.Problem(
            detail: $"The selected replay could not be read: {exception.Message}",
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }
});

app.MapGet("/api/evidence", () =>
{
    try
    {
        var files = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var fileName in new[] { "manifest.json", "comparison-summary.json", "run-summaries.jsonl" })
        {
            files[fileName] = File.ReadAllText(Path.Combine(evidencePackageDirectory, fileName));
        }

        var traceFiles = Directory.EnumerateFiles(evidencePackageDirectory, "*.json.gz", SearchOption.AllDirectories)
            .ToDictionary(
                path => Path.GetRelativePath(evidencePackageDirectory, path).Replace('\\', '/'),
                path => File.ReadAllBytes(path),
                StringComparer.OrdinalIgnoreCase);

        return Results.Ok(EvidencePackageReader.Read(files, traceFiles));
    }
    catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or EvidencePackageReadException)
    {
        return Results.Problem(
            detail: $"The configured evidence package could not be read: {exception.Message}",
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }
});

app.MapFallback(async context =>
{
    context.Response.ContentType = "text/html; charset=utf-8";
    await context.Response.SendFileAsync(Path.Combine(clientWebRoot, "index.html"));
});

app.Run();

static string RequireDirectorySetting(IConfiguration configuration, string key)
{
    var value = configuration[key];
    if (string.IsNullOrWhiteSpace(value))
    {
        throw new InvalidOperationException($"Missing required local setting '{key}'.");
    }

    return Path.GetFullPath(value);
}

static string ResolveMatchHistoryDirectory(string configuredDirectory)
{
    if (Directory.Exists(configuredDirectory))
    {
        return configuredDirectory;
    }

    var producerRoot = Directory.GetParent(configuredDirectory)?.FullName;
    var archiveRoot = producerRoot is null ? null : Path.Combine(producerRoot, ".local_archive");
    var archived = archiveRoot is null
        ? Array.Empty<string>()
        : Directory.EnumerateDirectories(archiveRoot)
            .Select(path => Path.Combine(path, "local_match_history"))
            .Where(Directory.Exists)
            .OrderByDescending(path => Directory.GetLastWriteTimeUtc(Directory.GetParent(path)!.FullName))
            .ToArray();

    return archived.FirstOrDefault() ?? configuredDirectory;
}

static string ResolveSafeFile(string directory, string relativeFileName)
{
    var root = Path.GetFullPath(directory).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
    var candidate = Path.GetFullPath(Path.Combine(directory, relativeFileName));
    if (!candidate.StartsWith(root, StringComparison.OrdinalIgnoreCase))
    {
        throw new UnauthorizedAccessException("The requested recording is outside the configured source directory.");
    }

    return candidate;
}

static MatchHistoryCatalog BuildMatchHistoryCatalog(string directory, string indexPath, string? agentCatalogFile)
{
    var matches = MatchHistoryReader.Read(File.ReadAllText(indexPath));
    var metadata = ReadAgentMetadata(agentCatalogFile);
    var byLabel = metadata.ToDictionary(item => item.Label, StringComparer.Ordinal);
    var enrichedMatches = matches.Select(match => match with
    {
        AgentMetadata = FindMetadata(byLabel, match.Agent),
        OpponentMetadata = FindMetadata(byLabel, match.Opponent)
    }).ToArray();

    return new MatchHistoryCatalog(
        SourceDirectory: directory,
        AgentCatalogPath: agentCatalogFile,
        LoadedAtUtc: DateTimeOffset.UtcNow,
        Matches: enrichedMatches);
}

static string WinnerFor(IReadOnlyList<decimal> rewards)
{
    if (rewards.Count < 2) return "Unknown";
    return rewards[0] == rewards[1] ? "Draw" : rewards[0] > rewards[1] ? "Player 1" : "Player 2";
}

static IReadOnlyList<AgentMetadata> ReadAgentMetadata(string? catalogFile)
{
    if (string.IsNullOrWhiteSpace(catalogFile) || !File.Exists(catalogFile))
    {
        return Array.Empty<AgentMetadata>();
    }

    return AgentMetadataReader.Read(File.ReadAllText(catalogFile));
}

static AgentMetadata? FindMetadata(IReadOnlyDictionary<string, AgentMetadata> metadata, string label)
{
    return metadata.TryGetValue(label, out var item) ? item : null;
}
