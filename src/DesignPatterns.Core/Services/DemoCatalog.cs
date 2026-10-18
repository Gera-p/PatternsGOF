using DesignPatterns.Core.Abstractions;
using DesignPatterns.Core.Models.Catalog;
using DesignPatterns.Core.Models.Diagrams;

namespace DesignPatterns.Core.Services;

/// <summary>
/// Демо-каталог для Этапов 1–2: три категории и по одному паттерну-заглушке.
/// Полный контент GoF (23+) появится на Этапе 4.
/// </summary>
public static class DemoCatalog
{
    public static IReadOnlyList<PatternCategory> Build() =>
    [
        new PatternCategory(
            "creational",
            new LocString("Порождающие паттерны", "Creational Patterns"),
            new LocString("Абстрагируют процесс создания объектов.", "Abstract the object creation process."),
            [DemoPattern("abstract-factory", "creational", "Абстрактная фабрика", "Abstract Factory")]),

        new PatternCategory(
            "structural",
            new LocString("Структурные паттерны", "Structural Patterns"),
            new LocString("Компонуют объекты в крупные структуры.", "Compose objects into larger structures."),
            [DemoPattern("adapter", "structural", "Адаптер", "Adapter")]),

        new PatternCategory(
            "behavioral",
            new LocString("Поведенческие паттерны", "Behavioral Patterns"),
            new LocString("Распределяют ответственность между объектами.", "Distribute responsibility between objects."),
            [DemoPattern("strategy", "behavioral", "Стратегия", "Strategy")]),
    ];

    private static PatternInfo DemoPattern(string id, string categoryId, string ru, string en) =>
        new(
            id,
            categoryId,
            new LocString(ru, en),
            new LocString("Демо-назначение (Этап 2).", "Demo intent (Stage 2)."),
            new LocString("Демо-проблема (Этап 2).", "Demo problem (Stage 2)."),
            new LocString("Демо-решение (Этап 2).", "Demo solution (Stage 2)."),
            new LocString("Демо-применимость (Этап 2).", "Demo applicability (Stage 2)."),
            [new LocString("+ Демо", "+ Demo")],
            [new LocString("- Демо", "- Demo")],
            new UmlDiagram(
                en,
                [
                    new UmlClassNode("a", "Base", UmlClassKind.AbstractClass, ["Run()"], 0, 0),
                    new UmlClassNode("b", "Concrete", UmlClassKind.Class, ["Run()"], 0, 1),
                ],
                [new UmlLink("b", "a", UmlLinkKind.Inheritance)]),
            new CodeExample("Console demo", "csharp", "Console.WriteLine(\"TODO: Stage 4\");"));
}
