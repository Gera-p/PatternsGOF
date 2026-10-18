using DesignPatterns.Core.Abstractions;

namespace DesignPatterns.App;

public static class Routing
{
    public const string Main = "main";
    public const string Category = "category";
    public const string Pattern = "pattern";
    public const string Favorites = "favorites";

    public static string PatternUri(string patternId) => $"{Pattern}/{patternId}";
}

/// <summary>Реализация <see cref="INavigationService"/> поверх Shell.GoToAsync.</summary>
public sealed class ShellNavigationService : INavigationService
{
    public string CurrentRoute { get; private set; } = Routing.Main;

    public Task NavigateToCategoryAsync(string categoryId, CancellationToken cancellationToken = default)
    {
        CurrentRoute = $"{Routing.Category}/{categoryId}";
        return Shell.Current.GoToAsync(CurrentRoute);
    }

    public Task NavigateToPatternAsync(string patternId, CancellationToken cancellationToken = default)
    {
        CurrentRoute = Routing.PatternUri(patternId);
        return Shell.Current.GoToAsync(CurrentRoute);
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
