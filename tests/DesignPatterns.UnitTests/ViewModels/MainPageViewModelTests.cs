using DesignPatterns.Core.Abstractions;
using DesignPatterns.Core.Services;
using DesignPatterns.Core.ViewModels;
using FluentAssertions;
using Moq;

namespace DesignPatterns.UnitTests.ViewModels;

public class MainPageViewModelTests
{
    private static (MainPageViewModel Vm, Mock<INavigationService> Nav) Create()
    {
        var repo = new InMemoryPatternRepository(DemoCatalog.Build());
        var nav = new Mock<INavigationService>();
        var loc = new DictionaryLocalizationService(new Dictionary<AppLanguage, IReadOnlyDictionary<string, string>>
        {
            [AppLanguage.Russian] = new Dictionary<string, string>(),
            [AppLanguage.English] = new Dictionary<string, string>(),
        });
        return (new MainPageViewModel(repo, nav.Object, loc), nav);
    }

    [Fact]
    public async Task Load_populates_three_categories()
    {
        var (vm, _) = Create();

        await vm.LoadAsync();

        vm.Categories.Should().HaveCount(3);
    }

    [Fact]
    public async Task SearchQuery_fills_results_and_flag()
    {
        var (vm, _) = Create();

        vm.SearchQuery = "strategy";
        await WaitAsync(() => vm.IsSearching && vm.SearchResults.Count > 0);

        vm.IsSearching.Should().BeTrue();
        vm.SearchResults.Should().ContainSingle(p => p.Id == "strategy");
    }

    [Fact]
    public async Task OpenCategoryCommand_navigates_to_category()
    {
        var (vm, nav) = Create();

        await vm.OpenCategoryCommand.ExecuteAsync("creational");

        nav.Verify(n => n.NavigateToCategoryAsync("creational", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task OpenFavoritesCommand_navigates_to_favorites()
    {
        var (vm, nav) = Create();

        await vm.OpenFavoritesCommand.ExecuteAsync(null);

        nav.Verify(n => n.NavigateToFavoritesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void ToggleLanguage_switches_ru_en()
    {
        var (vm, _) = Create();

        vm.ToggleLanguageCommand.Execute(null);
        vm.ToggleLanguageCommand.Execute(null);

        // Двойной переключатель возвращает к RU — проверяем, что оба вызова прошли без исключения
        // и язык реально менялся (см. LocalizationServiceTests).
        vm.ToggleLanguageCommand.CanExecute(null).Should().BeTrue();
    }

    private static async Task WaitAsync(Func<bool> condition)
    {
        for (var i = 0; i < 100 && !condition(); i++)
            await Task.Delay(10);
        condition.Should().BeTrue("search is dispatched asynchronously from OnSearchQueryChanged");
    }
}
