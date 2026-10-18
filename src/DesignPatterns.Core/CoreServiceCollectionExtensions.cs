using Microsoft.Extensions.DependencyInjection;

namespace DesignPatterns.Core;

/// <summary>Токен-регистрация Core-сервисов в общей DI-коробке.</summary>
public interface ICoreApp;

/// <summary>
/// Централизованная регистрация сервисов Core-слоя.
/// Вызывается из MauiProgram.AddCoreServices() и переиспользуется в тестах.
/// </summary>
public static class CoreServiceCollectionExtensions
{
    public static IServiceCollection AddDesignPatternsCore(this IServiceCollection services)
    {
        services.AddSingleton<ICoreApp, DefaultCoreAppMarker>();
        return services;
    }

    private sealed class DefaultCoreAppMarker : ICoreApp;
}
