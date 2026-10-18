using System.Text.Json;
using DesignPatterns.Core.Abstractions;
using DesignPatterns.Core.Content;
using DesignPatterns.Core.Models.Catalog;
using DesignPatterns.Core.Models.Diagrams;
using DesignPatterns.Core.Services;
using FluentAssertions;

namespace DesignPatterns.UnitTests.Content;

/// <summary>
/// Интеграционные тесты контент-пайплайна (Этап 3):
/// реальные embedded JSON (patterns-ru.json / patterns-en.json) загружаются,
/// валидируются, маппятся и дают консистентный RU/EN каталог.
/// </summary>
public class ContentPipelineTests
{
    private static IReadOnlyList<PatternCategory> LoadCatalog()
    {
        var ru = ContentLoader.LoadEmbeddedRu();
        var en = ContentLoader.LoadEmbeddedEn();
        PatternContentValidator.ValidatePair(ru, en);
        return ContentMapper.Map(ru, en);
    }

    [Fact]
    public void EmbeddedResources_LoadAndValidate()
    {
        var ru = ContentLoader.LoadEmbeddedRu();
        var en = ContentLoader.LoadEmbeddedEn();

        ru.Language.Should().Be("ru");
        en.Language.Should().Be("en");
        ru.Categories.Should().HaveCount(3);
        en.Categories.Should().HaveCount(3);
    }

    [Fact]
    public void Map_ProducesLocalizedCatalog()
    {
        var catalog = LoadCatalog();

        catalog.Select(c => c.Id).Should().Equal("creational", "structural", "behavioral");

        var factory = catalog[0].Patterns.Single();
        factory.Id.Should().Be("abstract-factory");
        factory.Name.Get(AppLanguage.Russian).Should().Be("Абстрактная фабрика");
        factory.Name.Get(AppLanguage.English).Should().Be("Abstract Factory");
        factory.Intent.En.Should().NotBeNullOrWhiteSpace();
        factory.Problem.Ru.Should().NotBeNullOrWhiteSpace();
        factory.Solution.Ru.Should().NotBeNullOrWhiteSpace();
        factory.Applicability.Ru.Should().Contain("•");
        factory.Pros.Should().OnlyContain(p => !string.IsNullOrWhiteSpace(p.Ru) && !string.IsNullOrWhiteSpace(p.En));
        factory.Cons.Should().NotBeEmpty();
    }

    [Fact]
    public void Map_DiagramsAreMappedWithCorrectKinds()
    {
        var catalog = LoadCatalog();
        var adapter = catalog.Single(c => c.Id == "structural").Patterns.Single();

        adapter.Diagram.Nodes.Should().HaveCount(3);
        adapter.Diagram.Nodes.Should().Contain(n => n.Kind == UmlClassKind.Interface);
        adapter.Diagram.Links.Should().Contain(l => l.Kind == UmlLinkKind.Composition);
        // Ссылочная целостность подтверждена валидатором при загрузке.
    }

    [Fact]
    public async Task Repository_ServesSearchInBothLanguages()
    {
        var repo = new InMemoryPatternRepository(LoadCatalog());

        var byRu = await repo.SearchAsync("Адаптер");
        byRu.Should().ContainSingle(p => p.Id == "adapter");

        var byEn = await repo.SearchAsync("Strategy");
        byEn.Should().ContainSingle(p => p.Id == "strategy");
    }

    [Fact]
    public void CodeExamples_HaveBalancedBracesAndTypeDeclarations()
    {
        // Полная компиляция примеров — на Этапе 4 (тесты примеров).
        // Здесь структурная sanity-проверка контента.
        var catalog = LoadCatalog();

        foreach (var pattern in catalog.SelectMany(c => c.Patterns))
        {
            var src = pattern.Example.SourceCode;
            src.Should().NotBeNullOrWhiteSpace($"example of {pattern.Id} must exist");
            Count(src, '{').Should().Be(Count(src, '}'), $"braces balanced in {pattern.Id}");
            Count(src, '(').Should().Be(Count(src, ')'), $"parens balanced in {pattern.Id}");
            src.Should().MatchRegex(@"(class|interface|struct|enum|record)", $"{pattern.Id} should declare types");
        }
    }

    [Fact]
    public void JsonRoundtrip_SerializationStable()
    {
        var ru = ContentLoader.LoadEmbeddedRu();
        var json = JsonSerializer.Serialize(ru);
        var reparsed = PatternContentValidator.Deserialize(json, "roundtrip");
        PatternContentValidator.Validate(reparsed, "roundtrip");
        reparsed.Categories.Count.Should().Be(ru.Categories.Count);
    }

    private static int Count(string s, char c) => s.Count(ch => ch == c);
}
