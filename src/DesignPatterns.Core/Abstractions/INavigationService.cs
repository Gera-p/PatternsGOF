namespace DesignPatterns.Core.Abstractions;

/// <summary>
/// Абстракция навигации. Реализуется в MAUI-слое поверх Shell,
/// тестируется через mock в unit-тестах ViewModels.
/// </summary>
public interface INavigationService
{
    /// <summary>Текущий маршрут (например, "pattern/abstract-factory").</summary>
    string CurrentRoute { get; }

    /// <summary>Переход к категории паттернов.</summary>
    Task NavigateToCategoryAsync(string categoryId, CancellationToken cancellationToken = default);

    /// <summary>Переход к детальной странице паттерна.</summary>
    Task NavigateToPatternAsync(string patternId, CancellationToken cancellationToken = default);

    /// <summary>Переход на страницу избранного.</summary>
    Task NavigateToFavoritesAsync(CancellationToken cancellationToken = default);

    /// <summary>Назад по стеку. Возвращает false, если стек пуст.</summary>
    Task<bool> GoBackAsync(CancellationToken cancellationToken = default);
}
