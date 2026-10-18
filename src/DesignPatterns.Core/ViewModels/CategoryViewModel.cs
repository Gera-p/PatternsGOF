using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DesignPatterns.Core.Abstractions;
using DesignPatterns.Core.Models.Catalog;

namespace DesignPatterns.Core.ViewModels;

/// <summary>Список паттернов выбранной категории.</summary>
public partial class CategoryViewModel : ObservableObject
{
    private readonly IPatternRepository _repository;
    private readonly INavigationService _navigation;
    private readonly ILocalizationService? _localization;

    public CategoryViewModel(
        IPatternRepository repository,
        INavigationService navigation,
        ILocalizationService? localization = null)
    {
        _repository = repository;
        _navigation = navigation;
        _localization = localization;
        if (_localization is not null)
            _localization.LanguageChanged += (_, _) => RefreshLocalizedTexts();
    }

    /// <summary>Паттерны категории (модели карточек).</summary>
    public ObservableCollection<PatternInfo> Patterns { get; } = [];

    [ObservableProperty]
    private LocString? _categoryName;

    [RelayCommand]
    public async Task LoadAsync(string categoryId, CancellationToken cancellationToken = default)
    {
        var categories = await _repository.GetCategoriesAsync(cancellationToken);
        var category = categories.FirstOrDefault(c =>
            string.Equals(c.Id, categoryId, StringComparison.OrdinalIgnoreCase));

        Patterns.Clear();
        if (category is null)
            return;

        CategoryName = category.Name;
        foreach (var p in category.Patterns)
            Patterns.Add(p);
    }

    /// <summary>Локализует строку под текущий язык UI (RU по умолчанию).</summary>
    public string L(LocString s)
        => s.Get(_localization?.CurrentLanguage ?? AppLanguage.Russian);

    private void RefreshLocalizedTexts()
    {
        OnPropertyChanged(nameof(CategoryName));
        foreach (var p in Patterns.ToList())
        {
            // PatternInfo — record, биндинги на вложенные LocString не «шумят» сами:
            // эмулируем обновление через PropertyChanged коллекции.
            var index = Patterns.IndexOf(p);
            if (index >= 0)
                OnPropertyChanged($@"Patterns[{index}].Name");
        }
    }

    [RelayCommand]
    public Task OpenPatternAsync(string patternId)
        => _navigation.NavigateToPatternAsync(patternId);

    [RelayCommand]
    public Task GoBackAsync()
        => _navigation.GoBackAsync();
}
