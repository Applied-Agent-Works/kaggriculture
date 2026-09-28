using System.Text;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using DecisionNetworkLab.Core;

namespace DecisionNetworkLab.Client.Components;

public partial class ExperimentEvidencePrototype
{
    [Parameter]
    public ExperimentEvidenceSummary Evidence { get; set; } = EvidencePrototype.Carrot;

    private ExperimentEvidenceSummary? loadedEvidence;
    private string? LoadMessage;
    private bool LoadSucceeded;

    private ExperimentEvidenceSummary CurrentEvidence => loadedEvidence ?? Evidence;

    private string SelectedPairKey { get; set; } = "42-0";

    private ExperimentEvidencePair SelectedPair =>
        CurrentEvidence.Pairs.FirstOrDefault(pair => PairKey(pair) == SelectedPairKey)
        ?? CurrentEvidence.Pairs[0];

    private async Task LoadPackage(InputFileChangeEventArgs args)
    {
        var files = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        try
        {
            foreach (var file in args.GetMultipleFiles(maximumFileCount: 10))
            {
                if (file.Name is not ("manifest.json" or "comparison-summary.json" or "run-summaries.jsonl"))
                {
                    continue;
                }

                await using var stream = file.OpenReadStream(maxAllowedSize: 10 * 1024 * 1024);
                using var reader = new StreamReader(stream, Encoding.UTF8);
                files[file.Name] = await reader.ReadToEndAsync();
            }

            loadedEvidence = EvidencePackageReader.Read(files);
            SelectedPairKey = loadedEvidence.Pairs.Count > 0
                ? PairKey(loadedEvidence.Pairs[0])
                : SelectedPairKey;
            LoadSucceeded = true;
            LoadMessage = $"Loaded {loadedEvidence.ExperimentId} from the selected evidence files.";
        }
        catch (EvidencePackageReadException exception)
        {
            loadedEvidence = null;
            LoadSucceeded = false;
            LoadMessage = exception.Message;
        }
        catch (Exception exception)
        {
            loadedEvidence = null;
            LoadSucceeded = false;
            LoadMessage = $"The selected files could not be read: {exception.Message}";
        }
    }

    private static string PairKey(ExperimentEvidencePair pair) =>
        $"{pair.Seed}-{pair.DecisionPlayer}";
}
