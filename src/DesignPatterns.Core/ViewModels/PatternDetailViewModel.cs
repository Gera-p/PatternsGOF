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

    private PatternInfo? _pattern;

    /// <summary>Загруженный паттерн (null до LoadAsync).</summary>
    public PatternInfo? Pattern => _pattern;

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
        _favorites.Changed += (_, _) => NotifyFavoriteChanged();
        _localization.LanguageChanged += (_, _) => NotifyContentChanged();
    }

    [ObservableProperty]
    private bool _isLoading;

    public bool IsFavorite => _pattern is not null && _favorites.IsFavorite(_pattern.Id);

    public string FavoriteGlyph => IsFavorite ? "★" : "☆";

    // ── Секции карточки (локализованы под текущий язык) ────────────────────────
    public string Title => L(_pattern?.Name);
    public string ProblemText => Section("ProblemHeader", _pattern?.Problem);
    public string SolutionText => Section("SolutionHeader", _pattern?.Solution);
    public string ApplicabilityText => Section("ApplicabilityHeader", _pattern?.Applicability);
    public string ProsText => Bullets("ProsHeader", _pattern?.Pros);
    public string ConsText => Bullets("ConsHeader", _pattern?.Cons);
    public string CodeTitle => _pattern is null
        ? string.Empty
        : $"{_localization.GetString("CodeHeader")}: {_pattern.Example.Title}";
    public string CodeText => _pattern?.Example.SourceCode ?? string.Empty;

    /// <summary>Модель диаграммы для DiagramDrawable (рендерится кодом).</summary>
    public UmlDiagram Diagram => _pattern?.Diagram ?? UmlDiagram.Empty;

    [RelayCommand]
    public async Task LoadAsync(string patternId, CancellationToken cancellationToken = default)
    {
        IsLoading = true;
        _pattern = await _repository.FindByIdAsync(patternId, cancellationToken);
        IsLoading = false;
        NotifyFavoriteChanged();
        NotifyContentChanged();
    }

    [RelayCommand]
    public void ToggleFavorite()
    {
        if (_pattern is not null)
            _favorites.Toggle(_pattern.Id);
    }

    [RelayCommand]
    public Task GoBackAsync()
        => _navigation.GoBackAsync();

    private void NotifyFavoriteChanged()
    {
        OnPropertyChanged(nameof(IsFavorite));
        OnPropertyChanged(nameof(FavoriteGlyph));
    }

    private void NotifyContentChanged()
    {
        OnPropertyChanged(nameof(Title));
        OnPropertyChanged(nameof(ProblemText));
        OnPropertyChanged(nameof(SolutionText));
        OnPropertyChanged(nameof(ApplicabilityText));
        OnPropertyChanged(nameof(ProsText));
        OnPropertyChanged(nameof(ConsText));
        OnPropertyChanged(nameof(CodeTitle));
        OnPropertyChanged(nameof(CodeText));
        OnPropertyChanged(nameof(Diagram));
    }

    private string L(LocString? s)
        => s is null ? string.Empty : s.Get(_localization.CurrentLanguage);

    private string Section(string headerKey, LocString? content)
        => content is null
            ? string.Empty
            : $"{_localization.GetString(headerKey)}\n{L(content)}";

    private string Bullets(string headerKey, IReadOnlyList<LocString>? items)
    {
        if (items is null || items.Count == 0)
            return string.Empty;

        var lines = items.Select(i => $"• {L(i)}");
        return $"{_localization.GetString(headerKey)}\n{string.Join("\n", lines)}";
    }
}
