using DesignPatterns.Core.ViewModels;
using Microsoft.Maui.Controls;

namespace DesignPatterns.App;

/// <summary>Список паттернов категории; id категории приходит через Shell-параметр.</summary>
[QueryProperty(nameof(Id), "id")]
public partial class CategoryPage : ContentPage
{
    private readonly CategoryViewModel _viewModel;
    private bool _loaded;

    public CategoryPage(CategoryViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    /// <summary>Shell передаёт id категории через query-параметр «id».</summary>
    public string Id
    {
        get => _categoryId;
        set
        {
            if (string.Equals(_categoryId, value, StringComparison.Ordinal) && _loaded)
                return;

            _categoryId = value;
            _loaded = false;
        }
    }

    private string _categoryId = string.Empty;

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (!_loaded)
        {
            _loaded = true;
            await _viewModel.LoadAsync(_categoryId);
        }
    }
}
