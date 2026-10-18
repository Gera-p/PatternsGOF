using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DesignPatterns.Core.Abstractions;
using DesignPatterns.Core.Models.Catalog;

namespace DesignPatterns.Core.ViewModels;

/// <summary>Главная страница: категории + поиск + избранный.</summary>
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
        _localization.LanguageChanged += (_, _) => OnPropertyChanged(nameof(Categories));
    }

    public ObservableCollection<PatternCategory> Categories { get; } = [];

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    [ObservableProperty]
    private bool _isSearching;

    public ObservableCollection<PatternInfo> SearchResults { get; } = [];

    [RelayCommand]
    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        var categories = await _repository.GetCategoriesAsync(cancellationToken);
        Categories.Clear();
        foreach (var c in categories)
            Categories.Add(c);
    }

    partial void OnSearchQueryChanged(string value)
    {
        IsSearching = !string.IsNullOrWhiteSpace(value);
        _ = SearchAsync(value, CancellationToken.None);
    }

    private async Task SearchAsync(string query, CancellationToken cancellationToken)
    {
        IReadOnlyList<PatternInfo> results = string.IsNullOrWhiteSpace(query)
            ? []
            : await _repository.SearchAsync(query, cancellationToken);

        SearchResults.Clear();
        foreach (var r in results)
            SearchResults.Add(r);
    }

    [RelayCommand]
    private Task OpenCategoryAsync(string categoryId)
        => _navigation.NavigateToCategoryAsync(categoryId);

    [RelayCommand]
    private Task OpenPatternAsync(string patternId)
        => _navigation.NavigateToPatternAsync(patternId);

    [RelayCommand]
    private Task OpenFavoritesAsync()
        => _navigation.NavigateToFavoritesAsync();

    [RelayCommand]
    private void ToggleLanguage()
    {
        var next = _localization.CurrentLanguage == AppLanguage.Russian
            ? AppLanguage.English
            : AppLanguage.Russian;
        _localization.SetLanguage(next);
    }
}
