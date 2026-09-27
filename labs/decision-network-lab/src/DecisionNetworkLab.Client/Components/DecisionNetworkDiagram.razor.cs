using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using DecisionNetworkLab.Core;

namespace DecisionNetworkLab.Client.Components;

public partial class DecisionNetworkDiagram
{
    private const int EvidenceWidth = 300;
    private const int EvidenceHeight = 140;
    private const int CircleRadiusX = 145;
    private const int CircleRadiusY = 92;
    private const int DecisionRadiusX = 145;
    private const int DecisionRadiusY = 78;

    private static readonly IReadOnlyDictionary<string, DiagramPosition> CarrotPositions =
        new Dictionary<string, DiagramPosition>
        {
            ["day"] = new(330, 105),
            ["opponent"] = new(1020, 105),
            ["market"] = new(185, 305),
            ["demand"] = new(520, 300),
            ["supply"] = new(1020, 300),
            ["price"] = new(690, 505),
            ["plant"] = new(520, 700),
            ["yield"] = new(1020, 700),
            ["utility"] = new(690, 835)
        };

    private static readonly IReadOnlyDictionary<string, DiagramPosition> SimpleCropPositions =
        new Dictionary<string, DiagramPosition>
        {
            ["day"] = new(300, 105),
            ["market"] = new(700, 105),
            ["care"] = new(1100, 105),
            ["price"] = new(700, 350),
            ["plant"] = new(500, 570),
            ["yield"] = new(1000, 570),
            ["utility"] = new(700, 790)
        };

    private NetworkNode? SelectedNode { get; set; }

    [Parameter]
    public DecisionNetworkDefinition Network { get; set; } = default!;

    private string TitleId => $"{Network.Id}-diagram-title";

    private string DescriptionId => $"{Network.Id}-diagram-description";

    private int SvgWidth => 1400;

    private int SvgHeight => 900;

    private IReadOnlyDictionary<string, DiagramPosition> Positions =>
        Network.Id == "carrot" ? CarrotPositions : SimpleCropPositions;

    private IReadOnlyList<NodeLayout> Layouts =>
        Network.Nodes
            .Select((node, index) =>
            {
                var position = GetPosition(node.Id, index);
                return new NodeLayout(node, position.CenterX, position.CenterY);
            })
            .ToArray();

    protected override void OnParametersSet()
    {
        if (SelectedNode is not null && Network.Nodes.All(node => node.Id != SelectedNode.Id))
        {
            SelectedNode = null;
        }
    }

    private void SelectNode(NetworkNode node)
    {
        SelectedNode = node;
    }

    private void HandleKeyDown(KeyboardEventArgs args, NetworkNode node)
    {
        if (args.Key is "Enter" or " ")
        {
            SelectNode(node);
        }
    }

    private DiagramPosition GetPosition(string nodeId, int fallbackIndex)
    {
        return Positions.TryGetValue(nodeId, out var position)
            ? position
            : new DiagramPosition(300 + (fallbackIndex % 3) * 400, 100 + (fallbackIndex / 3) * 220);
    }

    private EdgeLine? GetEdgeLine(NetworkEdge edge)
    {
        var from = Layouts.FirstOrDefault(layout => layout.Node.Id == edge.FromNodeId);
        var to = Layouts.FirstOrDefault(layout => layout.Node.Id == edge.ToNodeId);

        if (from is null || to is null)
        {
            return null;
        }

        var horizontal = Math.Abs(to.CenterX - from.CenterX) > Math.Abs(to.CenterY - from.CenterY);
        if (horizontal)
        {
            var direction = Math.Sign(to.CenterX - from.CenterX);
            return new EdgeLine(
                from.CenterX + direction * HalfWidth(from),
                from.CenterY,
                to.CenterX - direction * HalfWidth(to),
                to.CenterY);
        }

        var verticalDirection = Math.Sign(to.CenterY - from.CenterY);
        return new EdgeLine(
            from.CenterX,
            from.CenterY + verticalDirection * HalfHeight(from),
            to.CenterX,
            to.CenterY - verticalDirection * HalfHeight(to));
    }

    private static bool IsRectangular(NetworkNodeKind kind) =>
        kind is NetworkNodeKind.Evidence or NetworkNodeKind.Outcome;

    private static int HalfWidth(NodeLayout layout) =>
        IsRectangular(layout.Node.Kind) ? EvidenceWidth / 2 :
        layout.Node.Kind == NetworkNodeKind.Decision ? DecisionRadiusX : CircleRadiusX;

    private static int HalfHeight(NodeLayout layout) =>
        IsRectangular(layout.Node.Kind) ? EvidenceHeight / 2 :
        layout.Node.Kind == NetworkNodeKind.Decision ? DecisionRadiusY : CircleRadiusY;

    private static string DiamondPoints(NodeLayout layout)
    {
        return string.Join(",",
            $"{layout.CenterX},{layout.CenterY - DecisionRadiusY}",
            $"{layout.CenterX + DecisionRadiusX},{layout.CenterY}",
            $"{layout.CenterX},{layout.CenterY + DecisionRadiusY}",
            $"{layout.CenterX - DecisionRadiusX},{layout.CenterY}");
    }

    private static string NodeCssClass(NetworkNodeKind kind) =>
        $"uml-node-{kind.ToString().ToLowerInvariant()}";

    private static IReadOnlyList<TextLine> WrapText(string text, int maximumCharacters)
    {
        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var lines = new List<TextLine>();
        var current = string.Empty;

        foreach (var word in words)
        {
            var candidate = string.IsNullOrEmpty(current) ? word : $"{current} {word}";
            if (candidate.Length > maximumCharacters && !string.IsNullOrEmpty(current))
            {
                lines.Add(new TextLine(lines.Count, current));
                current = word;
            }
            else
            {
                current = candidate;
            }
        }

        if (!string.IsNullOrEmpty(current))
        {
            lines.Add(new TextLine(lines.Count, current));
        }

        return lines.Take(2).ToArray();
    }

    private sealed record DiagramPosition(int CenterX, int CenterY);

    private sealed record NodeLayout(NetworkNode Node, int CenterX, int CenterY);

    private sealed record EdgeLine(int X1, int Y1, int X2, int Y2);

    private sealed record TextLine(int Index, string Text);
}
