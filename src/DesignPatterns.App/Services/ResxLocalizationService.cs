using System.Resources;
using DesignPatterns.Core.Abstractions;

namespace DesignPatterns.App.Services;

/// <summary>
/// Локализация на базе .resx (AppResources.resx — RU дефолт, AppResources.en.resx — EN сателлит).
/// Переключение языка без перезапуска: меняем CurrentUICulture и порождаем LanguageChanged;
/// страницы/ViewModel'и перерисовывают биндинги по событию.
/// </summary>
public sealed class ResxLocalizationService : ILocalizationService
{
    private static readonly ResourceManager Manager =
        new("DesignPatterns.App.Resources.L10n.AppResources", typeof(ResxLocalizationService).Assembly);

    public ResxLocalizationService()
    {
        var culture = PreferSystemLanguage();
        Apply(culture);
        CurrentLanguage = ToApp(culture);
    }

    public AppLanguage CurrentLanguage { get; private set; }

    public event EventHandler<AppLanguage>? LanguageChanged;

    public string this[string key] => GetString(key);

    public string GetString(string key, params object[] args)
    {
        var value = Manager.GetString(key) ?? key;
        return args.Length > 0 ? string.Format(value, args) : value;
    }

    public void SetLanguage(AppLanguage language)
    {
        if (CurrentLanguage == language)
            return;

        Apply(language == AppLanguage.English ? "en" : "ru");
        CurrentLanguage = language;
        LanguageChanged?.Invoke(this, language);
    }

    private static void Apply(string culture)
    {
        var ci = new System.Globalization.CultureInfo(culture);
        System.Globalization.CultureInfo.DefaultThreadCurrentCulture = ci;
        System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = ci;
    }

    private static string PreferSystemLanguage()
    {
        var two = System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        return two.Equals("en", StringComparison.OrdinalIgnoreCase) ? "en" : "ru";
    }

    private static AppLanguage ToApp(string culture)
        => culture == "en" ? AppLanguage.English : AppLanguage.Russian;
}
