using DesignPatterns.Core.Abstractions;
using DesignPatterns.Core.Models.Diagrams;

namespace DesignPatterns.Core.Rendering;

/// <summary>
/// Реализация <see cref="IDiagramRenderer"/> поверх абстрактного <see cref="IDiagramSurface"/>.
/// Полностью чистая (не зависит от MAUI/Skia) — тестируется fake-поверхностью.
/// Алгоритм: layout (UmlLayoutEngine) → uniform fit-scale в заданную область → отрисовка.
/// Диаграмма никогда не обрезается: если она «выше» области, масштаб берётся по min(w,h),
/// а высота подстраивается через возвращаемый aspect ratio (см. <see cref="Measure"/>).
/// </summary>
public sealed class UmlDiagramRenderer : IDiagramRenderer
{
    // Палитра (ARGB uint, как в Microsoft.Maui.Graphics Color.ToUint32).
    public const uint ColorClassFill = 0xFFFFF8DC;      // светлое «слоновая кость»
    public const uint ColorAbstractFill = 0xFFE6F0FA;  // голубоватое для abstract
    public const uint ColorInterfaceFill = 0xFFEAF7EA; // зеленоватое для interface
    public const uint ColorBorder = 0xFF444A50;
    public const uint ColorText = 0xFF1B1E22;
    public const uint ColorLink = 0xFF5A6570;
    public const uint ColorTitle = 0xFF2C3E50;

    private const float TitleFontSize = 13f;
    private const float MemberFontSize = 11f;
    private const float LinkThickness = 1.5f;

    public SizeF Measure(UmlDiagram diagram) => UmlLayoutEngine.Layout(diagram).ContentSize;

    public void Render(UmlDiagram diagram, IDiagramSurface surface, float width, float height)
    {
        ArgumentNullException.ThrowIfNull(diagram);
        ArgumentNullException.ThrowIfNull(surface);

        if (width <= 0 || height <= 0 || diagram.Nodes.Count == 0)
            return; // пустая диаграмма или невидимая область — ничего не рисуем

        var layout = UmlLayoutEngine.Layout(diagram);
        var content = layout.ContentSize;

        // Uniform scale с запасом, чтобы тонкие линии не «слипались» на маленьком экране.
        var scale = Math.Min(width / content.Width, height / content.Height);
        var offsetX = (width - content.Width * scale) / 2f;
        var offsetY = (height - content.Height * scale) / 2f;

        var nodesById = diagram.Nodes.ToDictionary(n => n.Id);

        foreach (var box in layout.Boxes)
        {
            var node = nodesById[box.NodeId];
            DrawNode(surface, node, ScaleRect(box.Bounds, scale, offsetX, offsetY));
        }

        foreach (var link in layout.Links)
        {
            var from = Transform(link.From, scale, offsetX, offsetY);
            var to = Transform(link.To, scale, offsetX, offsetY);
            DrawLink(surface, link, from, to);
        }
    }

    private static void DrawNode(IDiagramSurface surface, UmlClassNode node, RectF r)
    {
        var fill = node.Kind switch
        {
            UmlClassKind.AbstractClass => ColorAbstractFill,
            UmlClassKind.Interface => ColorInterfaceFill,
            _ => ColorClassFill,
        };

        surface.FillRoundedRect(r, 6f, fill);
        surface.StrokeRoundedRect(r, 6f, ColorBorder, 1.5f);

        // Стереотип над именем для interface/abstract — компактно, в одну строку заголовка.
        var title = node.Kind switch
        {
            UmlClassKind.Interface => $"\u00ABinterface\u00BB {node.Name}",
            UmlClassKind.AbstractClass => $"\u00ABabstract\u00BB {node.Name}",
            UmlClassKind.Enum => $"\u00ABenum\u00BB {node.Name}",
            _ => node.Name,
        };

        // Заголовок занимает фиксированную долю высоты узла (масштаб уже применён).
        var naturalHeight = UmlLayoutEngine.MeasureNodeHeight(node);
        var headerFraction = Math.Clamp(UmlLayoutEngine.TitleHeight / naturalHeight, 0.2f, 0.6f);
        var titleRect = new RectF(r.X, r.Y, r.Width, r.Height * headerFraction);

        surface.FillText(titleRect, title, ColorTitle, TitleFontSize, bold: true, center: true);

        if (node.Members.Count == 0)
            return;

        var membersRect = new RectF(
            r.X + 4f,
            titleRect.Bottom,
            r.Width - 8f,
            Math.Max(r.Height - titleRect.Height - 4f, 0f));

        var text = string.Join("\n", node.Members);
        surface.FillText(membersRect, text, ColorText, MemberFontSize, bold: false, center: false);
    }

    private static void DrawLink(
        IDiagramSurface surface,
        LinkGeometry link,
        PointF from,
        PointF to)
    {
        var dashed = link.Kind is UmlLinkKind.Realization or UmlLinkKind.Dependency;

        var head = link.Kind switch
        {
            UmlLinkKind.Inheritance => ArrowShape.HollowTriangle,
            UmlLinkKind.Realization => ArrowShape.HollowTriangle,
            UmlLinkKind.Aggregation => ArrowShape.OpenDiamond,
            UmlLinkKind.Composition => ArrowShape.FilledDiamond,
            UmlLinkKind.Dependency => ArrowShape.OpenArrow,
            _ => ArrowShape.Line,
        };

        // Один вызов DrawArrow рисует и линию, и наконечник нужной формы.
        surface.DrawArrow(from, to, ColorLink, LinkThickness, head);

        if (dashed)
            surface.DrawDashedLine(from, to, ColorLink, LinkThickness);

        if (!string.IsNullOrEmpty(link.Label))
        {
            var mid = new PointF((from.X + to.X) / 2f, (from.Y + to.Y) / 2f);
            var labelRect = new RectF(mid.X - 40f, mid.Y - 8f, 80f, 16f);
            surface.FillText(labelRect, link.Label, ColorLink, MemberFontSize, bold: false, center: true);
        }
    }

    private static RectF ScaleRect(RectF r, float scale, float dx, float dy)
        => new(r.X * scale + dx, r.Y * scale + dy, r.Width * scale, r.Height * scale);

    private static PointF Transform(PointF p, float scale, float dx, float dy)
        => new(p.X * scale + dx, p.Y * scale + dy);
}
