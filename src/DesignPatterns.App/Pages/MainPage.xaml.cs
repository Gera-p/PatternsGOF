using DesignPatterns.Core.ViewModels;

namespace DesignPatterns.App;

/// <summary>Главная страница: категории, поиск, переключение языка, избранное.</summary>
public partial class MainPage : ContentPage
{
    private readonly MainPageViewModel _viewModel;

    public MainPage(MainPageViewModel viewModel)
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
