using System.Text.Json;

namespace DesignPatterns.Core.Content;

/// <summary>Ошибка валидации контента.</summary>
public sealed class ContentValidationException(IReadOnlyList<string> errors)
    : InvalidOperationException("Content validation failed:\n - " + string.Join("\n - ", errors))
{
    public IReadOnlyList<string> Errors { get; } = errors;
}

/// <summary>
/// Валидатор JSON-контента (Этап 3): схема RU==EN, уникальность id,
/// ссылочная целостность диаграмм, допустимые enum-значения.
/// </summary>
public static class PatternContentValidator
{
    private static readonly string[] NodeKinds = ["class", "abstract", "interface", "enum"];
    private static readonly string[] LinkKinds =
        ["inheritance", "realization", "association", "aggregation", "composition", "dependency"];

    public const int SupportedSchemaVersion = 1;

    public static void Validate(ContentDocument doc, string sourceName = "document")
    {
        var errors = new List<string>();

        if (doc.SchemaVersion != SupportedSchemaVersion)
            errors.Add($"[{sourceName}] Unsupported schemaVersion {doc.SchemaVersion} (expected {SupportedSchemaVersion}).");

        if (string.IsNullOrWhiteSpace(doc.Language))
            errors.Add($"[{sourceName}] language is empty.");

        if (doc.Categories is null || doc.Categories.Count == 0)
            errors.Add($"[{sourceName}] categories list is empty.");
        else
            ValidateCategories(doc.Categories, sourceName, errors);

        if (errors.Count > 0)
            throw new ContentValidationException(errors);
    }

    /// <summary>Кросс-валидация двух документов: одинаковые id и структура диаграмм/списков.</summary>
    public static void ValidatePair(ContentDocument ru, ContentDocument en)
    {
        Validate(ru, "ru");
        Validate(en, "en");

        var errors = new List<string>();

        var ruCats = ru.Categories.ToDictionary(c => c.Id, StringComparer.OrdinalIgnoreCase);
        var enCats = en.Categories.ToDictionary(c => c.Id, StringComparer.OrdinalIgnoreCase);

        foreach (var (id, ruCat) in ruCats)
        {
            if (!enCats.TryGetValue(id, out var enCat))
            {
                errors.Add($"Category '{id}' missing in EN.");
                continue;
            }

            var ruPats = ruCat.Patterns.ToDictionary(p => p.Id, StringComparer.OrdinalIgnoreCase);
            var enPats = enCat.Patterns.ToDictionary(p => p.Id, StringComparer.OrdinalIgnoreCase);

            foreach (var (pid, ruP) in ruPats)
            {
                if (!enPats.TryGetValue(pid, out var enP))
                {
                    errors.Add($"Pattern '{id}/{pid}' missing in EN.");
                    continue;
                }

                CompareShape(ruP, enP, $"{id}/{pid}", errors);
            }

            foreach (var pid in enPats.Keys.Except(ruPats.Keys, StringComparer.OrdinalIgnoreCase))
                errors.Add($"Pattern '{id}/{pid}' exists only in EN.");
        }

        foreach (var id in enCats.Keys.Except(ruCats.Keys, StringComparer.OrdinalIgnoreCase))
            errors.Add($"Category '{id}' exists only in EN.");

        if (errors.Count > 0)
            throw new ContentValidationException(errors);
    }

    private static void CompareShape(PatternDto ruP, PatternDto enP, string path, List<string> errors)
    {
        if (ruP.Applicability.Count != enP.Applicability.Count)
            errors.Add($"{path}: applicability count mismatch ({ruP.Applicability.Count} vs {enP.Applicability.Count}).");
        if (ruP.Pros.Count != enP.Pros.Count)
            errors.Add($"{path}: pros count mismatch.");
        if (ruP.Cons.Count != enP.Cons.Count)
            errors.Add($"{path}: cons count mismatch.");

        if (ruP.Diagram.Nodes.Count != enP.Diagram.Nodes.Count ||
            ruP.Diagram.Links.Count != enP.Diagram.Links.Count)
            errors.Add($"{path}: diagram shape mismatch.");
        else
        {
            var ruNodes = ruP.Diagram.Nodes.Select(n => (n.Id, n.Kind, n.Col, n.Row)).ToList();
            var enNodes = enP.Diagram.Nodes.Select(n => (n.Id, n.Kind, n.Col, n.Row)).ToList();
            if (!ruNodes.SequenceEqual(enNodes))
                errors.Add($"{path}: diagram nodes differ between RU/EN.");
        }
    }

    private static void ValidateCategories(IReadOnlyList<CategoryDto> categories, string source, List<string> errors)
    {
        var catIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var patternIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var cat in categories)
        {
            if (!catIds.Add(cat.Id))
                errors.Add($"[{source}] Duplicate category id '{cat.Id}'.");
            if (string.IsNullOrWhiteSpace(cat.Name))
                errors.Add($"[{source}] Category '{cat.Id}': empty name.");
            if (string.IsNullOrWhiteSpace(cat.Description))
                errors.Add($"[{source}] Category '{cat.Id}': empty description.");

            foreach (var p in cat.Patterns)
            {
                if (!patternIds.Add(p.Id))
                    errors.Add($"[{source}] Duplicate pattern id '{p.Id}'.");

                ValidatePattern(cat.Id, p, source, errors);
            }
        }
    }

    private static void ValidatePattern(string catId, PatternDto p, string source, List<string> errors)
    {
        string Prefix() => $"[{source}] {catId}/{p.Id}";

        RequireText(p.Name, "name", Prefix(), errors);
        RequireText(p.Intent, "intent", Prefix(), errors);
        RequireText(p.Problem, "problem", Prefix(), errors);
        RequireText(p.Solution, "solution", Prefix(), errors);

        if (p.Applicability.Count == 0) errors.Add($"{Prefix()}: applicability is empty.");
        if (p.Pros.Count == 0) errors.Add($"{Prefix()}: pros is empty.");
        if (p.Cons.Count == 0) errors.Add($"{Prefix()}: cons is empty.");

        ValidateDiagram(p, Prefix(), errors);

        if (string.IsNullOrWhiteSpace(p.Code?.Source))
            errors.Add($"{Prefix()}: code.source is empty.");
        if (!string.Equals(p.Code?.Language, "csharp", StringComparison.OrdinalIgnoreCase))
            errors.Add($"{Prefix()}: code.language must be 'csharp'.");
    }

    private static void ValidateDiagram(PatternDto p, string prefix, List<string> errors)
    {
        var d = p.Diagram;
        if (d is null)
        {
            errors.Add($"{prefix}: diagram is missing.");
            return;
        }

        var nodeIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var n in d.Nodes)
        {
            if (!nodeIds.Add(n.Id))
                errors.Add($"{prefix}: duplicate node id '{n.Id}'.");
            if (!NodeKinds.Contains(n.Kind.ToLowerInvariant()))
                errors.Add($"{prefix}: node '{n.Id}' has unknown kind '{n.Kind}'.");
            if (n.Col < 0 || n.Row < 0)
                errors.Add($"{prefix}: node '{n.Id}' has negative grid position.");
        }

        if (d.Nodes.Count == 0)
            errors.Add($"{prefix}: diagram has no nodes.");

        foreach (var l in d.Links)
        {
            if (!nodeIds.Contains(l.From))
                errors.Add($"{prefix}: link from unknown node '{l.From}'.");
            if (!nodeIds.Contains(l.To))
                errors.Add($"{prefix}: link to unknown node '{l.To}'.");
            if (!LinkKinds.Contains(l.Kind.ToLowerInvariant()))
                errors.Add($"{prefix}: link '{l.From}'→'{l.To}' has unknown kind '{l.Kind}'.");
        }
    }

    private static void RequireText(string? value, string field, string prefix, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
            errors.Add($"{prefix}: '{field}' is empty.");
    }

    /// <summary>Десериализация с строгими опциями (camelCase не угадываем — JsonPropertyName).</summary>
    public static ContentDocument Deserialize(string json, string sourceName = "document")
    {
        try
        {
            var doc = JsonSerializer.Deserialize<ContentDocument>(json,
                new JsonSerializerOptions { ReadCommentHandling = JsonCommentHandling.Skip })
                ?? throw new JsonException("Null document.");
            return doc;
        }
        catch (JsonException ex)
        {
            throw new ContentValidationException([$"[{sourceName}] Invalid JSON: {ex.Message}"]);
        }
    }
}
