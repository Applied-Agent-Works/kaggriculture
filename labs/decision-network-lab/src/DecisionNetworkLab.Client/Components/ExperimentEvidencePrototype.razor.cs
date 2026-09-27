using Microsoft.AspNetCore.Components;
using DecisionNetworkLab.Core;

namespace DecisionNetworkLab.Client.Components;

public partial class ExperimentEvidencePrototype
{
    [Parameter]
    public ExperimentEvidenceSummary Evidence { get; set; } = EvidencePrototype.Carrot;

    private string SelectedPairKey { get; set; } = "42-0";

    private ExperimentEvidencePair SelectedPair =>
        Evidence.Pairs.FirstOrDefault(pair => PairKey(pair) == SelectedPairKey)
        ?? Evidence.Pairs[0];

    private static string PairKey(ExperimentEvidencePair pair) =>
        $"{pair.Seed}-{pair.DecisionPlayer}";
}
