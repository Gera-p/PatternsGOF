using DesignPatterns.Core.Abstractions;

namespace DesignPatterns.Core.Models.Catalog;

/// <summary>Категория GoF: Creational / Structural / Behavioral.</summary>
public sealed record PatternCategory(
    string Id,
    LocString Name,
    LocString Description,
    IReadOnlyList<PatternInfo> Patterns);

/// <summary>Карточка паттерна (полная).</summary>
public sealed record PatternInfo(
    string Id,
    string CategoryId,
    LocString Name,
    LocString Intent,
    LocString Problem,
    LocString Solution,
    LocString Applicability,
    IReadOnlyList<LocString> Pros,
    IReadOnlyList<LocString> Cons,
    UmlDiagram Diagram,
    CodeExample Example);

/// <summary>Локализованная строка RU + EN.</summary>
public sealed record LocString(string Ru, string En)
{
    public string Get(AppLanguage language) => language == AppLanguage.English ? En : Ru;
}

/// <summary>Канонический консольный пример на C#.</summary>
public sealed record CodeExample(
    string Title,
    string Language,
    string SourceCode);
