using DesignPatterns.App.Services;
using DesignPatterns.Core;
using DesignPatterns.Core.Abstractions;
using DesignPatterns.Core.Services;
using DesignPatterns.Core.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace DesignPatterns.App;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>();

        // ── Core-слой (общая регистрация, переиспользуется в тестах) ──────────
        builder.Services.AddDesignPatternsCore();

        // ── Контент и данные ───────────────────────────────────────────────────
        builder.Services.AddSingleton<IPatternRepository>(_ => new InMemoryPatternRepository(DemoCatalog.Build()));
        builder.Services.AddSingleton<IFavoritesStore, InMemoryFavoritesStore>();

        // ── Локализация: resx → ILocalizationService (переключение на лету) ────
        builder.Services.AddSingleton<ILocalizationService, ResxLocalizationService>();

        // ── Навигация поверх Shell ─────────────────────────────────────────────
        builder.Services.AddSingleton<INavigationService, ShellNavigationService>();

        // TODO (Этап 2): builder.Services.AddSingleton<IDiagramRenderer, UmlDiagramRenderer>();

        // ── ViewModel'ы ────────────────────────────────────────────────────────
        builder.Services.AddTransient<MainPageViewModel>();
        builder.Services.AddTransient<CategoryViewModel>();
        builder.Services.AddTransient<PatternDetailViewModel>();

        // ── Страницы ───────────────────────────────────────────────────────────
        builder.Services.AddSingleton<MainPage>();
        builder.Services.AddTransient<CategoryPage>();
        builder.Services.AddTransient<PatternDetailPage>();

        return builder.Build();
    }
}
