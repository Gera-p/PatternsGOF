using DesignPatterns.Core.Models.Catalog;

namespace DesignPatterns.Core.Abstractions;

/// <summary>
/// Источник контента (каталога паттернов). В Этапе 3 реализуется
/// JSON-загрузчиком из MauiAsset; пока — in-memory репозиторий-заглушка.
/// </summary>
public interface IPatternRepository
{
    Task<IReadOnlyList<PatternCategory>> GetCategoriesAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PatternInfo>> GetAllPatternsAsync(CancellationToken cancellationToken = default);

    Task<PatternInfo?> FindByIdAsync(string patternId, CancellationToken cancellationToken = default);

    /// <summary>Поиск по названию и описанию (без учёта регистра, обе локали).</summary>
    Task<IReadOnlyList<PatternInfo>> SearchAsync(string query, CancellationToken cancellationToken = default);
}

/// <summary>Хранилище избранных паттернов (персистентность обеспечивает MAUI-слой).</summary>
public interface IFavoritesStore
{
    IReadOnlyCollection<string> FavoriteIds { get; }
    bool IsFavorite(string patternId);
    void Toggle(string patternId);
    event EventHandler? Changed;
}
