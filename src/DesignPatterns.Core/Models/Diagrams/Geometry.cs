using DesignPatterns.Core.Abstractions;

namespace DesignPatterns.Core.Models.Diagrams;

/// <summary>Простейшая геометрия (не зависим от System.Drawing / Microsoft.Maui.Graphics).</summary>
public readonly record struct PointF(float X, float Y);

/// <summary>Простейший размер.</summary>
public readonly record struct SizeF(float Width, float Height);

/// <summary>Прямоугольная область.</summary>
public readonly record struct RectF(float X, float Y, float Width, float Height)
{
    public float Right => X + Width;
    public float Bottom => Y + Height;
    public PointF Center => new(X + Width / 2f, Y + Height / 2f);
}

/// <summary>Форма стрелки UML-связи.</summary>
public enum ArrowShape
{
    /// <summary>Пустый треугольник — наследование (Generalization).</summary>
    HollowTriangle,

    /// <summary>Залитый треугольник — реализация интерфейса.</summary>
    FilledTriangle,

    /// <summary>Пустая ромбовидная — агрегация.</summary>
    OpenDiamond,

    /// <summary>Залитая ромбовидная — композиция.</summary>
    FilledDiamond,

    /// <summary>Простая линия — ассоциация.</summary>
    Line,

    /// <summary>Открытая стрелка — зависимость/использование.</summary>
    OpenArrow,
}
