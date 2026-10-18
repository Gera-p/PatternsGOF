using DesignPatterns.Core.ViewModels;
using Microsoft.Maui.Controls;

namespace DesignPatterns.App;

/// <summary>Список паттернов категории; id категории приходит через Shell-параметр.</summary>
public partial class CategoryPage : ContentPage
{
    private readonly CategoryViewModel _viewModel;

    public CategoryPage(CategoryViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (!_loaded)
        {
            _loaded = true;
            await _viewModel.LoadAsync(_categoryId);
        }
    }

    private bool _loaded;
    private string _categoryId = string.Empty;

    /// <summary>Заполняется ShellNavigationService перед переходом.</summary>
    public void SetCategoryId(string categoryId)
    {
        _categoryId = categoryId;
        _loaded = false;
    }
}
