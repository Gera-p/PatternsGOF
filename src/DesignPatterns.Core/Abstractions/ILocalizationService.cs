namespace DesignPatterns.Core.Abstractions;

/// <summary>Поддерживаемые языки приложения.</summary>
public enum AppLanguage
{
    Russian,
    English,
}

/// <summary>
/// Сервис локализации: строки UI и переключение языка «на лету»
/// без перезапуска приложения.
/// </summary>
public interface ILocalizationService
{
    /// <summary>Текущий язык.</summary>
    AppLanguage CurrentLanguage { get; }

    /// <summary>Индексатор для строк: loc["AppTitle"].</summary>
    string this[string key] { get; }

    /// <summary>Получение локализованной строки с аргументами формата.</summary>
    string GetString(string key, params object[] args);

    /// <summary>Переключение языка. Порождает <see cref="LanguageChanged"/>.</summary>
    void SetLanguage(AppLanguage language);

    /// <summary>Событие смены языка — подписываются ViewModel'и и страницы.</summary>
    event EventHandler<AppLanguage>? LanguageChanged;
}
