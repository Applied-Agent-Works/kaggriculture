using DecisionNetworkLab.Core;
using Microsoft.Extensions.FileProviders;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var configuredMatchHistoryDirectory = RequireDirectorySetting(builder.Configuration, "MatchHistory:Directory");
var agentCatalogFile = builder.Configuration["MatchHistory:AgentCatalogFile"];
var replayDirectory = RequireDirectorySetting(builder.Configuration, "Replay:Directory");
var evidencePackageDirectory = RequireDirectorySetting(builder.Configuration, "Evidence:PackageDirectory");
var viewerBaseUrl = RequireSetting(builder.Configuration, "Viewer:BaseUrl").TrimEnd('/');
using var viewerHttpClient = new HttpClient { BaseAddress = new Uri(viewerBaseUrl + "/") };
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

app.MapGet("/api/viewer/matches", async () =>
{
    try
    {
        return Results.Ok(await ReadViewerCatalog(viewerHttpClient, viewerBaseUrl));
    }
    catch (Exception exception) when (exception is HttpRequestException or JsonException or InvalidDataException)
    {
        return Results.Problem(
            detail: $"The Kaggriculture launcher could not be read at {viewerBaseUrl}: {exception.Message}",
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }
});

app.MapGet("/api/viewer/agents", async () =>
{
    try
    {
        return Results.Ok(await ReadViewerAgents(viewerHttpClient, viewerBaseUrl));
    }
    catch (Exception exception) when (exception is HttpRequestException or JsonException or InvalidDataException)
    {
        return Results.Problem(
            detail: $"The Kaggriculture launcher agent catalog could not be read: {exception.Message}",
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }
});

app.MapPost("/api/viewer/run-match", async (ViewerRunRequest request) =>
{
    if (string.IsNullOrWhiteSpace(request.Agent)
        || string.IsNullOrWhiteSpace(request.Opponent)
        || request.Days is < 1 or > 30
        || string.IsNullOrWhiteSpace(request.Seed))
    {
        return Results.BadRequest("Agent, opponent, days, and seed are required.");
    }

    var payload = JsonSerializer.Serialize(new
    {
        agent = request.Agent,
        opponent = request.Opponent,
        days = request.Days,
        seed = request.Seed
    });
    using var content = new StringContent(payload, System.Text.Encoding.UTF8, "application/json");
    using var response = await viewerHttpClient.PostAsync("api/run-match", content);
    var responseText = await response.Content.ReadAsStringAsync();
    return Results.Content(responseText, "application/json", statusCode: (int)response.StatusCode);
});

app.MapGet("/api/viewer/matches/{id}", async (string id) =>
{
    try
    {
        var catalog = await ReadViewerCatalog(viewerHttpClient, viewerBaseUrl);
        var requested = catalog.Matches.FirstOrDefault(match =>
            string.Equals(match.Id, id, StringComparison.Ordinal));
        if (requested is null)
        {
            return Results.NotFound();
        }

        using var response = await viewerHttpClient.GetAsync($"api/matches/{Uri.EscapeDataString(id)}");
        var responseText = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            return Results.Problem(
                detail: $"The launcher returned {(int)response.StatusCode}: {responseText}",
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        using var document = JsonDocument.Parse(responseText);
        var root = document.RootElement;
        var match = root.GetProperty("match");
        var returnedId = match.GetProperty("id").GetString();
        if (!string.Equals(returnedId, id, StringComparison.Ordinal))
        {
            throw new InvalidDataException("The launcher returned a different match ID than requested.");
        }

        var replay = root.GetProperty("replay");
        var steps = replay.GetProperty("steps");
        var rewards = replay.GetProperty("rewards");
        var statuses = replay.GetProperty("statuses");
        if (steps.ValueKind != JsonValueKind.Array
            || steps.GetArrayLength() != requested.Steps
            || rewards.ValueKind != JsonValueKind.Array
            || statuses.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidDataException("The launcher replay failed the match metadata validation.");
        }

        var rawReplay = replay.GetRawText();
        return Results.Ok(RecordedMatchReader.Read(
            rawReplay,
            requested,
            rawReplay,
            viewerBaseUrl + "/viewer"));
    }
    catch (Exception exception) when (exception is HttpRequestException or JsonException or InvalidDataException or RecordedMatchReadException)
    {
        return Results.Problem(
            detail: $"The selected launcher match could not be loaded: {exception.Message}",
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
    return Path.GetFullPath(RequireSetting(configuration, key));
}

static string RequireSetting(IConfiguration configuration, string key)
{
    var value = configuration[key];
    if (string.IsNullOrWhiteSpace(value))
    {
        throw new InvalidOperationException($"Missing required local setting '{key}'.");
    }

    return value;
}

static async Task<ViewerMatchCatalog> ReadViewerCatalog(HttpClient client, string baseUrl)
{
    var agentCatalog = await ReadViewerAgents(client, baseUrl);
    var byLabel = agentCatalog.Agents.ToDictionary(agent => agent.Label, StringComparer.Ordinal);

    using var matchesDocument = JsonDocument.Parse(await client.GetStringAsync("api/matches"));
    if (!matchesDocument.RootElement.TryGetProperty("matches", out var matchesElement))
    {
        throw new InvalidDataException("The launcher returned no matches collection.");
    }

    var matches = matchesElement.EnumerateArray().Select(match =>
    {
        var agent = ReadString(match, "agent") ?? "Unknown agent";
        var opponent = ReadString(match, "opponent") ?? "Unknown opponent";
        var summary = new RecordedMatchSummary(
            Id: RequiredExternal(match, "id"),
            CreatedAt: ReadString(match, "createdAt") ?? string.Empty,
            Agent: agent,
            Opponent: opponent,
            Days: ReadInt(match, "days"),
            Steps: ReadInt(match, "steps"),
            Seed: ReadInt(match, "seed"),
            Rewards: ReadDecimalArray(match, "rewards"),
            Statuses: ReadStringArray(match, "statuses"),
            Winner: ReadString(match, "winner"),
            Source: ReadString(match, "source") ?? "local",
            RecordingPath: ReadString(match, "path") ?? string.Empty)
        {
            AgentId = FindAgentId(byLabel, agent),
            OpponentId = FindAgentId(byLabel, opponent),
            AgentMetadata = FindMetadata(byLabel, agent),
            OpponentMetadata = FindMetadata(byLabel, opponent)
        };
        return summary;
    }).ToArray();

    return new ViewerMatchCatalog(baseUrl, DateTimeOffset.UtcNow, matches);
}

static async Task<ViewerAgentCatalog> ReadViewerAgents(HttpClient client, string baseUrl)
{
    using var agentsDocument = JsonDocument.Parse(await client.GetStringAsync("api/agents"));
    var agents = agentsDocument.RootElement.TryGetProperty("agents", out var agentsElement)
        ? agentsElement.EnumerateArray().Select(ReadViewerAgent).ToArray()
        : Array.Empty<AgentMetadata>();
    return new ViewerAgentCatalog(baseUrl, DateTimeOffset.UtcNow, agents);
}

static AgentMetadata ReadViewerAgent(JsonElement element)
{
    return new AgentMetadata(
        Id: ReadString(element, "id") ?? string.Empty,
        Label: ReadString(element, "label") ?? "Unknown agent",
        Description: ReadString(element, "description"),
        Traits: ReadStringArray(element, "traits"),
        ExperimentRound: ReadString(element, "experiment_round"),
        Approximate: ReadBool(element, "approximate"),
        SourceKind: ReadString(element, "source_kind"),
        SourcePath: ReadString(element, "source_path"))
    {
        Runnable = ReadBool(element, "runnable"),
        Version = ReadString(element, "version"),
        CreatedAt = ReadString(element, "created_at"),
        UpdatedAt = ReadString(element, "updated_at")
    };
}

static string? FindAgentId(IReadOnlyDictionary<string, AgentMetadata> agents, string label) =>
    agents.TryGetValue(label, out var agent) ? agent.Id : null;

static string RequiredExternal(JsonElement element, string name) =>
    ReadString(element, name) ?? throw new InvalidDataException($"The launcher match is missing '{name}'.");

static string? ReadString(JsonElement element, string name) =>
    element.ValueKind == JsonValueKind.Object && element.TryGetProperty(name, out var value)
        ? value.GetString()
        : null;

static int ReadInt(JsonElement element, string name) =>
    element.ValueKind == JsonValueKind.Object && element.TryGetProperty(name, out var value)
        && value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var result) ? result : 0;

static bool? ReadBool(JsonElement element, string name) =>
    element.ValueKind == JsonValueKind.Object && element.TryGetProperty(name, out var value)
        && value.ValueKind is JsonValueKind.True or JsonValueKind.False ? value.GetBoolean() : null;

static IReadOnlyList<decimal> ReadDecimalArray(JsonElement element, string name) =>
    element.ValueKind == JsonValueKind.Object && element.TryGetProperty(name, out var values)
        && values.ValueKind == JsonValueKind.Array
        ? values.EnumerateArray().Select(value => value.TryGetDecimal(out var result) ? result : 0m).ToArray()
        : Array.Empty<decimal>();

static IReadOnlyList<string> ReadStringArray(JsonElement element, string name) =>
    element.ValueKind == JsonValueKind.Object && element.TryGetProperty(name, out var values)
        && values.ValueKind == JsonValueKind.Array
        ? values.EnumerateArray().Select(value => value.GetString() ?? string.Empty).ToArray()
        : Array.Empty<string>();

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

public sealed record ViewerRunRequest(string Agent, string Opponent, int Days, string Seed);
