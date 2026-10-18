using DesignPatterns.Core.Abstractions;
using DesignPatterns.Core.Models.Diagrams;
using DesignPatterns.Core.Rendering;
using FluentAssertions;

namespace DesignPatterns.UnitTests.Rendering;

/// <summary>Записывает все вызовы surface — «snapshot» примитивов отрисовки.</summary>
public sealed class FakeDiagramSurface : IDiagramSurface
{
    public List<string> Log { get; } = [];
    private readonly List<RectF> _rects = [];

    public IReadOnlyList<RectF> FilledRects => _rects;

    public void FillRoundedRect(RectF rect, float radius, uint colorArgb)
    {
        _rects.Add(rect);
        Log.Add($"fill-rect [{rect.X:F1},{rect.Y:F1},{rect.Width:F1},{rect.Height:F1}] r={radius:F0} #{colorArgb:X8}");
    }

    public void StrokeRoundedRect(RectF rect, float radius, uint colorArgb, float thickness)
        => Log.Add($"stroke-rect [{rect.X:F1},{rect.Y:F1},{rect.Width:F1},{rect.Height:F1}] t={thickness:F1}");

    public void FillText(RectF rect, string text, uint colorArgb, float fontSizePx, bool bold, bool center)
        => Log.Add($"text \"{text}\" @[{rect.X:F1},{rect.Y:F1}] size={fontSizePx:F0} bold={bold} center={center}");

    public void DrawArrow(PointF from, PointF to, uint colorArgb, float thickness, ArrowShape head)
        => Log.Add($"arrow ({from.X:F1},{from.Y:F1})→({to.X:F1},{to.Y:F1}) head={head}");

    public void DrawDiamond(PointF center, float width, float height, uint colorArgb, bool filled)
        => Log.Add($"diamond ({center.X:F1},{center.Y:F1}) {width:F1}x{height:F1} filled={filled}");

    public void DrawDashedLine(PointF from, PointF to, uint colorArgb, float thickness)
        => Log.Add($"dashed ({from.X:F1},{from.Y:F1})→({to.X:F1},{to.Y:F1})");

    public IEnumerable<string> With(string needle) => Log.Where(l => l.Contains(needle));
}

public class UmlLayoutEngineTests
{
    private static UmlDiagram TwoNodeDiagram() => new(
        "Test",
        [
            new UmlClassNode("base", "Base", UmlClassKind.AbstractClass, ["Run()"], 0, 0),
            new UmlClassNode("impl", "Concrete", UmlClassKind.Class, ["Run()", "Extra()"], 0, 1),
        ],
        [new UmlLink("impl", "base", UmlLinkKind.Inheritance)]);

    [Fact]
    public void Layout_PlacesNodesByColumnRowGrid()
    {
        var layout = UmlLayoutEngine.Layout(TwoNodeDiagram());

        layout.Boxes.Should().HaveCount(2);
        var baseBox = layout.Boxes.First(b => b.NodeId == "base");
        var implBox = layout.Boxes.First(b => b.NodeId == "impl");

        // Одинаковая колонка → одинаковый X.
        implBox.Bounds.X.Should().Be(baseBox.Bounds.X);
        // Строка ниже → Y строго больше нижней границы предыдущей.
        implBox.Bounds.Y.Should().BeGreaterThan(baseBox.Bounds.Bottom);
    }

    [Fact]
    public void Layout_NodeHeightDependsOnMemberCount()
    {
        var layout = UmlLayoutEngine.Layout(TwoNodeDiagram());

        var one = layout.Boxes.First(b => b.NodeId == "base");
        var two = layout.Boxes.First(b => b.NodeId == "impl");

        two.Bounds.Height.Should().Be(one.Bounds.Height + UmlLayoutEngine.MemberLineHeight);
    }

    [Fact]
    public void Layout_InheritanceLinkGoesFromSubclassUpToBaseBorder()
    {
        var layout = UmlLayoutEngine.Layout(TwoNodeDiagram());
        var link = layout.Links.Single();

        var baseBox = layout.Boxes.First(b => b.NodeId == "base");
        var implBox = layout.Boxes.First(b => b.NodeId == "impl");

        link.Kind.Should().Be(UmlLinkKind.Inheritance);
        // Идёт вверх: от верхней границы потомка к нижней границе базы.
        link.From.Y.Should().BeApproximately(implBox.Bounds.Y, 0.01f);
        link.To.Y.Should().BeApproximately(baseBox.Bounds.Bottom, 0.01f);
    }

    [Fact]
    public void Layout_SkipsLinkToUnknownNode()
    {
        var diagram = new UmlDiagram(
            "Broken",
            [new UmlClassNode("a", "A", UmlClassKind.Class, [], 0, 0)],
            [new UmlLink("a", "ghost", UmlLinkKind.Association)]);

        var layout = UmlLayoutEngine.Layout(diagram);

        layout.Links.Should().BeEmpty();
    }

    [Fact]
    public void Layout_EmptyDiagramProducesPositiveContentSize()
    {
        var layout = UmlLayoutEngine.Layout(UmlDiagram.Empty);

        layout.ContentSize.Width.Should().BePositive();
        layout.ContentSize.Height.Should().BePositive();
    }

    [Theory]
    [InlineData(UmlLinkKind.Inheritance)]
    [InlineData(UmlLinkKind.Composition)]
    [InlineData(UmlLinkKind.Dependency)]
    public void Measure_GrowsWithMoreRows(UmlLinkKind kind)
    {
        var single = new UmlDiagram("t", [new UmlClassNode("a", "A", UmlClassKind.Class, ["m"], 0, 0)], []);
        var double_ = single with
        {
            Nodes = [.. single.Nodes, new UmlClassNode("b", "B", UmlClassKind.Class, ["m"], 0, 1)],
            Links = [new UmlLink("b", "a", kind)],
        };

        UmlLayoutEngine.Layout(double_).ContentSize.Height
            .Should().BeGreaterThan(UmlLayoutEngine.Layout(single).ContentSize.Height);
    }
}

public class UmlDiagramRendererTests
{
    private static UmlDiagram SampleDiagram() => new(
        "Strategy",
        [
            new UmlClassNode("iface", "IStrategy", UmlClassKind.Interface, ["Execute()"], 0, 0),
            new UmlClassNode("conc", "ConcreteStrategy", UmlClassKind.Class, ["Execute()"], 0, 1),
            new UmlClassNode("ctx", "Context", UmlClassKind.Class, ["Strategy", "Run()"], 1, 1),
        ],
        [
            new UmlLink("conc", "iface", UmlLinkKind.Realization),
            new UmlLink("ctx", "iface", UmlLinkKind.Association, "uses"),
        ]);

    [Fact]
    public void Render_DrawsEveryNodeAndLink()
    {
        var surface = new FakeDiagramSurface();
        var renderer = new UmlDiagramRenderer();

        renderer.Render(SampleDiagram(), surface, 360f, 480f);

        // Три узла → три заливки + три обводки.
        surface.Log.Count(l => l.StartsWith("fill-rect")).Should().Be(3);
        surface.Log.Count(l => l.StartsWith("stroke-rect")).Should().Be(3);
        surface.With("head=HollowTriangle").Should().NotBeEmpty();   // realization
        surface.With("head=Line").Should().NotBeEmpty();             // association
        surface.With("\"uses\"").Should().NotBeEmpty();              // label
    }

    [Fact]
    public void Render_FitsInsideSmallCanvas_NothingOverflows()
    {
        var surface = new FakeDiagramSurface();
        var renderer = new UmlDiagramRenderer();
        const float w = 320f, h = 400f;

        renderer.Render(SampleDiagram(), surface, w, h);

        foreach (var rect in surface.FilledRects)
        {
            rect.X.Should().BeGreaterThanOrEqualTo(0f);
            rect.Y.Should().BeGreaterThanOrEqualTo(0f);
            rect.Right.Should().BeLessThanOrEqualTo(w + 0.01f);
            rect.Bottom.Should().BeLessThanOrEqualTo(h + 0.01f);
        }
    }

    [Fact]
    public void Render_ScalesLargerCanvasProportionally()
    {
        var small = new FakeDiagramSurface();
        var big = new FakeDiagramSurface();
        var renderer = new UmlDiagramRenderer();
        var diagram = SampleDiagram();

        renderer.Render(diagram, small, 320f, 400f);
        renderer.Render(diagram, big, 640f, 800f);

        var firstSmall = small.FilledRects[0];
        var firstBig = big.FilledRects[0];

        // В два раза большая область → узлы примерно вдвое шире (uniform scale).
        firstBig.Width.Should().BeApproximately(firstSmall.Width * 2f, 1f);
    }

    [Fact]
    public void Render_ZeroArea_DrawsNothing()
    {
        var surface = new FakeDiagramSurface();

        new UmlDiagramRenderer().Render(SampleDiagram(), surface, 0f, 0f);

        surface.Log.Should().BeEmpty();
    }

    [Fact]
    public void Render_EmptyDiagram_DrawsNothing()
    {
        var surface = new FakeDiagramSurface();

        new UmlDiagramRenderer().Render(UmlDiagram.Empty, surface, 300f, 300f);

        surface.Log.Should().BeEmpty();
    }

    [Fact]
    public void Render_InterfaceNode_HasStereotypeInTitle()
    {
        var surface = new FakeDiagramSurface();

        new UmlDiagramRenderer().Render(SampleDiagram(), surface, 360f, 480f);

        surface.With("«interface» IStrategy").Should().NotBeEmpty();
    }

    [Fact]
    public void Render_DependencyLink_IsDashed()
    {
        var diagram = new UmlDiagram(
            "dep",
            [
                new UmlClassNode("a", "A", UmlClassKind.Class, [], 0, 0),
                new UmlClassNode("b", "B", UmlClassKind.Class, [], 0, 1),
            ],
            [new UmlLink("a", "b", UmlLinkKind.Dependency)]);

        var surface = new FakeDiagramSurface();
        new UmlDiagramRenderer().Render(diagram, surface, 300f, 300f);

        surface.Log.Should().Contain(l => l.StartsWith("dashed"));
    }

    [Fact]
    public void Measure_ReturnsNaturalContentSize()
    {
        var renderer = new UmlDiagramRenderer();

        var size = renderer.Measure(SampleDiagram());

        size.Width.Should().BeGreaterThan(0);
        size.Height.Should().BeGreaterThan(0);
        // Две колонки → ширина минимум 2*NodeWidth + ColumnGap.
        size.Width.Should().BeGreaterThanOrEqualTo(2 * UmlLayoutEngine.NodeWidth + UmlLayoutEngine.ColumnGap);
    }
}
