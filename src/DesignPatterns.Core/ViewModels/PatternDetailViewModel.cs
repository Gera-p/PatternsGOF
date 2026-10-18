using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DesignPatterns.Core.Abstractions;
using DesignPatterns.Core.Models.Catalog;
using DesignPatterns.Core.Models.Diagrams;

namespace DesignPatterns.Core.ViewModels;

/// <summary>Детальная страница паттерна по ТЗ-структуре карточки.</summary>
public partial class PatternDetailViewModel : ObservableObject
{
    private readonly IPatternRepository _repository;
    private readonly IFavoritesStore _favorites;
    private readonly INavigationService _navigation;
    private readonly ILocalizationService _localization;

    public PatternDetailViewModel(
        IPatternRepository repository,
        IFavoritesStore favorites,
        INavigationService navigation,
        ILocalizationService localization)
    {
        _repository = repository;
        _favorites = favorites;
        _navigation = navigation;
        _localization = localization;
        _favorites.Changed += (_, _) =>
        {
            OnPropertyChanged(nameof(IsFavorite));
            OnPropertyChanged(nameof(FavoriteGlyph));
        };
    }

    public PatternInfo? Pattern { get; private set; }

    [ObservableProperty]
    private bool _isLoading;

    public bool IsFavorite => Pattern is not null && _favorites.IsFavorite(Pattern.Id);

    public string FavoriteGlyph => IsFavorite ? "★" : "☆";

    /// <summary>Локализует контент карточки под текущий язык UI.</summary>
    public string L(LocString s) => s.Get(_localization.CurrentLanguage);

    public UmlDiagram Diagram => Pattern?.Diagram ?? UmlDiagram.Empty;

    [RelayCommand]
    public async Task LoadAsync(string patternId, CancellationToken cancellationToken = default)
    {
        IsLoading = true;
        Pattern = await _repository.FindByIdAsync(patternId, cancellationToken);
        IsLoading = false;

        OnPropertyChanged(nameof(Pattern));
        OnPropertyChanged(nameof(IsFavorite));
        OnPropertyChanged(nameof(FavoriteGlyph));
        OnPropertyChanged(nameof(Diagram));
    }

    [RelayCommand]
    private void ToggleFavorite()
    {
        if (Pattern is not null)
            _favorites.Toggle(Pattern.Id);
    }

    [RelayCommand]
    private Task GoBackAsync()
        => _navigation.GoBackAsync();
}
