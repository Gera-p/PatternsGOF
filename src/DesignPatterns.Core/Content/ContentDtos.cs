using System.Text.Json.Serialization;

namespace DesignPatterns.Core.Content;

/// <summary>
/// DTO для JSON-контента (Этап 3). Формат хранения: один файл на язык —
/// Content/data/patterns.ru.json / Content/data/patterns.en.json.
/// Схемы RU и EN обязаны быть идентичны (одинаковые id/структура) —
/// это проверяет <see cref="PatternContentValidator"/>.
/// </summary>
public sealed record ContentDocument(
    [property: JsonPropertyName("schemaVersion")] int SchemaVersion,
    [property: JsonPropertyName("language")] string Language,
    [property: JsonPropertyName("categories")] IReadOnlyList<CategoryDto> Categories);

public sealed record CategoryDto(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("description")] string Description,
    [property: JsonPropertyName("patterns")] IReadOnlyList<PatternDto> Patterns);

public sealed record PatternDto(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("intent")] string Intent,
    [property: JsonPropertyName("problem")] string Problem,
    [property: JsonPropertyName("solution")] string Solution,
    [property: JsonPropertyName("applicability")] IReadOnlyList<string> Applicability,
    [property: JsonPropertyName("pros")] IReadOnlyList<string> Pros,
    [property: JsonPropertyName("cons")] IReadOnlyList<string> Cons,
    [property: JsonPropertyName("diagram")] DiagramDto Diagram,
    [property: JsonPropertyName("code")] CodeDto Code);

public sealed record DiagramDto(
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("nodes")] IReadOnlyList<NodeDto> Nodes,
    [property: JsonPropertyName("links")] IReadOnlyList<LinkDto> Links);

public sealed record NodeDto(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("name")] string Name,
    /// <summary>class | abstract | interface | enum</summary>
    [property: JsonPropertyName("kind")] string Kind,
    [property: JsonPropertyName("members")] IReadOnlyList<string>? Members,
    [property: JsonPropertyName("col")] int Col,
    [property: JsonPropertyName("row")] int Row);

public sealed record LinkDto(
    [property: JsonPropertyName("from")] string From,
    [property: JsonPropertyName("to")] string To,
    /// <summary>inheritance | realization | association | aggregation | composition | dependency</summary>
    [property: JsonPropertyName("kind")] string Kind,
    [property: JsonPropertyName("label")] string? Label);

public sealed record CodeDto(
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("language")] string Language,
    [property: JsonPropertyName("source")] string Source);
