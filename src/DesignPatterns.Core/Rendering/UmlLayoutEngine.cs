using DesignPatterns.Core.Models.Diagrams;

namespace DesignPatterns.Core.Rendering;

/// <summary>Прямоугольник узла с id (результат layout).</summary>
public sealed record NodeBox(string NodeId, RectF Bounds);

/// <summary>Конечные точки связи (по границам узлов, не по центрам).</summary>
public sealed record LinkGeometry(
    string FromNodeId,
    string ToNodeId,
    UmlLinkKind Kind,
    string? Label,
    PointF From,
    PointF To);

/// <summary>Полный результат раскладки диаграммы в «естественных» координатах.</summary>
public sealed record DiagramLayout(
    IReadOnlyList<NodeBox> Boxes,
    IReadOnlyList<LinkGeometry> Links,
    SizeF ContentSize);

/// <summary>
/// Чистый (не зависящий от графики) layout-движок UML-диаграмм.
/// Узлы размещаются по Column/Row сетке; высота узла считается по числу строк,
/// связи режутся по границам прямоугольников. Mobile-first: fit-scale выполняет
/// сам рендерер, здесь — только естественные координаты.
/// </summary>
public static class UmlLayoutEngine
{
    public const float NodeWidth = 170f;
    public const float TitleHeight = 34f;
    public const float MemberLineHeight = 18f;
    public const float MembersPadding = 10f;
    public const float ColumnGap = 60f;
    public const float RowGap = 44f;
    public const float Margin = 16f;

    /// <summary>Выполняет раскладку всей диаграммы.</summary>
    public static DiagramLayout Layout(UmlDiagram diagram)
    {
        ArgumentNullException.ThrowIfNull(diagram);

        var maxCols = diagram.Nodes.Count == 0 ? 0 : diagram.Nodes.Max(n => n.Column);
        var maxRows = diagram.Nodes.Count == 0 ? 0 : diagram.Nodes.Max(n => n.Row);

        // Высота каждой строки сетки = максимум высот узлов в этой строке.
        var rowHeights = new Dictionary<int, float>();
        for (var r = 0; r <= maxRows; r++)
        {
            var nodesInRow = diagram.Nodes.Where(n => n.Row == r).ToList();
            rowHeights[r] = nodesInRow.Count == 0
                ? TitleHeight + MembersPadding
                : nodesInRow.Max(MeasureNodeHeight);
        }

        // Y-координата начала каждой строки сетки.
        var rowY = new Dictionary<int, float>();
        var y = Margin;
        for (var r = 0; r <= maxRows; r++)
        {
            rowY[r] = y;
            y += rowHeights[r] + RowGap;
        }

        var boxes = new List<NodeBox>();
        foreach (var node in diagram.Nodes)
        {
            var height = MeasureNodeHeight(node);
            var x = Margin + node.Column * (NodeWidth + ColumnGap);
            // Узел прижимается к верху своей строки сетки.
            boxes.Add(new NodeBox(node.Id, new RectF(x, rowY[node.Row], NodeWidth, height)));
        }

        var links = new List<LinkGeometry>();
        foreach (var link in diagram.Links)
        {
            var from = boxes.FirstOrDefault(b => b.NodeId == link.FromNodeId);
            var to = boxes.FirstOrDefault(b => b.NodeId == link.ToNodeId);
            if (from is null || to is null)
                continue; // связь в несуществующий узел игнорируется, а не роняет рендер

            var (start, end) = RouteEdge(from.Bounds, to.Bounds);
            links.Add(new LinkGeometry(link.FromNodeId, link.ToNodeId, link.Kind, link.Label, start, end));
        }

        var contentWidth = Margin * 2 + (maxCols + 1) * NodeWidth + maxCols * ColumnGap;
        var contentHeight = rowY[maxRows] + rowHeights[maxRows] + Margin;

        return new DiagramLayout(boxes, links, new SizeF(contentWidth, contentHeight));
    }

    /// <summary>Высота узла: заголовок + строки членов (минимум одна пустая секция).</summary>
    public static float MeasureNodeHeight(UmlClassNode node)
    {
        var members = node.Members.Count == 0 ? 1 : node.Members.Count;
        return TitleHeight + members * MemberLineHeight + MembersPadding;
    }

    /// <summary>
    /// Точки связи на периметрах двух прямоугольников: если узлы смещены
    /// преимущественно по вертикали — соединяем нижнюю/верхнюю стороны,
    /// иначе — левую/правую. Стрелка рисуется от подкласса к суперклассу.
    /// </summary>
    internal static (PointF Start, PointF End) RouteEdge(RectF from, RectF to)
    {
        var fc = from.Center;
        var tc = to.Center;
        var dx = Math.Abs(tc.X - fc.X);
        var dy = Math.Abs(tc.Y - fc.Y);

        if (dy >= dx)
        {
            var goingDown = tc.Y > fc.Y;
            var start = new PointF(fc.X, goingDown ? from.Bottom : from.Y);
            var end = new PointF(tc.X, goingDown ? to.Y : to.Bottom);
            return (start, end);
        }
        else
        {
            var goingRight = tc.X > fc.X;
            var start = new PointF(goingRight ? from.Right : from.X, fc.Y);
            var end = new PointF(goingRight ? to.X : to.Right, tc.Y);
            return (start, end);
        }
    }
}
