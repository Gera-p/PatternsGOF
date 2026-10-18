using DesignPatterns.Core.Services;
using FluentAssertions;

namespace DesignPatterns.UnitTests.Services;

public class PatternRepositoryTests
{
    private static InMemoryPatternRepository Create() => new(DemoCatalog.Build());

    [Fact]
    public async Task Returns_three_gof_categories()
    {
        var categories = await Create().GetCategoriesAsync();

        categories.Should().HaveCount(3);
        categories.Select(c => c.Id).Should().BeEquivalentTo(["creational", "structural", "behavioral"]);
    }

    [Fact]
    public async Task FindById_is_case_insensitive()
    {
        var pattern = await Create().FindByIdAsync("Strategy");

        pattern.Should().NotBeNull();
        pattern!.Id.Should().Be("strategy");
    }

    [Fact]
    public async Task Search_matches_russian_and_english_names()
    {
        var repo = Create();

        (await repo.SearchAsync("Стратегия")).Should().ContainSingle(p => p.Id == "strategy");
        (await repo.SearchAsync("adapter")).Should().ContainSingle(p => p.Id == "adapter");
    }

    [Fact]
    public async Task Search_empty_query_returns_nothing()
    {
        var results = await Create().SearchAsync("   ");

        results.Should().BeEmpty();
    }

    [Fact]
    public void FavoritesStore_toggle_adds_and_removes_with_event()
    {
        var store = new InMemoryFavoritesStore();
        var events = 0;
        store.Changed += (_, _) => events++;

        store.Toggle("strategy");
        store.IsFavorite("strategy").Should().BeTrue();

        store.Toggle("strategy");
        store.IsFavorite("strategy").Should().BeFalse();

        events.Should().Be(2);
    }
}
