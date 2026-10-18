using DesignPatterns.Core.Abstractions;

namespace DesignPatterns.Core.Services;

/// <summary>
/// Базовая реализация <see cref="ILocalizationService"/> на словаре строк.
/// В MAUI-слое наполняется из .resx (AppResources); здесь — чистый .NET, тестируется без UI.
/// </summary>
public class DictionaryLocalizationService : ILocalizationService
{
    private readonly IReadOnlyDictionary<AppLanguage, IReadOnlyDictionary<string, string>> _strings;

    public DictionaryLocalizationService(
        IReadOnlyDictionary<AppLanguage, IReadOnlyDictionary<string, string>> strings,
        AppLanguage initial = AppLanguage.Russian)
    {
        if (!strings.ContainsKey(initial))
            throw new ArgumentException($"No strings provided for language '{initial}'.", nameof(initial));

        _strings = strings;
        CurrentLanguage = initial;
    }

    public AppLanguage CurrentLanguage { get; private set; }

    public event EventHandler<AppLanguage>? LanguageChanged;

    public string this[string key] => GetString(key);

    public string GetString(string key, params object[] args)
    {
        // Fallback: текущая локаль → RU (дефолт) → сам ключ.
        // Языковые словари могут отсутствовать в _strings (например, EN не загружен) — тогда тоже fallback.
        if ((_strings.TryGetValue(CurrentLanguage, out var current) && current.TryGetValue(key, out var value)) ||
            (_strings.TryGetValue(AppLanguage.Russian, out var ru) && ru.TryGetValue(key, out value)))
        {
            return args.Length > 0 ? string.Format(value, args) : value;
        }

        return key;
    }

    public void SetLanguage(AppLanguage language)
    {
        if (CurrentLanguage == language)
            return;

        if (!_strings.ContainsKey(language))
            throw new NotSupportedException($"Language '{language}' is not registered.");

        CurrentLanguage = language;
        LanguageChanged?.Invoke(this, language);
    }
}
