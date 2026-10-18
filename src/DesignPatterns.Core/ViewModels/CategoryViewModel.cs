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

    public CategoryViewModel(IPatternRepository repository, INavigationService navigation)
    {
        _repository = repository;
        _navigation = navigation;
    }

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

    [RelayCommand]
    private Task OpenPatternAsync(string patternId)
        => _navigation.NavigateToPatternAsync(patternId);

    [RelayCommand]
    private Task GoBackAsync()
        => _navigation.GoBackAsync();
}
