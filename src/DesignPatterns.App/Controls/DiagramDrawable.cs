using DesignPatterns.Core.Abstractions;
using DesignPatterns.Core.Models.Diagrams;

namespace DesignPatterns.App.Controls;

/// <summary>
/// IDrawable для GraphicsView: рисует текущую UmlDiagram через IDiagramRenderer.
/// Диаграмма отдаётся лениво (Func) — при смене паттерна пере-рисовывается
/// тем же экземпляром drawable (Invalidate на смену размера делает сам GraphicsView).
/// </summary>
public sealed class DiagramDrawable(IDiagramRenderer renderer, Func<UmlDiagram> diagramProvider) : IDrawable
{
    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        var diagram = diagramProvider();
        if (diagram.Nodes.Count == 0 || dirtyRect.Width <= 0 || dirtyRect.Height <= 0)
            return;

        var surface = new Services.MauiGraphicsDiagramSurface(canvas);
        // Core-геометрия конвертируется на границе слоёв; fit-масштаб делает рендерер.
        renderer.Render(diagram, surface, dirtyRect.Width, dirtyRect.Height);
    }
}
