using DesignPatterns.Core.Abstractions;
using DesignPatterns.Core.Models.Diagrams;
using Microsoft.Maui.Graphics;

namespace DesignPatterns.App.Services;

/// <summary>
/// Production-поверхность: адаптер <see cref="IDiagramSurface"/> над Maui.Graphics ICanvas.
/// Единственное место, где Core-абстракция «касается» реальной графики MAUI.
/// </summary>
public sealed class MauiGraphicsDiagramSurface(ICanvas canvas) : IDiagramSurface
{
    public void FillRoundedRect(RectF rect, float radius, uint colorArgb)
    {
        canvas.FillColor = FromArgb(colorArgb);
        canvas.FillRoundRectangle(ToMaui(rect), radius);
    }

    public void StrokeRoundedRect(RectF rect, float radius, uint colorArgb, float thickness)
    {
        canvas.StrokeColor = FromArgb(colorArgb);
        canvas.StrokeLineThickness = thickness;
        canvas.DrawRoundRectangle(ToMaui(rect), radius);
    }

    public void FillText(RectF rect, string text, uint colorArgb, float fontSizePx, bool bold, bool center)
    {
        if (string.IsNullOrEmpty(text))
            return;

        canvas.FillColor = FromArgb(colorArgb);
        canvas.FontSize = Math.Max(fontSizePx, 6f);
        canvas.FontAttributes = bold ? FontAttributes.Bold : FontAttributes.None;

        var alignment = center ? TextAlignment.Center : TextAlignment.Left;
        var mrect = ToMaui(rect);

        // Многострочный текст внутри секции класса; вертикально — по верху блока.
        canvas.DrawString(text, mrect, VerticalAlignment.Top, alignment, wrap: true);
    }

    public void DrawArrow(PointF from, PointF to, uint colorArgb, float thickness, ArrowShape head)
    {
        var color = FromArgb(colorArgb);
        canvas.StrokeColor = color;
        canvas.StrokeLineThickness = thickness;
        canvas.DrawLine(from.X, from.Y, to.X, to.Y);

        var angle = MathF.Atan2(to.Y - from.Y, to.X - from.X);
        const float size = 9f;

        switch (head)
        {
            case ArrowShape.HollowTriangle:
            case ArrowShape.FilledTriangle:
                DrawTriangleHead(to, angle, size, filled: head == ArrowShape.FilledTriangle, color);
                break;

            case ArrowShape.OpenDiamond:
            case ArrowShape.FilledDiamond:
                DrawDiamondHead(to, angle, size, filled: head == ArrowShape.FilledDiamond, color);
                break;

            case ArrowShape.OpenArrow:
                DrawChevronHead(to, angle, size, color);
                break;

            case ArrowShape.Line:
            default:
                break; // чистая линия — наконечник не нужен
        }
    }

    public void DrawDiamond(PointF center, float width, float height, uint colorArgb, bool filled)
    {
        var path = BuildDiamondPath(center, width, height);
        if (filled)
        {
            canvas.FillColor = FromArgb(colorArgb);
            canvas.FillPath(path);
        }
        else
        {
            canvas.StrokeColor = FromArgb(colorArgb);
            canvas.DrawPath(path);
        }
    }

    public void DrawDashedLine(PointF from, PointF to, uint colorArgb, float thickness)
    {
        canvas.StrokeColor = FromArgb(colorArgb);
        canvas.StrokeLineThickness = thickness;
        canvas.StrokeDashPattern = [4f, 3f];
        canvas.DrawLine(from.X, from.Y, to.X, to.Y);
        canvas.StrokeDashPattern = [];
    }

    private void DrawTriangleHead(PointF tip, float angle, float size, bool filled, Color color)
    {
        // Треугольник, «смотрящий» вдоль линии и заканчивающийся в точке tip.
        var left = new PointF(
            tip.X - size * MathF.Cos(angle - 0.42f),
            tip.Y - size * MathF.Sin(angle - 0.42f));
        var right = new PointF(
            tip.X - size * MathF.Cos(angle + 0.42f),
            tip.Y - size * MathF.Sin(angle + 0.42f));

        if (filled)
        {
            canvas.FillColor = color;
            canvas.FillCircle(tip, 0.5f);
        }
        else
        {
            canvas.StrokeColor = color;
            canvas.BeginPath();
            canvas.MoveTo(left.X, left.Y);
            canvas.LineTo(tip.X, tip.Y);
            canvas.LineTo(right.X, right.Y);
            canvas.ClosePath();
            // Пустый треугольник UML: обводим белым фоном, затем контуром.
            canvas.FillColor = Colors.White;
            canvas.FillPath();
            canvas.DrawPath();
        }
    }

    private void DrawDiamondHead(PointF tip, float angle, float size, bool filled, Color color)
    {
        var center = new PointF(
            tip.X - size * MathF.Cos(angle),
            tip.Y - size * MathF.Sin(angle));
        var path = BuildDiamondPath(center, size * 1.6f, size);

        if (filled)
        {
            canvas.FillColor = color;
            canvas.FillPath(path);
        }
        else
        {
            canvas.FillColor = Colors.White;
            canvas.FillPath(path);
            canvas.StrokeColor = color;
            canvas.DrawPath(path);
        }
    }

    private void DrawChevronHead(PointF tip, float angle, float size, Color color)
    {
        canvas.StrokeColor = color;
        canvas.BeginPath();
        canvas.MoveTo(tip.X - size * MathF.Cos(angle - 0.5f), tip.Y - size * MathF.Sin(angle - 0.5f));
        canvas.LineTo(tip.X, tip.Y);
        canvas.LineTo(tip.X - size * MathF.Cos(angle + 0.5f), tip.Y - size * MathF.Sin(angle + 0.5f));
        canvas.StrokePath();
    }

    private static GraphicsPath BuildDiamondPath(PointF center, float width, float height)
    {
        var path = new GraphicsPath();
        var halfW = width / 2f;
        var halfH = height / 2f;
        path.BeginFigure();
        path.MoveTo(center.X, center.Y - halfH);
        path.LineTo(center.X + halfW, center.Y);
        path.LineTo(center.X, center.Y + halfH);
        path.LineTo(center.X - halfW, center.Y);
        path.CloseFigure();
        return path;
    }

    private static Microsoft.Maui.Graphics.RectF ToMaui(RectF r)
        => new(r.X, r.Y, r.Width, r.Height);

    private static Color FromArgb(uint argb)
    {
        var a = (argb >> 24 & 0xFF) / 255f;
        var r = (argb >> 16 & 0xFF) / 255f;
        var g = (argb >> 8 & 0xFF) / 255f;
        var b = (argb & 0xFF) / 255f;
        return Color.FromArgb(a, r, g, b);
    }
}
