namespace DesignPatterns.Core.Models.Diagrams;

/// <summary>
/// Модель UML-диаграммы: классы + связи + сетка layout.
/// Рендерер адаптирует её под любой экран (fit-scale).
/// </summary>
public sealed record UmlDiagram(
    string Title,
    IReadOnlyList<UmlClassNode> Nodes,
    IReadOnlyList<UmlLink> Links)
{
    public static readonly UmlDiagram Empty = new(string.Empty, [], []);
}

/// <summary>Вид класса на диаграмме.</summary>
public enum UmlClassKind
{
    Class,
    AbstractClass,
    Interface,
    Enum,
}

/// <summary>Узел — класс/интерфейс с полями и методами.</summary>
public sealed record UmlClassNode(
    string Id,
    string Name,
    UmlClassKind Kind,
    IReadOnlyList<string> Members,
    int Column,
    int Row);

/// <summary>Тип UML-связи.</summary>
public enum UmlLinkKind
{
    /// <summary>Наследование (solid + hollow triangle).</summary>
    Inheritance,

    /// <summary>Реализация интерфейса (dashed + hollow triangle).</summary>
    Realization,

    /// <summary>Ассоциация.</summary>
    Association,

    /// <summary>Агрегация.</summary>
    Aggregation,

    /// <summary>Композиция.</summary>
    Composition,

    /// <summary>Зависимость (dashed arrow).</summary>
    Dependency,
}

/// <summary>Связь между двумя узлами.</summary>
public sealed record UmlLink(
    string FromNodeId,
    string ToNodeId,
    UmlLinkKind Kind,
    string? Label = null);
