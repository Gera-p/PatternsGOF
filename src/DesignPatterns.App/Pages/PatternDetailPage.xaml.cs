using DesignPatterns.Core.Rendering;
using DesignPatterns.Core.ViewModels;
using Microsoft.Maui.Controls;

namespace DesignPatterns.App;

/// <summary>Детальная карточка паттерна: текст + векторная UML-диаграмма + код.</summary>
[QueryProperty(nameof(Id), "id")]
public partial class PatternDetailPage : ContentPage
{
    private readonly PatternDetailViewModel _viewModel;
    private bool _loaded;
    private string _patternId = string.Empty;

    public PatternDetailPage(PatternDetailViewModel viewModel, IDiagramRenderer renderer)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;

        DiagramCanvas.Drawable = new Controls.DiagramDrawable(renderer, () => _viewModel.Diagram);
        _viewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(_viewModel.Diagram))
                DiagramCanvas.Invalidate();
        };
    }

    /// <summary>Shell передаёт id паттерна через query-параметр «id».</summary>
    public string Id
    {
        get => _patternId;
        set
        {
            if (string.Equals(_patternId, value, StringComparison.Ordinal) && _loaded)
                return;

            _patternId = value;
            _loaded = false;
        }
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
}
