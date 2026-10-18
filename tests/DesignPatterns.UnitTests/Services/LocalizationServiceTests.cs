using DesignPatterns.Core.Abstractions;
using DesignPatterns.Core.Services;
using FluentAssertions;

namespace DesignPatterns.UnitTests.Services;

public class LocalizationServiceTests
{
    private static DictionaryLocalizationService Create(
        AppLanguage initial = AppLanguage.Russian) => new(new Dictionary<AppLanguage, IReadOnlyDictionary<string, string>>
        {
            [AppLanguage.Russian] = new Dictionary<string, string>
            {
                ["AppTitle"] = "Паттерны проектирования",
                ["PatternCount"] = "{0} паттернов",
            },
            [AppLanguage.English] = new Dictionary<string, string>
            {
                ["AppTitle"] = "Design Patterns",
            },
        }, initial);

    [Fact]
    public void Default_language_is_russian()
    {
        var service = Create();

        service.CurrentLanguage.Should().Be(AppLanguage.Russian);
        service["AppTitle"].Should().Be("Паттерны проектирования");
    }

    [Fact]
    public void SetLanguage_switches_strings_without_restart()
    {
        var service = Create();

        service.SetLanguage(AppLanguage.English);

        service.CurrentLanguage.Should().Be(AppLanguage.English);
        service["AppTitle"].Should().Be("Design Patterns");
    }

    [Fact]
    public void SetLanguage_raises_languageChanged_event()
    {
        var service = Create();
        AppLanguage? received = null;
        service.LanguageChanged += (_, lang) => received = lang;

        service.SetLanguage(AppLanguage.English);

        received.Should().Be(AppLanguage.English);
    }

    [Fact]
    public void SetLanguage_to_same_language_does_not_raise_event()
    {
        var service = Create();
        var raised = false;
        service.LanguageChanged += (_, _) => raised = true;

        service.SetLanguage(AppLanguage.Russian);

        raised.Should().BeFalse();
    }

    [Fact]
    public void Missing_key_in_current_language_falls_back_to_russian_default()
    {
        var service = Create(AppLanguage.English);

        // PatternCount отсутствует в EN — fallback на RU (дефолтная культура).
        service.GetString("PatternCount", 5).Should().Be("5 паттернов");
    }

    [Fact]
    public void Completely_unknown_key_returns_key_itself()
    {
        var service = Create();

        service["NoSuchKey"].Should().Be("NoSuchKey");
    }

    [Fact]
    public void SetLanguage_unregistered_language_throws()
    {
        var service = Create();
        var act = () => service.SetLanguage((AppLanguage)99);

        act.Should().Throw<NotSupportedException>();
    }
}
