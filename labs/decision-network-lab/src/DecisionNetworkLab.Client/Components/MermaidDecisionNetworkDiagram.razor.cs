using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using DecisionNetworkLab.Core;

namespace DecisionNetworkLab.Client.Components;

public partial class MermaidDecisionNetworkDiagram : IAsyncDisposable
{
    private ElementReference diagramHost;
    private IJSObjectReference? mermaidModule;
    private DotNetObjectReference<MermaidDecisionNetworkDiagram>? dotNetReference;
    private bool renderRequested = true;

    [Inject]
    private IJSRuntime JSRuntime { get; set; } = default!;

    [Parameter, EditorRequired]
    public DecisionNetworkDefinition Network { get; set; } = default!;

    [Parameter, EditorRequired]
    public DecisionResult Result { get; set; } = default!;

    private NetworkNode? SelectedNode { get; set; }

    private string? RenderKey { get; set; }

    private string SelectedNodeValue =>
        Result.Trace.FirstOrDefault(step => step.NodeId == SelectedNode?.Id)?.Value
        ?? "No calculated value";

    private IReadOnlyList<NetworkEdge> UpstreamEdges =>
        Network.Edges.Where(edge => edge.ToNodeId == SelectedNode?.Id).ToArray();

    private IReadOnlyList<NetworkEdge> DownstreamEdges =>
        Network.Edges.Where(edge => edge.FromNodeId == SelectedNode?.Id).ToArray();

    protected override void OnParametersSet()
    {
        var currentRenderKey = $"{Network.Id}|{string.Join('|', Result.Trace.Select(step => $"{step.NodeId}:{step.Value}"))}";
        if (!string.Equals(RenderKey, currentRenderKey, StringComparison.Ordinal))
        {
            renderRequested = true;
            RenderKey = currentRenderKey;
        }

        if (SelectedNode is not null && Network.Nodes.All(node => node.Id != SelectedNode.Id))
        {
            SelectedNode = null;
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            mermaidModule = await JSRuntime.InvokeAsync<IJSObjectReference>(
                "import",
                "./js/mermaid-interop.js");
            dotNetReference = DotNetObjectReference.Create(this);
        }

        if (!renderRequested || mermaidModule is null || dotNetReference is null)
        {
            return;
        }

        renderRequested = false;
        await mermaidModule.InvokeVoidAsync(
            "renderDecisionNetwork",
            diagramHost,
            MermaidDefinitionBuilder.Build(Network, Result),
            Network.Nodes.Select(node => node.Id).ToArray(),
            Network.Edges,
            SelectedNode?.Id,
            dotNetReference);
    }

    [JSInvokable]
    public async Task SelectNodeFromMermaid(string nodeId)
    {
        SelectedNode = Network.Nodes.FirstOrDefault(node => node.Id == nodeId);

        if (mermaidModule is not null && SelectedNode is not null)
        {
            await mermaidModule.InvokeVoidAsync(
                "highlightDecisionNetworkPath",
                diagramHost,
                SelectedNode.Id,
                Network.Edges);
        }

        await InvokeAsync(StateHasChanged);
    }

    private string NodeLabel(string nodeId) =>
        Network.Nodes.FirstOrDefault(node => node.Id == nodeId)?.Label ?? nodeId;

    public async ValueTask DisposeAsync()
    {
        if (mermaidModule is not null)
        {
            try
            {
                await mermaidModule.InvokeVoidAsync("disposeDecisionNetwork", diagramHost);
                await mermaidModule.DisposeAsync();
            }
            catch (JSDisconnectedException)
            {
                // The browser may already be gone during shutdown.
            }
        }

        dotNetReference?.Dispose();
    }
}
