using DesignPatterns.Core.Abstractions;
using DesignPatterns.Core.Services;
using DesignPatterns.Core.ViewModels;
using FluentAssertions;
using Moq;

namespace DesignPatterns.UnitTests.ViewModels;

public class PatternDetailViewModelTests
{
    private static (PatternDetailViewModel Vm, Mock<INavigationService> Nav) Create()
    {
        var repo = new InMemoryPatternRepository(DemoCatalog.Build());
        var favorites = new InMemoryFavoritesStore();
        var nav = new Mock<INavigationService>();
        var loc = new DictionaryLocalizationService(new Dictionary<AppLanguage, IReadOnlyDictionary<string, string>>
        {
            [AppLanguage.Russian] = new Dictionary<string, string>(),
            [AppLanguage.English] = new Dictionary<string, string>(),
        });
        return (new PatternDetailViewModel(repo, favorites, nav.Object, loc), nav);
    }

    [Fact]
    public async Task Load_resolves_pattern_and_diagram()
    {
        var (vm, _) = Create();

        await vm.LoadAsync("strategy");

        vm.Pattern!.Id.Should().Be("strategy");
        vm.Diagram.Nodes.Should().NotBeEmpty();
        vm.IsLoading.Should().BeFalse();
    }

    [Fact]
    public async Task ToggleFavorite_updates_state_and_glyph()
    {
        var (vm, _) = Create();
        await vm.LoadAsync("adapter");

        vm.FavoriteGlyph.Should().Be("☆");
        vm.ToggleFavoriteCommand.Execute(null);

        vm.IsFavorite.Should().BeTrue();
        vm.FavoriteGlyph.Should().Be("★");
    }

    [Fact]
    public async Task Localizes_content_by_current_language()
    {
        var (vm, _) = Create();
        await vm.LoadAsync("strategy");

        vm.Title.Should().Be("Стратегия");
    }

    [Fact]
    public async Task GoBack_delegates_to_navigation_service()
    {
        var (vm, nav) = Create();
        await vm.LoadAsync("strategy");

        await vm.GoBackCommand.ExecuteAsync(null);

        nav.Verify(n => n.GoBackAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
