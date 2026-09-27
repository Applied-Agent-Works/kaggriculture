using DecisionNetworkLab.Core;
using DecisionNetworkLab.Client.Components;

namespace DecisionNetworkLab.Client.Pages;

public partial class Home
{
    private string selectedNetworkId = NetworkCatalog.All[0].Id;
    private DecisionScenario currentScenario = CreateScenario(NetworkCatalog.All[0]);
    private bool DiagramExpanded { get; set; }

    private IReadOnlyList<DecisionNetworkDefinition> Networks => NetworkCatalog.All;

    private DecisionNetworkDefinition CurrentNetwork =>
        NetworkCatalog.GetById(selectedNetworkId);

    private DecisionScenario CurrentScenario => currentScenario;

    private DecisionResult Result =>
        DecisionEvaluator.Evaluate(CurrentNetwork, CurrentScenario);

    private ExperimentEvidenceSummary Evidence => EvidencePrototype.Carrot;

    private void ToggleDiagramExpanded()
    {
        DiagramExpanded = !DiagramExpanded;
    }

    private string SelectedNetworkId
    {
        get => selectedNetworkId;
        set
        {
            if (string.Equals(selectedNetworkId, value, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            selectedNetworkId = value;
            currentScenario = CreateScenario(CurrentNetwork);
        }
    }

    private static DecisionScenario CreateScenario(DecisionNetworkDefinition network)
    {
        return new DecisionScenario
        {
            CurrentPrice = network.CropCode switch
            {
                "CARROT" => 35m,
                "WHEAT" => 25m,
                "MELON" => 250m,
                _ => 1m
            }
        };
    }
}
