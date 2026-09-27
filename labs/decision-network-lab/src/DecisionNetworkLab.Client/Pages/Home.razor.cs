using DecisionNetworkLab.Core;
using DecisionNetworkLab.Client.Components;

namespace DecisionNetworkLab.Client.Pages;

public partial class Home
{
    private string selectedNetworkId = NetworkCatalog.All[0].Id;
    private DecisionScenario currentScenario = CreateScenario(NetworkCatalog.All[0]);
    private SimulationRun? simulation;
    private int simulationSeed = 42;
    private IReadOnlyList<DecisionNetworkDefinition> Networks => NetworkCatalog.All;

    private DecisionNetworkDefinition CurrentNetwork =>
        NetworkCatalog.GetById(selectedNetworkId);

    private DecisionScenario CurrentScenario => currentScenario;

    private DecisionResult Result =>
        DecisionEvaluator.Evaluate(CurrentNetwork, CurrentScenario);

    private ExperimentEvidenceSummary Evidence => EvidencePrototype.Carrot;

    private SimulationRun? Simulation => simulation;

    private int SimulationSeed
    {
        get => simulationSeed;
        set => simulationSeed = value;
    }

    private Task HandleScenarioChanged()
    {
        simulation = null;
        return Task.CompletedTask;
    }

    private void ApplyPreset(string preset)
    {
        currentScenario = CreateScenario(CurrentNetwork);

        switch (preset)
        {
            case "tight-season":
                currentScenario.Day = Math.Max(0, CurrentNetwork.SeasonDays - CurrentNetwork.PlannedHarvestDay - 1);
                break;
            case "low-care":
                currentScenario.CareSuccessProbability = 0.35m;
                break;
            case "supply-pressure":
                currentScenario.VisibleOpponentMatureCrops = 6;
                currentScenario.ActiveDemandSources = 0;
                break;
        }

        simulation = null;
    }

    private void RunSimulation()
    {
        simulation = InteractiveSimulator.Run(CurrentNetwork, CurrentScenario, SimulationSeed);
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
            simulation = null;
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
