using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DesignPatterns.Core.Abstractions;

namespace DesignPatterns.Core.ViewModels;

/// <summary>Главная страница: категории + поиск + избранный + переключение языка.</summary>
public partial class MainPageViewModel : ObservableObject
{
    private readonly IPatternRepository _repository;
    private readonly INavigationService _navigation;
    private readonly ILocalizationService _localization;

    public MainPageViewModel(
        IPatternRepository repository,
        INavigationService navigation,
        ILocalizationService localization)
    {
        _repository = repository;
        _navigation = navigation;
        _localization = localization;
        _localization.LanguageChanged += (_, _) => OnLanguageChanged();
    }

    /// <summary>Карточки категорий (уже локализованы под текущий язык).</summary>
    public IReadOnlyList<CategoryCardItem> Categories { get; private set; } = [];

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    [ObservableProperty]
    private bool _isSearching;

    /// <summary>Результаты поиска по названию/описанию (локализованы).</summary>
    public IReadOnlyList<PatternCardItem> SearchResults { get; private set; } = [];

    // ── Локализуемые строки UI (обновляются по LanguageChanged) ────────────────
    public string Title => _localization.GetString("MainTitle");
    public string SearchPlaceholder => _localization.GetString("SearchPlaceholder");

    /// <summary>Кнопка показывает ЯЗЫК, НА который переключаемся.</summary>
    public string LanguageButtonText =>
        _localization.CurrentLanguage == AppLanguage.Russian ? "EN" : "RU";

    public string FavoritesLabel => _localization.GetString("FavoritesTitle");

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        await RebuildCategoriesAsync(cancellationToken);
        await RebuildSearchResultsAsync(cancellationToken);
        NotifyTextsChanged();
    }

    partial void OnSearchQueryChanged(string value)
    {
        IsSearching = !string.IsNullOrWhiteSpace(value);
        _ = RebuildSearchResultsAsync(CancellationToken.None);
    }

    [RelayCommand]
    public Task OpenCategoryAsync(string categoryId)
        => _navigation.NavigateToCategoryAsync(categoryId);

    [RelayCommand]
    public Task OpenPatternAsync(string patternId)
        => _navigation.NavigateToPatternAsync(patternId);

    [RelayCommand]
    public Task OpenFavoritesAsync()
        => _navigation.NavigateToFavoritesAsync();

    [RelayCommand]
    public void ToggleLanguage()
    {
        var next = _localization.CurrentLanguage == AppLanguage.Russian
            ? AppLanguage.English
            : AppLanguage.Russian;
        _localization.SetLanguage(next);
    }

    private void OnLanguageChanged()
    {
        _ = RebuildCategoriesAsync(CancellationToken.None);
        _ = RebuildSearchResultsAsync(CancellationToken.None);
        NotifyTextsChanged();
    }

    private void NotifyTextsChanged()
    {
        OnPropertyChanged(nameof(Title));
        OnPropertyChanged(nameof(SearchPlaceholder));
        OnPropertyChanged(nameof(LanguageButtonText));
        OnPropertyChanged(nameof(FavoritesLabel));
    }

    private async Task RebuildCategoriesAsync(CancellationToken cancellationToken)
    {
        var categories = await _repository.GetCategoriesAsync(cancellationToken);
        var lang = _localization.CurrentLanguage;
        Categories = categories
            .Select(c => new CategoryCardItem(c.Id, c.Name.Get(lang), c.Description.Get(lang)))
            .ToList();
        OnPropertyChanged(nameof(Categories));
    }

    private async Task RebuildSearchResultsAsync(CancellationToken cancellationToken)
    {
        var results = string.IsNullOrWhiteSpace(SearchQuery)
            ? []
            : await _repository.SearchAsync(SearchQuery, cancellationToken);

        var lang = _localization.CurrentLanguage;
        SearchResults = results
            .Select(p => new PatternCardItem(p.Id, p.Name.Get(lang), p.Intent.Get(lang)))
            .ToList();
        OnPropertyChanged(nameof(SearchResults));
    }
}

/// <summary>Карточка категории для CollectionView (уже локализована).</summary>
public sealed record CategoryCardItem(string Id, string Name, string Description);

/// <summary>Карточка паттерна в поиске (уже локализована).</summary>
public sealed record PatternCardItem(string Id, string Name, string Intent);
