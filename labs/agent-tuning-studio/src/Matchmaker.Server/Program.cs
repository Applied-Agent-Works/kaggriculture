using System.Text.Json;
using System.Diagnostics;
using System.ComponentModel;
using Matchmaker.Core;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();
var repositoryRoot = Path.GetFullPath(Path.Combine(
    app.Environment.ContentRootPath,
    "..", "..", "..", ".."));
var catalog = new CatalogStore(
    Environment.GetEnvironmentVariable("KAGGRICULTURE_MATCHMAKER_DATA")
        ?? Path.Combine(repositoryRoot, "labs", "agent-tuning-studio", "catalog"),
    repositoryRoot);
var matchExecutor = new MatchExecutor(repositoryRoot, catalog);

var clientOutputBase = Path.GetFullPath(Path.Combine(
    app.Environment.ContentRootPath, "..", "Matchmaker.Client", "bin"));
var preferredConfiguration = builder.Environment.IsDevelopment() ? "Debug" : "Release";
var clientWebRoot = Path.Combine(clientOutputBase, preferredConfiguration, "net10.0", "publish", "wwwroot");
if (!Directory.Exists(clientWebRoot))
{
    clientWebRoot = new[] { "Debug", "Release" }
        .Select(configuration => Path.Combine(clientOutputBase, configuration, "net10.0", "publish", "wwwroot"))
        .FirstOrDefault(Directory.Exists) ?? clientWebRoot;
}
if (!Directory.Exists(clientWebRoot))
{
    throw new DirectoryNotFoundException($"The Matchmaker client output was not found at '{clientWebRoot}'. Build the Server project first.");
}
var clientFileProvider = new PhysicalFileProvider(clientWebRoot);
app.UseDefaultFiles(new DefaultFilesOptions { FileProvider = clientFileProvider });
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = clientFileProvider,
    ServeUnknownFileTypes = true,
    DefaultContentType = "application/octet-stream"
});

app.MapGet("/api/matchmaker/status", () => Results.Ok(new MatchmakerStatus(
    "Matchmaker",
    "Local catalog and runner",
    "Agent and match records are stored locally. Planned records can run through the isolated fixed-seed runner.",
    true,
    true)));

app.MapGet("/api/matchmaker/visualizer", async (HttpContext context) =>
{
    var visualizer = new VisualizerHost(repositoryRoot);
    var result = await visualizer.EnsureAsync(
        context.Request.Host.Host,
        $"{context.Request.Scheme}://{context.Request.Host}");
    return result.Error is null
        ? Results.Ok(result.Launch)
        : Results.Problem(result.Error, statusCode: StatusCodes.Status503ServiceUnavailable);
});

app.MapGet("/api/matchmaker/catalog", () => Results.Ok(catalog.ReadCatalog()));

app.MapGet("/api/matchmaker/agents", () => Results.Ok(catalog.ListAgents()));

app.MapGet("/api/matchmaker/agents/{id}", (string id) =>
    catalog.TryGetAgent(id, out var agent)
        ? Results.Ok(agent)
        : Results.NotFound(new { error = $"No agent record exists for '{id}'." }));

app.MapPost("/api/matchmaker/agents", (AgentUpsertRequest request) =>
{
    var validation = ValidateAgent(request);
    if (validation is not null)
    {
        return Results.BadRequest(new { error = validation });
    }

    var result = catalog.CreateAgent(request);
    return result.Error is not null
        ? Results.Conflict(new { error = result.Error })
        : Results.Created($"/api/matchmaker/agents/{request.Id}", result.Agent);
});

app.MapPut("/api/matchmaker/agents/{id}", (string id, AgentUpsertRequest request) =>
{
    if (!string.Equals(id, request.Id, StringComparison.Ordinal))
    {
        return Results.BadRequest(new { error = "The route id must match the request id." });
    }

    var validation = ValidateAgent(request);
    if (validation is not null)
    {
        return Results.BadRequest(new { error = validation });
    }

    var result = catalog.UpdateAgent(request);
    return result.Error switch
    {
        null => Results.Ok(result.Agent),
        "not_found" => Results.NotFound(new { error = $"No agent record exists for '{id}'." }),
        _ => Results.Conflict(new { error = result.Error })
    };
});

app.MapDelete("/api/matchmaker/agents/{id}", (string id) =>
{
    var result = catalog.DeleteAgent(id);
    return result switch
    {
        DeleteResult.Deleted => Results.NoContent(),
        DeleteResult.NotFound => Results.NotFound(new { error = $"No agent record exists for '{id}'." }),
        DeleteResult.InUse => Results.Conflict(new { error = $"Agent '{id}' is referenced by a match record." }),
        _ => Results.Problem("The Matchmaker catalog returned an unknown delete result.")
    };
});

app.MapGet("/api/matchmaker/matches", () => Results.Ok(catalog.ListMatches()));

app.MapGet("/api/matchmaker/matches/{id}", (string id) =>
    catalog.TryGetMatch(id, out var match)
        ? Results.Ok(match)
        : Results.NotFound(new { error = $"No match record exists for '{id}'." }));

app.MapGet("/api/matchmaker/matches/{id}/replay", (string id, HttpContext context) =>
{
    if (!catalog.TryGetMatch(id, out var match) || match is null)
    {
        return Results.NotFound(new { error = $"No match record exists for '{id}'." });
    }

    if (string.IsNullOrWhiteSpace(match.ArtifactPath))
    {
        return Results.Conflict(new { error = "This match does not have a retained replay artifact." });
    }

    var runsRoot = Path.GetFullPath(Path.Combine(repositoryRoot, "labs", "agent-tuning-studio", "runs"));
    var replayPath = Path.GetFullPath(Path.Combine(repositoryRoot, match.ArtifactPath, "replay.json"));
    if (!replayPath.StartsWith(runsRoot + Path.DirectorySeparatorChar, StringComparison.Ordinal) ||
        !File.Exists(replayPath))
    {
        return Results.NotFound(new { error = "The replay artifact could not be found." });
    }

    context.Response.Headers.CacheControl = "no-store";
    return Results.File(replayPath, "application/json", enableRangeProcessing: true);
});

app.MapPost("/api/matchmaker/matches/{id}/run", (string id) =>
{
    var result = catalog.StartMatch(id);
    return result.Error switch
    {
        null => StartMatchExecution(result.Match!, matchExecutor),
        "not_found" => Results.NotFound(new { error = $"No match record exists for '{id}'." }),
        _ => Results.Conflict(new { error = result.Error })
    };
});

app.MapPost("/api/matchmaker/matches", (MatchUpsertRequest request) =>
{
    var validation = ValidateMatch(request, catalog);
    if (validation is not null)
    {
        return Results.BadRequest(new { error = validation });
    }

    var result = catalog.CreateMatch(request);
    return result.Error is not null
        ? Results.Conflict(new { error = result.Error })
        : Results.Created($"/api/matchmaker/matches/{request.Id}", result.Match);
});

app.MapPut("/api/matchmaker/matches/{id}", (string id, MatchUpsertRequest request) =>
{
    if (!string.Equals(id, request.Id, StringComparison.Ordinal))
    {
        return Results.BadRequest(new { error = "The route id must match the request id." });
    }

    var validation = ValidateMatch(request, catalog);
    if (validation is not null)
    {
        return Results.BadRequest(new { error = validation });
    }

    var result = catalog.UpdateMatch(request);
    return result.Error switch
    {
        null => Results.Ok(result.Match),
        "not_found" => Results.NotFound(new { error = $"No match record exists for '{id}'." }),
        _ => Results.Conflict(new { error = result.Error })
    };
});

app.MapDelete("/api/matchmaker/matches/{id}", (string id) =>
    catalog.DeleteMatch(id)
        ? Results.NoContent()
        : Results.NotFound(new { error = $"No match record exists for '{id}'." }));

app.MapFallback(async context =>
{
    context.Response.ContentType = "text/html; charset=utf-8";
    await context.Response.SendFileAsync(Path.Combine(clientWebRoot, "index.html"));
});

app.Run();

static IResult StartMatchExecution(MatchmakerMatch match, MatchExecutor executor)
{
    _ = executor.RunAsync(match);
    return Results.Accepted($"/api/matchmaker/matches/{match.Id}", match);
}

static string? ValidateAgent(AgentUpsertRequest request)
{
    if (string.IsNullOrWhiteSpace(request.Id) || request.Id.Length > 120)
    {
        return "Agent id is required and must be at most 120 characters.";
    }

    if (string.IsNullOrWhiteSpace(request.Label))
    {
        return "Agent label is required.";
    }

    if (request.Kind is not ("repository" or "builtin"))
    {
        return "Agent kind must be 'repository' or 'builtin'.";
    }

    if (request.Kind == "repository" && string.IsNullOrWhiteSpace(request.EntryPoint))
    {
        return "Repository agents require an entryPoint.";
    }

    return null;
}

static string? ValidateMatch(MatchUpsertRequest request, CatalogStore catalog)
{
    if (string.IsNullOrWhiteSpace(request.Id))
    {
        return "Match id is required.";
    }

    if (!catalog.TryGetAgent(request.Agent, out _) || !catalog.TryGetAgent(request.Opponent, out _))
    {
        return "Both agent and opponent must refer to catalog agent ids.";
    }

    if (request.Seed < 0)
    {
        return "Seed must be non-negative.";
    }

    if (request.Steps is < 1 or > 720)
    {
        return "Steps must be between 1 and 720.";
    }

    if (request.Seat is < 0 or > 1)
    {
        return "Seat must be 0 or 1.";
    }

    if (request.Status is not ("planned" or "running" or "succeeded" or "failed" or "cancelled"))
    {
        return "Status must be planned, running, succeeded, failed, or cancelled.";
    }

    return null;
}

enum DeleteResult
{
    Deleted,
    NotFound,
    InUse
}

sealed class CatalogStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };
    private readonly object gate = new();
    private readonly string agentsPath;
    private readonly string matchesPath;
    private readonly string repositoryRoot;

    public CatalogStore(string directory, string repositoryRoot)
    {
        Directory.CreateDirectory(directory);
        agentsPath = Path.Combine(directory, "agents.json");
        matchesPath = Path.Combine(directory, "matches.json");
        this.repositoryRoot = repositoryRoot;
        EnsureSeedData();
    }

    public MatchmakerCatalog ReadCatalog()
    {
        lock (gate)
        {
            return new MatchmakerCatalog(
                ReadAgents(),
                ReadMatches().Select(ToSummary).ToArray(),
                "Catalog records are local metadata. Deleting a record never deletes agent source or match artifacts.");
        }
    }

    public IReadOnlyList<MatchmakerAgent> ListAgents()
    {
        lock (gate) return ReadAgents();
    }

    public IReadOnlyList<MatchmakerMatchSummary> ListMatches()
    {
        lock (gate) return ReadMatches().Select(ToSummary).ToArray();
    }

    public bool TryGetAgent(string id, out MatchmakerAgent? agent)
    {
        lock (gate)
        {
            agent = ReadAgents().FirstOrDefault(item => item.Id == id);
            return agent is not null;
        }
    }

    public bool TryGetMatch(string id, out MatchmakerMatch? match)
    {
        lock (gate)
        {
            match = ReadMatches().FirstOrDefault(item => item.Id == id);
            return match is not null;
        }
    }

    public (MatchmakerAgent? Agent, string? Error) CreateAgent(AgentUpsertRequest request)
    {
        lock (gate)
        {
            var agents = ReadAgents();
            if (agents.Any(item => item.Id == request.Id))
            {
                return (null, "An agent with this id already exists.");
            }

            var now = DateTimeOffset.UtcNow;
            var agent = ToAgent(request, now, now);
            WriteAgents(agents.Append(agent).ToArray());
            return (agent, null);
        }
    }

    public (MatchmakerAgent? Agent, string? Error) UpdateAgent(AgentUpsertRequest request)
    {
        lock (gate)
        {
            var agents = ReadAgents();
            var existing = agents.FirstOrDefault(item => item.Id == request.Id);
            if (existing is null)
            {
                return (null, "not_found");
            }

            var agent = ToAgent(request, existing.CreatedAt ?? DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
            WriteAgents(agents.Select(item => item.Id == request.Id ? agent : item).ToArray());
            return (agent, null);
        }
    }

    public DeleteResult DeleteAgent(string id)
    {
        lock (gate)
        {
            var agents = ReadAgents();
            if (!agents.Any(item => item.Id == id))
            {
                return DeleteResult.NotFound;
            }

            if (ReadMatches().Any(item => item.Agent == id || item.Opponent == id))
            {
                return DeleteResult.InUse;
            }

            WriteAgents(agents.Where(item => item.Id != id).ToArray());
            return DeleteResult.Deleted;
        }
    }

    public (MatchmakerMatch? Match, string? Error) CreateMatch(MatchUpsertRequest request)
    {
        lock (gate)
        {
            var matches = ReadMatches();
            if (matches.Any(item => item.Id == request.Id))
            {
                return (null, "A match with this id already exists.");
            }

            var now = DateTimeOffset.UtcNow;
            var match = ToMatch(request, now, now);
            WriteMatches(matches.Append(match).ToArray());
            return (match, null);
        }
    }

    public (MatchmakerMatch? Match, string? Error) StartMatch(string id)
    {
        lock (gate)
        {
            var matches = ReadMatches();
            var existing = matches.FirstOrDefault(item => item.Id == id);
            if (existing is null)
            {
                return (null, "not_found");
            }

            if (existing.Status != "planned")
            {
                return (null, $"Only planned matches can be run; '{id}' is {existing.Status}.");
            }

            var match = existing with
            {
                Status = "running",
                ErrorMessage = null,
                ExitCode = null,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            WriteMatches(matches.Select(item => item.Id == id ? match : item).ToArray());
            return (match, null);
        }
    }

    public bool CompleteMatch(
        string id,
        string status,
        string? artifactPath,
        string? errorMessage,
        int exitCode)
    {
        lock (gate)
        {
            var matches = ReadMatches();
            var existing = matches.FirstOrDefault(item => item.Id == id);
            if (existing is null)
            {
                return false;
            }

            var match = existing with
            {
                Status = status,
                ArtifactPath = artifactPath ?? existing.ArtifactPath,
                ErrorMessage = errorMessage,
                ExitCode = exitCode,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            WriteMatches(matches.Select(item => item.Id == id ? match : item).ToArray());
            return true;
        }
    }

    public (MatchmakerMatch? Match, string? Error) UpdateMatch(MatchUpsertRequest request)
    {
        lock (gate)
        {
            var matches = ReadMatches();
            var existing = matches.FirstOrDefault(item => item.Id == request.Id);
            if (existing is null)
            {
                return (null, "not_found");
            }

            var match = ToMatch(request, existing.CreatedAt ?? DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
            WriteMatches(matches.Select(item => item.Id == request.Id ? match : item).ToArray());
            return (match, null);
        }
    }

    public bool DeleteMatch(string id)
    {
        lock (gate)
        {
            var matches = ReadMatches();
            if (!matches.Any(item => item.Id == id))
            {
                return false;
            }

            WriteMatches(matches.Where(item => item.Id != id).ToArray());
            return true;
        }
    }

    private void EnsureSeedData()
    {
        lock (gate)
        {
            if (!File.Exists(agentsPath))
            {
                var now = DateTimeOffset.UtcNow;
                WriteAgents(new[]
                {
                    new MatchmakerAgent("main.py", "Carrot decision", "The current root submission policy for the local Kaggriculture agent.", ["local", "decision policy"], true, "repository", "main.py", now, now),
                    new MatchmakerAgent("random", "Random", "A built-in randomized opponent for smoke tests.", ["built-in", "baseline"], true, "builtin", "random", now, now),
                    new MatchmakerAgent("pass", "Pass", "A built-in no-op opponent for interface checks.", ["built-in", "control"], true, "builtin", "pass", now, now),
                    new MatchmakerAgent("starter", "Starter", "A built-in starter policy for local comparisons.", ["built-in", "baseline"], true, "builtin", "starter", now, now)
                });
            }

            if (!File.Exists(matchesPath))
            {
                WriteMatches(Array.Empty<MatchmakerMatch>());
            }
        }
    }

    private MatchmakerAgent[] ReadAgents() => ReadEnvelope<AgentEnvelope>(agentsPath)?.Agents ?? [];

    private MatchmakerMatch[] ReadMatches() => ReadEnvelope<MatchEnvelope>(matchesPath)?.Matches ?? [];

    private void WriteAgents(IEnumerable<MatchmakerAgent> agents) =>
        WriteEnvelope(agentsPath, new AgentEnvelope
        {
            Agents = agents.ToArray()
        });

    private void WriteMatches(IEnumerable<MatchmakerMatch> matches) =>
        WriteEnvelope(matchesPath, new MatchEnvelope
        {
            Matches = matches.ToArray()
        });

    private T? ReadEnvelope<T>(string path)
    {
        if (!File.Exists(path))
        {
            return default;
        }

        var value = JsonSerializer.Deserialize<T>(File.ReadAllText(path), JsonOptions);
        return value;
    }

    private void WriteEnvelope<T>(string path, T value)
    {
        var temporaryPath = $"{path}.{Environment.ProcessId}.tmp";
        File.WriteAllText(temporaryPath, JsonSerializer.Serialize(value, JsonOptions));
        File.Move(temporaryPath, path, true);
    }

    private static MatchmakerAgent ToAgent(AgentUpsertRequest request, DateTimeOffset created, DateTimeOffset updated) =>
        new(request.Id.Trim(), request.Label.Trim(), request.Description.Trim(),
            (request.Traits ?? []).Where(item => !string.IsNullOrWhiteSpace(item)).Select(item => item.Trim()).Distinct().ToArray(),
            request.Available, request.Kind, request.EntryPoint?.Trim(), created, updated);

    private MatchmakerMatch ToMatch(MatchUpsertRequest request, DateTimeOffset created, DateTimeOffset updated) =>
        new(request.Id.Trim(), request.Agent.Trim(), request.Opponent.Trim(), request.Seed, request.Steps, request.Seat,
            request.Status, request.ArtifactPath?.Trim(),
            RunSourceRevision(), request.Configuration, null, null, created, updated);

    private string? RunSourceRevision()
    {
        using var process = Process.Start(new ProcessStartInfo
        {
            FileName = "git",
            Arguments = "rev-parse HEAD",
            WorkingDirectory = repositoryRoot,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        });
        if (process is null)
        {
            return null;
        }

        process.WaitForExit();
        return process.ExitCode == 0 ? process.StandardOutput.ReadToEnd().Trim() : null;
    }

    private static MatchmakerMatchSummary ToSummary(MatchmakerMatch match) =>
        new(match.Id, match.Agent, match.Opponent, match.Seed, match.Steps, match.Seat, match.Status,
            match.ArtifactPath, match.ErrorMessage, match.UpdatedAt);

    private sealed class AgentEnvelope
    {
        public string Schema { get; set; } = "kaggriculture-agent-tuning-studio-agent-catalog/v1";
        public MatchmakerAgent[] Agents { get; set; } = [];
    }

    private sealed class MatchEnvelope
    {
        public string Schema { get; set; } = "kaggriculture-agent-tuning-studio-match-catalog/v1";
        public MatchmakerMatch[] Matches { get; set; } = [];
    }
}

sealed class MatchExecutor
{
    private const string RunnerPath = "labs/agent-tuning-studio/tools/run_isolated_match.py";
    private readonly string repositoryRoot;
    private readonly CatalogStore catalog;

    public MatchExecutor(string repositoryRoot, CatalogStore catalog)
    {
        this.repositoryRoot = repositoryRoot;
        this.catalog = catalog;
    }

    public async Task RunAsync(MatchmakerMatch match)
    {
        try
        {
            var result = await RunProcessAsync(match);
            catalog.CompleteMatch(
                match.Id,
                result.ExitCode == 0 ? "succeeded" : "failed",
                result.ArtifactPath,
                result.ExitCode == 0 ? null : result.ErrorMessage,
                result.ExitCode);
        }
        catch (MatchExecutionException error)
        {
            catalog.CompleteMatch(match.Id, "failed", null, error.Message, -1);
        }
        catch (Win32Exception error)
        {
            catalog.CompleteMatch(match.Id, "failed", null, $"Could not start the local runner: {error.Message}", -1);
        }
        catch (IOException error)
        {
            catalog.CompleteMatch(match.Id, "failed", null, $"Could not access the local runner: {error.Message}", -1);
        }
    }

    private async Task<ExecutionResult> RunProcessAsync(MatchmakerMatch match)
    {
        var agent = GetEntryPoint(match.Agent);
        var opponent = GetEntryPoint(match.Opponent);
        var python = ResolvePython();
        var startInfo = new ProcessStartInfo
        {
            FileName = python,
            WorkingDirectory = repositoryRoot,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        startInfo.ArgumentList.Add(RunnerPath);
        startInfo.ArgumentList.Add("--agent");
        startInfo.ArgumentList.Add(agent);
        startInfo.ArgumentList.Add("--opponent");
        startInfo.ArgumentList.Add(opponent);
        startInfo.ArgumentList.Add("--steps");
        startInfo.ArgumentList.Add(match.Steps.ToString());
        startInfo.ArgumentList.Add("--seed");
        startInfo.ArgumentList.Add(match.Seed.ToString());
        startInfo.ArgumentList.Add("--seat");
        startInfo.ArgumentList.Add(match.Seat.ToString());

        using var process = new Process { StartInfo = startInfo };
        if (!process.Start())
        {
            throw new MatchExecutionException("The local runner process could not be started.");
        }

        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        var stdout = await stdoutTask;
        var stderr = await stderrTask;
        var artifactPath = FindArtifactPath(stdout);
        var errorMessage = process.ExitCode == 0
            ? null
            : LastOutputLine(stderr) ?? LastOutputLine(stdout) ?? $"The local runner exited with code {process.ExitCode}.";
        return new ExecutionResult(process.ExitCode, artifactPath, errorMessage);
    }

    private string GetEntryPoint(string agentId)
    {
        if (!catalog.TryGetAgent(agentId, out var agent) || agent is null)
        {
            throw new MatchExecutionException($"No catalog agent exists for '{agentId}'.");
        }

        if (!agent.Available)
        {
            throw new MatchExecutionException($"Agent '{agentId}' is unavailable.");
        }

        if (string.IsNullOrWhiteSpace(agent.EntryPoint))
        {
            throw new MatchExecutionException($"Agent '{agentId}' has no executable entry point.");
        }

        return agent.EntryPoint;
    }

    private string ResolvePython()
    {
        var configured = Environment.GetEnvironmentVariable("KAGGRICULTURE_MATCHMAKER_PYTHON");
        if (!string.IsNullOrWhiteSpace(configured))
        {
            return configured;
        }

        var virtualEnvironmentPython = OperatingSystem.IsWindows()
            ? Path.Combine(repositoryRoot, ".venv", "Scripts", "python.exe")
            : Path.Combine(repositoryRoot, ".venv", "bin", "python");
        if (File.Exists(virtualEnvironmentPython))
        {
            return virtualEnvironmentPython;
        }

        throw new MatchExecutionException(
            "The repository virtual-environment Python executable was not found. " +
            "Set KAGGRICULTURE_MATCHMAKER_PYTHON or create .venv.");
    }

    private string? FindArtifactPath(string stdout)
    {
        var candidate = stdout
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .LastOrDefault(line => line.StartsWith("labs/agent-tuning-studio/runs/", StringComparison.Ordinal));
        if (candidate is null)
        {
            return null;
        }

        var fullPath = Path.GetFullPath(Path.Combine(repositoryRoot, candidate));
        var runsRoot = Path.GetFullPath(Path.Combine(repositoryRoot, "labs", "agent-tuning-studio", "runs"));
        if (!fullPath.StartsWith(runsRoot + Path.DirectorySeparatorChar, StringComparison.Ordinal) ||
            !Directory.Exists(fullPath))
        {
            throw new MatchExecutionException("The local runner reported an invalid artifact path.");
        }

        return Path.GetRelativePath(repositoryRoot, fullPath).Replace(Path.DirectorySeparatorChar, '/');
    }

    private static string? LastOutputLine(string output) =>
        output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).LastOrDefault();

    private sealed record ExecutionResult(int ExitCode, string? ArtifactPath, string? ErrorMessage);
}

sealed class VisualizerHost(string repositoryRoot)
{
    private const int Port = 5191;
    private const string TargetDirectory = "tools/visualizers/default";
    private const string ConfigFile = "vite.matchmaker.config.mjs";
    private readonly string repositoryRoot = repositoryRoot;

    public async Task<(VisualizerLaunch? Launch, string? Error)> EnsureAsync(string browserHost, string parentOrigin)
    {
        var environmentsRoot = FindEnvironmentsRoot();
        if (environmentsRoot is null)
        {
            return (null,
                "The visualizer dependencies are unavailable. Set KAGGRICULTURE_VISUALIZER_ROOT to a Kaggle Environments checkout with its pnpm dependencies installed.");
        }

        var visualizerDirectory = Path.Combine(repositoryRoot, TargetDirectory);
        var configPath = Path.Combine(visualizerDirectory, ConfigFile);
        if (!File.Exists(configPath))
        {
            return (null, $"The Matchmaker visualizer config was not found at '{configPath}'.");
        }

        var loopbackUrl = $"http://127.0.0.1:{Port}/";
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(1) };
        if (!await IsVisualizerHealthyAsync(client, loopbackUrl))
        {
            try
            {
                var node = Environment.GetEnvironmentVariable("KAGGRICULTURE_NODE") ?? "node";
                var vite = Path.Combine(environmentsRoot, "node_modules", "vite", "bin", "vite.js");
                var start = new ProcessStartInfo
                {
                    FileName = node,
                    WorkingDirectory = visualizerDirectory,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                start.ArgumentList.Add(vite);
                start.ArgumentList.Add("--config");
                start.ArgumentList.Add(configPath);
                start.ArgumentList.Add("--host");
                start.ArgumentList.Add("0.0.0.0");
                start.ArgumentList.Add("--port");
                start.ArgumentList.Add(Port.ToString());
                start.ArgumentList.Add("--strictPort");
                start.Environment["KAGGRICULTURE_ENVIRONMENTS_ROOT"] = environmentsRoot;
                start.Environment["KAGGRICULTURE_MATCHMAKER_API_TARGET"] = parentOrigin;
                using var process = Process.Start(start);
                if (process is null)
                {
                    return (null, "The Kaggriculture visualizer process could not be started.");
                }
            }
            catch (Exception error) when (error is Win32Exception or IOException)
            {
                return (null, $"Could not start the Kaggriculture visualizer: {error.Message}");
            }

            var deadline = DateTimeOffset.UtcNow.AddSeconds(20);
            while (DateTimeOffset.UtcNow < deadline)
            {
                if (await IsVisualizerHealthyAsync(client, loopbackUrl))
                {
                    break;
                }

                await Task.Delay(250);
            }

            if (!await IsVisualizerHealthyAsync(client, loopbackUrl))
            {
                return (null, $"The visualizer did not become ready on port {Port}. Check the Matchmaker server log for Vite startup errors.");
            }
        }

        var url = new UriBuilder(Uri.UriSchemeHttp, browserHost, Port).Uri.ToString();
        return (new VisualizerLaunch(url, parentOrigin), null);
    }

    private string? FindEnvironmentsRoot()
    {
        var configured = Environment.GetEnvironmentVariable("KAGGRICULTURE_VISUALIZER_ROOT");
        if (!string.IsNullOrWhiteSpace(configured) && IsEnvironmentsRoot(configured))
        {
            return Path.GetFullPath(configured);
        }

        if (IsEnvironmentsRoot(repositoryRoot))
        {
            return repositoryRoot;
        }

        var searchRoots = new HashSet<string>(StringComparer.Ordinal);
        var current = new DirectoryInfo(repositoryRoot);
        for (var level = 0; current is not null && level < 3; level++, current = current.Parent)
        {
            searchRoots.Add(current.FullName);
        }

        foreach (var searchRoot in searchRoots)
        {
            var found = FindEnvironmentsRoot(searchRoot, 3);
            if (found is not null)
            {
                return found;
            }
        }

        return null;
    }

    private static string? FindEnvironmentsRoot(string path, int depth)
    {
        if (IsEnvironmentsRoot(path))
        {
            return Path.GetFullPath(path);
        }

        if (depth == 0 || !Directory.Exists(path))
        {
            return null;
        }

        try
        {
            foreach (var directory in Directory.EnumerateDirectories(path))
            {
                var name = Path.GetFileName(directory);
                if (name is ".git" or "node_modules" or ".venv")
                {
                    continue;
                }

                var found = FindEnvironmentsRoot(directory, depth - 1);
                if (found is not null)
                {
                    return found;
                }
            }
        }
        catch (UnauthorizedAccessException)
        {
        }

        return null;
    }

    private static bool IsEnvironmentsRoot(string path) =>
        File.Exists(Path.Combine(path, "web", "core", "package.json")) &&
        File.Exists(Path.Combine(path, "node_modules", "vite", "bin", "vite.js"));

    private static async Task<bool> IsVisualizerHealthyAsync(HttpClient client, string url)
    {
        try
        {
            using var response = await client.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                return false;
            }

            var html = await response.Content.ReadAsStringAsync();
            return html.Contains("Kaggriculture Visualizer", StringComparison.Ordinal);
        }
        catch (HttpRequestException)
        {
            return false;
        }
        catch (TaskCanceledException)
        {
            return false;
        }
    }
}

sealed record VisualizerLaunch(string Url, string ParentOrigin);

sealed class MatchExecutionException : Exception
{
    public MatchExecutionException(string message)
        : base(message)
    {
    }
}
