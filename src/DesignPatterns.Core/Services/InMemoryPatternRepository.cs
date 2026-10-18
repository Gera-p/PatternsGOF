using DesignPatterns.Core.Abstractions;
using DesignPatterns.Core.Models.Catalog;

namespace DesignPatterns.Core.Services;

/// <summary>
/// In-memory репозиторий каталога. На Этапе 3 будет обёрнут/заменён
/// на JsonPatternRepository, читающий контент из MauiAsset.
/// </summary>
public sealed class InMemoryPatternRepository : IPatternRepository
{
    private readonly IReadOnlyList<PatternCategory> _categories;
    private readonly IReadOnlyList<PatternInfo> _patterns;

    public InMemoryPatternRepository(IEnumerable<PatternCategory> categories)
    {
        ArgumentNullException.ThrowIfNull(categories);
        _categories = categories.ToList();
        _patterns = _categories.SelectMany(c => c.Patterns).ToList();
    }

    public Task<IReadOnlyList<PatternCategory>> GetCategoriesAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(_categories);

    public Task<IReadOnlyList<PatternInfo>> GetAllPatternsAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(_patterns);

    public Task<PatternInfo?> FindByIdAsync(string patternId, CancellationToken cancellationToken = default)
        => Task.FromResult(_patterns.FirstOrDefault(p =>
            string.Equals(p.Id, patternId, StringComparison.OrdinalIgnoreCase)));

    public Task<IReadOnlyList<PatternInfo>> SearchAsync(string query, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
            return Task.FromResult<IReadOnlyList<PatternInfo>>(Array.Empty<PatternInfo>());

        var q = query.Trim();
        var result = _patterns
            .Where(p => Contains(p.Name.Ru, q) ||
                        Contains(p.Name.En, q) ||
                        Contains(p.Intent.Ru, q) ||
                        Contains(p.Intent.En, q) ||
                        Contains(p.Problem.Ru, q) ||
                        Contains(p.Problem.En, q))
            .ToList();

        return Task.FromResult<IReadOnlyList<PatternInfo>>(result);
    }

    private static bool Contains(string haystack, string needle)
        => haystack.Contains(needle, StringComparison.OrdinalIgnoreCase);
}

/// <summary>Простейшее in-memory хранилище избранного (MAUI-слой добавит персистентность).</summary>
public sealed class InMemoryFavoritesStore : IFavoritesStore
{
    private readonly HashSet<string> _ids;

    public InMemoryFavoritesStore(IEnumerable<string>? initial = null)
        => _ids = new HashSet<string>(initial ?? [], StringComparer.OrdinalIgnoreCase);

    public IReadOnlyCollection<string> FavoriteIds => _ids;

    public event EventHandler? Changed;

    public bool IsFavorite(string patternId) => _ids.Contains(patternId);

    public void Toggle(string patternId)
    {
        if (!_ids.Remove(patternId))
            _ids.Add(patternId);

        Changed?.Invoke(this, EventArgs.Empty);
    }
}
