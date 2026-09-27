using System.Text;
using DecisionNetworkLab.Core;

namespace DecisionNetworkLab.Client.Components;

/// <summary>
/// Converts one core network definition into the Mermaid flowchart syntax.
/// The core model remains the source of truth; Mermaid only lays it out.
/// </summary>
public static class MermaidDefinitionBuilder
{
    public static string Build(DecisionNetworkDefinition network, DecisionResult result)
    {
        var definition = new StringBuilder()
            .AppendLine("flowchart TB")
            .AppendLine("    classDef evidence fill:#183444,stroke:#5fa4c9,color:#e7edf2")
            .AppendLine("    classDef chance fill:#40351f,stroke:#d0a455,color:#e7edf2")
            .AppendLine("    classDef decision fill:#1d3b2d,stroke:#70b17e,color:#e7edf2")
            .AppendLine("    classDef outcome fill:#342944,stroke:#ae86cb,color:#e7edf2")
            .AppendLine("    classDef utility fill:#482b2a,stroke:#d47e6e,color:#e7edf2");

        foreach (var node in network.Nodes)
        {
            var id = ToMermaidId(node.Id);
            var currentValue = result.Trace
                .FirstOrDefault(step => step.NodeId == node.Id)?.Value
                ?? "No current value";
            var label = $"{EscapeText(node.Label)}<br/><span class='mermaid-node-value'>{EscapeText(currentValue)}</span>";
            var shape = node.Kind switch
            {
                NetworkNodeKind.Chance => $"(({label}))",
                NetworkNodeKind.Decision => $"{{{label}}}",
                NetworkNodeKind.Utility => $"{{{{{label}}}}}",
                _ => $"[{label}]"
            };

            definition.AppendLine($"    {id}{shape}");
            definition.AppendLine($"    class {id} {node.Kind.ToString().ToLowerInvariant()}");
        }

        foreach (var edge in network.Edges)
        {
            definition.AppendLine($"    {ToMermaidId(edge.FromNodeId)} --> {ToMermaidId(edge.ToNodeId)}");
        }

        foreach (var node in network.Nodes)
        {
            var id = ToMermaidId(node.Id);
            definition.AppendLine($"    click {id} call mermaidDecisionNetworkNodeClick() \"Select {EscapeText(node.Label)}\"");
        }

        return definition.ToString();
    }

    private static string ToMermaidId(string id) => id.Replace('-', '_');

    private static string EscapeText(string text) =>
        text.Replace("&", "&amp;")
            .Replace("\"", "'")
            .Replace("[", "(")
            .Replace("]", ")");
}
