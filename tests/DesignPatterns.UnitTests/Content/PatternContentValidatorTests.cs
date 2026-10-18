using DesignPatterns.Core.Content;
using FluentAssertions;

namespace DesignPatterns.UnitTests.Content;

/// <summary>
/// Тесты валидатора JSON-контента (Этап 3): схема, уникальность id,
/// целостность диаграмм, кросс-валидация RU/EN.
/// </summary>
public class PatternContentValidatorTests
{
    private static ContentDocument MakeDoc(
        string language = "ru",
        List<CategoryDto>? categories = null) =>
        new(1, language, categories ?? [MakeCategory()]);

    private static CategoryDto MakeCategory(string id = "creational", params PatternDto[] patterns) =>
        new(id, "Name", "Description", patterns.Length > 0 ? patterns.ToList() : [MakePattern()]);

    private static PatternDto MakePattern(string id = "abstract-factory") =>
        new(
            id,
            "Name",
            "Intent",
            "Problem",
            "Solution",
            ["applicable when"],
            ["pro"],
            ["con"],
            new DiagramDto(
                "T",
                [
                    new NodeDto("a", "A", "interface", [], 0, 0),
                    new NodeDto("b", "B", "class", ["M()"], 0, 1),
                ],
                [new LinkDto("b", "a", "realization", null)]),
            new CodeDto("t", "csharp", "class Demo { }"));

    [Fact]
    public void Validate_CorrectDocument_DoesNotThrow()
    {
        var act = () => PatternContentValidator.Validate(MakeDoc());
        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_WrongSchemaVersion_Throws()
    {
        var doc = new ContentDocument(99, "ru", [MakeCategory()]);
        var act = () => PatternContentValidator.Validate(doc);
        act.Should().Throw<ContentValidationException>()
            .WithMessage("*schemaVersion*");
    }

    [Fact]
    public void Validate_EmptyCategories_Throws()
    {
        var doc = new ContentDocument(1, "ru", []);
        var act = () => PatternContentValidator.Validate(doc);
        act.Should().Throw<ContentValidationException>().WithMessage("*categories*");
    }

    [Fact]
    public void Validate_DuplicatePatternIds_Throws()
    {
        var cat = new CategoryDto("c", "N", "D", [MakePattern("p"), MakePattern("p")]);
        var act = () => PatternContentValidator.Validate(MakeDoc(categories: [cat]));
        act.Should().Throw<ContentValidationException>().WithMessage("*Duplicate pattern id*");
    }

    [Fact]
    public void Validate_LinkToUnknownNode_Throws()
    {
        var pattern = MakePattern() with
        {
            Diagram = new DiagramDto(
                "T",
                [new NodeDto("a", "A", "class", [], 0, 0)],
                [new LinkDto("a", "ghost", "association", null)]),
        };
        var cat = new CategoryDto("c", "N", "D", [pattern]);

        var act = () => PatternContentValidator.Validate(MakeDoc(categories: [cat]));
        act.Should().Throw<ContentValidationException>().WithMessage("*unknown node 'ghost'*");
    }

    [Fact]
    public void Validate_UnknownLinkKind_Throws()
    {
        var pattern = MakePattern() with
        {
            Diagram = new DiagramDto(
                "T",
                [new NodeDto("a", "A", "class", [], 0, 0), new NodeDto("b", "B", "class", [], 0, 1)],
                [new LinkDto("a", "b", "telepathy", null)]),
        };
        var cat = new CategoryDto("c", "N", "D", [pattern]);

        var act = () => PatternContentValidator.Validate(MakeDoc(categories: [cat]));
        act.Should().Throw<ContentValidationException>().WithMessage("*unknown kind 'telepathy'*");
    }

    [Fact]
    public void Validate_NonCsharpCode_Throws()
    {
        var pattern = MakePattern() with { Code = new CodeDto("t", "python", "print(1)") };
        var cat = new CategoryDto("c", "N", "D", [pattern]);

        var act = () => PatternContentValidator.Validate(MakeDoc(categories: [cat]));
        act.Should().Throw<ContentValidationException>().WithMessage("*csharp*");
    }

    [Fact]
    public void Deserialize_InvalidJson_ThrowsValidationException()
    {
        var act = () => PatternContentValidator.Deserialize("{ not json ]", "bad.json");
        act.Should().Throw<ContentValidationException>().WithMessage("*Invalid JSON*");
    }

    [Fact]
    public void ValidatePair_MissingEnPattern_Throws()
    {
        var ru = MakeDoc("ru");
        var enCat = new CategoryDto("creational", "EN", "EN desc", []); // без паттерна abstract-factory
        var en = MakeDoc("en", [enCat]);

        var act = () => PatternContentValidator.ValidatePair(ru, en);
        act.Should().Throw<ContentValidationException>().WithMessage("*missing in EN*");
    }

    [Fact]
    public void ValidatePair_ListLengthMismatch_Throws()
    {
        var ru = MakeDoc("ru");
        var enPattern = MakePattern() with { Pros = ["one", "two"] }; // у RU один pros
        var en = MakeDoc("en", [new CategoryDto("creational", "EN", "D", [enPattern])]);

        var act = () => PatternContentValidator.ValidatePair(ru, en);
        act.Should().Throw<ContentValidationException>().WithMessage("*pros count mismatch*");
    }

    [Fact]
    public void ValidatePair_IdenticalShapes_DoesNotThrow()
    {
        var ru = MakeDoc("ru");
        var en = MakeDoc("en");

        var act = () => PatternContentValidator.ValidatePair(ru, en);
        act.Should().NotThrow();
    }
}
