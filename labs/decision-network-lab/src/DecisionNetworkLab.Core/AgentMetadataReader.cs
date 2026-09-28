using System.Text.RegularExpressions;

namespace DecisionNetworkLab.Core;

/// <summary>
/// Reads descriptive fields from the local Python agent catalog.
///
/// The catalog is source text, not a Python module that this lab executes.
/// This small reader intentionally extracts only the documented metadata
/// fields needed for the evidence list.
/// </summary>
public static partial class AgentMetadataReader
{
    public static IReadOnlyList<AgentMetadata> Read(string pythonSource)
    {
        var entries = new List<AgentMetadata>();
        string? id = null;
        var fields = new Dictionary<string, string>(StringComparer.Ordinal);
        var traits = Array.Empty<string>();

        foreach (var line in pythonSource.Split('\n'))
        {
            var entry = EntryStartRegex.Match(line);
            if (entry.Success)
            {
                AddCurrent(entries, id, fields, traits);
                id = entry.Groups["id"].Value;
                fields = new Dictionary<string, string>(StringComparer.Ordinal);
                traits = Array.Empty<string>();
            }

            if (id is null)
            {
                continue;
            }

            CaptureString(line, "label", fields);
            CaptureString(line, "description", fields);
            CaptureString(line, "experiment_round", fields);
            CaptureString(line, "source_kind", fields);
            CaptureString(line, "source_path", fields);

            var approximate = BoolRegex("approximate").Match(line);
            if (approximate.Success)
            {
                fields["approximate"] = approximate.Groups["value"].Value;
            }

            var traitMatch = TraitsRegex.Match(line);
            if (traitMatch.Success)
            {
                traits = QuotedValueRegex.Matches(traitMatch.Groups["values"].Value)
                    .Select(match => match.Groups["value"].Value)
                    .ToArray();
            }
        }

        AddCurrent(entries, id, fields, traits);
        return entries;
    }

    private static void AddCurrent(
        ICollection<AgentMetadata> entries,
        string? id,
        IReadOnlyDictionary<string, string> fields,
        IReadOnlyList<string> traits)
    {
        if (id is null || !fields.TryGetValue("label", out var label))
        {
            return;
        }

        bool? approximate = fields.TryGetValue("approximate", out var value)
            ? bool.Parse(value)
            : null;

        fields.TryGetValue("description", out var description);
        fields.TryGetValue("experiment_round", out var experimentRound);
        fields.TryGetValue("source_kind", out var sourceKind);
        fields.TryGetValue("source_path", out var sourcePath);

        entries.Add(new AgentMetadata(
            Id: id,
            Label: label,
            Description: description,
            Traits: traits,
            ExperimentRound: experimentRound,
            Approximate: approximate,
            SourceKind: sourceKind,
            SourcePath: sourcePath));
    }

    private static void CaptureString(string line, string field, IDictionary<string, string> fields)
    {
        var match = QuotedFieldRegex(field).Match(line);
        if (match.Success)
        {
            fields[field] = match.Groups["value"].Value;
        }
    }

    private static readonly Regex EntryStartRegex = new(
        @"^\s{8}(?:\x22(?<id>[^\x22]+)\x22|(?<ignored>[^:]+))\s*:\s*\{",
        RegexOptions.Compiled);

    private static Regex QuotedFieldRegex(string field) =>
        new($@"\x22{Regex.Escape(field)}\x22\s*:\s*\x22(?<value>(?:\\.|[^\x22\\])*)\x22", RegexOptions.Compiled);

    private static Regex BoolRegex(string field) =>
        new($@"\x22{Regex.Escape(field)}\x22\s*:\s*(?<value>true|false)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex TraitsRegex = new(
        @"\x22traits\x22\s*:\s*\[(?<values>[^\]]*)\]",
        RegexOptions.Compiled);

    private static readonly Regex QuotedValueRegex = new(
        @"\x22(?<value>(?:\\.|[^\x22\\])*)\x22",
        RegexOptions.Compiled);
}
