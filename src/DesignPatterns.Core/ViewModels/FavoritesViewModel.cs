using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DesignPatterns.Core.Abstractions;

namespace DesignPatterns.Core.ViewModels;

/// <summary>Страница избранного: паттерны, отмеченные звёздочкой.</summary>
public partial class FavoritesViewModel : ObservableObject
{
    private readonly IPatternRepository _repository;
    private readonly IFavoritesStore _favorites;
    private readonly INavigationService _navigation;
    private readonly ILocalizationService _localization;

    public FavoritesViewModel(
        IPatternRepository repository,
        IFavoritesStore favorites,
        INavigationService navigation,
        ILocalizationService localization)
    {
        _repository = repository;
        _favorites = favorites;
        _navigation = navigation;
        _localization = localization;
        _favorites.Changed += (_, _) => _ = LoadAsync();
        _localization.LanguageChanged += (_, _) => _ = LoadAsync();
    }

    /// <summary>Избранные карточки в порядке каталога (локализованы).</summary>
    public IReadOnlyList<PatternCardItem> Favorites { get; private set; } = [];

    public string Title => _localization.GetString("FavoritesTitle");
    public string EmptyText => _localization.GetString("FavoritesEmpty");

    [RelayCommand]
    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        var all = await _repository.GetAllPatternsAsync(cancellationToken);
        var lang = _localization.CurrentLanguage;
        Favorites = all
            .Where(p => _favorites.IsFavorite(p.Id))
            .Select(p => new PatternCardItem(p.Id, p.Name.Get(lang), p.Intent.Get(lang)))
            .ToList();

        OnPropertyChanged(nameof(Favorites));
        OnPropertyChanged(nameof(Title));
        OnPropertyChanged(nameof(EmptyText));
    }

    [RelayCommand]
    public Task OpenPatternAsync(string patternId)
        => _navigation.NavigateToPatternAsync(patternId);
}
