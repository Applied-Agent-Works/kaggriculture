using DecisionNetworkLab.Core;

namespace DecisionNetworkLab.Client.Components;

/// <summary>
/// Representative data from the first frozen carrot experiment package.
///
/// This is a temporary fixture for designing the review experience. The next
/// increment can replace it with a reader for manifest.json and JSONL files.
/// </summary>
public static class EvidencePrototype
{
    public static ExperimentEvidenceSummary Carrot { get; } =
        new(
            ExperimentId: "carrot-decision-vs-conveyor-v1",
            Description: "Decision policy versus conveyor baseline · pass opponent",
            PairedRuns: 6,
            TraceCount: 12,
            BaselineMeanReward: 7895.50m,
            CandidateMeanReward: 7968.83m,
            MeanDelta: 73.33m,
            Pairs: new[]
            {
                Pair(42, 0, 6365m, 6425m, 60m),
                Pair(42, 1, 6325m, 6405m, 80m),
                Pair(43, 0, 9632m, 9692m, 60m),
                Pair(43, 1, 10919m, 10999m, 80m),
                Pair(44, 0, 7046m, 7126m, 80m),
                Pair(44, 1, 7086m, 7166m, 80m)
            });

    private static ExperimentEvidencePair Pair(
        int seed,
        int player,
        decimal baseline,
        decimal candidate,
        decimal delta)
    {
        return new ExperimentEvidencePair(
            Seed: seed,
            DecisionPlayer: player,
            BaselineReward: baseline,
            CandidateReward: candidate,
            CandidateMinusBaseline: delta,
            Trace: new ExperimentTracePreview(
                Policy: "candidate-decision",
                Seed: seed,
                DecisionPlayer: player,
                Step: 0,
                Day: 0,
                Decision: "PLANT CARROT",
                ExpectedSalePrice: 33.145m,
                PlantUtility: 79.435m,
                PassUtility: 0m,
                VisibleOpponentRipeCarrots: 0,
                SourceLabel: "candidate-decision trace · step 0"));
    }
}
