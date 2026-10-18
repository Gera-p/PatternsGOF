using DesignPatterns.Core.Rendering;
using DesignPatterns.Core.ViewModels;
using Microsoft.Maui.Controls;

namespace DesignPatterns.App;

/// <summary>Детальная карточка паттерна: текст + векторная UML-диаграмма + код.</summary>
public partial class PatternDetailPage : ContentPage
{
    private readonly PatternDetailViewModel _viewModel;
    private readonly IDiagramRenderer _renderer;

    public PatternDetailPage(PatternDetailViewModel viewModel, IDiagramRenderer renderer)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _renderer = renderer;
        BindingContext = _viewModel;

        DiagramCanvas.Drawable = new Controls.DiagramDrawable(renderer, () => _viewModel.Diagram);
        _viewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(_viewModel.Diagram))
                DiagramCanvas.Invalidate();
        };
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (!_loaded)
        {
            _loaded = true;
            await _viewModel.LoadAsync(_patternId);
        }
    }

    private bool _loaded;
    private string _patternId = string.Empty;

    /// <summary>Заполняется ShellNavigationService перед переходом.</summary>
    public void SetPatternId(string patternId)
    {
        _patternId = patternId;
        _loaded = false;
    }
}
