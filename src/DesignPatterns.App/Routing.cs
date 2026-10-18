using DesignPatterns.Core.Abstractions;

namespace DesignPatterns.App;

public static class Routing
{
    public const string Main = "main";
    public const string Category = "category";
    public const string Pattern = "pattern";
    public const string Favorites = "favorites";

    public static string CategoryUri(string categoryId) => $"{Category}?id={Uri.EscapeDataString(categoryId)}";

    public static string PatternUri(string patternId) => $"{Pattern}?id={Uri.EscapeDataString(patternId)}";
}

/// <summary>Реализация <see cref="INavigationService"/> поверх Shell.GoToAsync с query-параметрами.</summary>
public sealed class ShellNavigationService : INavigationService
{
    public string CurrentRoute { get; private set; } = Routing.Main;

    public Task NavigateToCategoryAsync(string categoryId, CancellationToken cancellationToken = default)
    {
        CurrentRoute = Routing.Category;
        // id передаётся через QueryProperty целевой страницы (см. CategoryPage).
        return Shell.Current.GoToAsync(Routing.CategoryUri(categoryId));
    }

    public Task NavigateToPatternAsync(string patternId, CancellationToken cancellationToken = default)
    {
        CurrentRoute = Routing.Pattern;
        return Shell.Current.GoToAsync(Routing.PatternUri(patternId));
    }

    public Task NavigateToFavoritesAsync(CancellationToken cancellationToken = default)
    {
        CurrentRoute = Routing.Favorites;
        return Shell.Current.GoToAsync(Routing.Favorites);
    }

    public async Task<bool> GoBackAsync(CancellationToken cancellationToken = default)
    {
        if (Shell.Current.Navigation.NavigationStack.Count <= 1)
            return false;

        await Shell.Current.GoToAsync("..");
        return true;
    }
}
