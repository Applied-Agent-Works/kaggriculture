using System.Text.Json.Serialization;
using DecisionNetworkLab.Core;

namespace DecisionNetworkLab.Client;

/// <summary>
/// Keeps the local evidence response compatible with trimmed WebAssembly.
/// </summary>
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(MatchHistoryCatalog))]
[JsonSerializable(typeof(RecordedMatchSummary))]
[JsonSerializable(typeof(AgentMetadata))]
[JsonSerializable(typeof(RecordedMatchDetails))]
[JsonSerializable(typeof(RecordedDay))]
[JsonSerializable(typeof(RecordedTurn))]
[JsonSerializable(typeof(RecordedPlayerStep))]
[JsonSerializable(typeof(ReplayCatalog))]
[JsonSerializable(typeof(ReplayRecordingSummary))]
[JsonSerializable(typeof(ExperimentEvidenceSummary))]
[JsonSerializable(typeof(ExperimentEvidencePair))]
[JsonSerializable(typeof(ExperimentTracePreview))]
internal partial class MatchHistoryJsonContext : JsonSerializerContext
{
}
