using DesignPatterns.Core.Models.Diagrams;

namespace DesignPatterns.Core.Abstractions;

/// <summary>
/// Абстракция векторного рендерера UML-диаграмм.
/// Принимает модель диаграммы и рисует её на абстрактном canvas (Maui.Graphics / SkiaSharp).
/// Диаграмма обязана масштабироваться под доступную область (mobile-first).
/// </summary>
public interface IDiagramRenderer
{
    /// <summary>
    /// Рисует диаграмму в заданной области <paramref name="surface"/>.
    /// Реализация сама выполняет layout и fit-масштабирование.
    /// </summary>
    void Render(UmlDiagram diagram, IDiagramSurface surface, float width, float height);

    /// <summary>
    /// Вычисляет естественный размер диаграммы (до масштабирования) —
    /// используется для расчёта aspect ratio и скролла.
    /// </summary>
    SizeF Measure(UmlDiagram diagram);
}

/// <summary>
/// Минимальная абстракция «холста», чтобы Core не зависел от конкретной графики.
/// Реализации: MauiGraphicsDiagramSurface (production), FakeDiagramSurface (тесты).
/// </summary>
public interface IDiagramSurface
{
    void FillRoundedRect(RectF rect, float radius, uint colorArgb);
    void StrokeRoundedRect(RectF rect, float radius, uint colorArgb, float thickness);
    void FillText(RectF rect, string text, uint colorArgb, float fontSizePx, bool bold, bool center);
    void DrawArrow(PointF from, PointF to, uint colorArgb, float thickness, ArrowShape head);
    void DrawDiamond(PointF center, float width, float height, uint colorArgb, bool filled);
    void DrawDashedLine(PointF from, PointF to, uint colorArgb, float thickness);
}
