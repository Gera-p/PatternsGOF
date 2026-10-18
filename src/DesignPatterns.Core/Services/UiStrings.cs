using DesignPatterns.Core.Abstractions;

namespace DesignPatterns.Core.Services;

/// <summary>
/// Словарь строк UI (RU — основной, EN). Используется DictionaryLocalizationService
/// в тестах и как источник ключей для resx в MAUI-слое.
/// </summary>
public static class UiStrings
{
    public static IReadOnlyDictionary<AppLanguage, IReadOnlyDictionary<string, string>> All { get; } =
        new Dictionary<AppLanguage, IReadOnlyDictionary<string, string>>
        {
            [AppLanguage.Russian] = Ru,
            [AppLanguage.English] = En,
        };

    /// <summary>Создаёт сервис локализации со строками UI (RU по умолчанию).</summary>
    public static ILocalizationService CreateDefaultService(AppLanguage initial = AppLanguage.Russian)
        => new DictionaryLocalizationService(All, initial);

    private static readonly Dictionary<string, string> Ru = new(StringComparer.Ordinal)
    {
        ["MainTitle"] = "Паттерны проектирования",
        ["SearchPlaceholder"] = "Поиск по названию или описанию…",
        ["FavoritesTitle"] = "Избранное",
        ["FavoritesEmpty"] = "Список избранного пуст",
        ["ProblemHeader"] = "Проблема",
        ["SolutionHeader"] = "Решение",
        ["ApplicabilityHeader"] = "Применимость",
        ["ProsHeader"] = "Плюсы",
        ["ConsHeader"] = "Минусы",
        ["CodeHeader"] = "Пример кода на C#",
        ["DiagramHeader"] = "UML-диаграмма",
        ["CreationalCategory"] = "Порождающие паттерны",
        ["StructuralCategory"] = "Структурные паттерны",
        ["BehavioralCategory"] = "Поведенческие паттерны",
    };

    private static readonly Dictionary<string, string> En = new(StringComparer.Ordinal)
    {
        ["MainTitle"] = "Design Patterns",
        ["SearchPlaceholder"] = "Search by name or description…",
        ["FavoritesTitle"] = "Favorites",
        ["FavoritesEmpty"] = "Favorites list is empty",
        ["ProblemHeader"] = "Problem",
        ["SolutionHeader"] = "Solution",
        ["ApplicabilityHeader"] = "Applicability",
        ["ProsHeader"] = "Pros",
        ["ConsHeader"] = "Cons",
        ["CodeHeader"] = "C# code example",
        ["DiagramHeader"] = "UML diagram",
        ["CreationalCategory"] = "Creational Patterns",
        ["StructuralCategory"] = "Structural Patterns",
        ["BehavioralCategory"] = "Behavioral Patterns",
    };
}
