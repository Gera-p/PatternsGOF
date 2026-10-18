using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DesignPatterns.Core.Abstractions;

namespace DesignPatterns.Core.ViewModels;

/// <summary>Список паттернов выбранной категории.</summary>
public partial class CategoryViewModel : ObservableObject
{
    private readonly IPatternRepository _repository;
    private readonly INavigationService _navigation;
    private readonly ILocalizationService _localization;

    public CategoryViewModel(
        IPatternRepository repository,
        INavigationService navigation,
        ILocalizationService localization)
    {
        _repository = repository;
        _navigation = navigation;
        _localization = localization;
        _localization.LanguageChanged += (_, _) => OnLanguageChanged();
    }

    /// <summary>Карточки паттернов категории (локализованы).</summary>
    public IReadOnlyList<PatternCardItem> Patterns { get; private set; } = [];

    public string Title { get; private set; } = string.Empty;

    private string _categoryId = string.Empty;

    [RelayCommand]
    public async Task LoadAsync(string categoryId, CancellationToken cancellationToken = default)
    {
        _categoryId = categoryId;
        await RebuildAsync(cancellationToken);
    }

    private void OnLanguageChanged() => _ = RebuildAsync(CancellationToken.None);

    private async Task RebuildAsync(CancellationToken cancellationToken)
    {
        var categories = await _repository.GetCategoriesAsync(cancellationToken);
        var category = categories.FirstOrDefault(c =>
            string.Equals(c.Id, _categoryId, StringComparison.OrdinalIgnoreCase));

        var lang = _localization.CurrentLanguage;
        Title = category?.Name.Get(lang) ?? string.Empty;
        Patterns = (category?.Patterns ?? [])
            .Select(p => new PatternCardItem(p.Id, p.Name.Get(lang), p.Intent.Get(lang)))
            .ToList();

        OnPropertyChanged(nameof(Patterns));
        OnPropertyChanged(nameof(Title));
    }

    [RelayCommand]
    public Task OpenPatternAsync(string patternId)
        => _navigation.NavigateToPatternAsync(patternId);

    [RelayCommand]
    public Task GoBackAsync()
        => _navigation.GoBackAsync();
}
