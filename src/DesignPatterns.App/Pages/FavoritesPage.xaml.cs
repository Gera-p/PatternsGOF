using DesignPatterns.Core.ViewModels;

namespace DesignPatterns.App.Pages;

/// <summary>Страница избранного: список паттернов из IFavoritesStore.</summary>
public partial class FavoritesPage : ContentPage
{
    private readonly FavoritesViewModel _viewModel;

    public FavoritesPage(FavoritesViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadAsync();
    }
}
