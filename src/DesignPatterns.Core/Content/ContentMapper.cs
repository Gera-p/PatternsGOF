using DesignPatterns.Core.Abstractions;
using DesignPatterns.Core.Models.Catalog;
using DesignPatterns.Core.Models.Diagrams;

namespace DesignPatterns.Core.Content;

/// <summary>
/// Конвертер DTO → доменные модели Core (LocString/UmlDiagram/PatternInfo).
/// Жёстко маппит строковые enum-значения; неизвестные значения — ошибка контента,
/// а не тихий fallback (контент должен быть валидным).
/// </summary>
public static class ContentMapper
{
    public static IReadOnlyList<PatternCategory> Map(
        ContentDocument ru,
        ContentDocument en,
        AppLanguage primary = AppLanguage.Russian)
    {
        if (ru.Categories.Count != en.Categories.Count)
            throw new InvalidOperationException("RU/EN documents have different category counts.");

        var enById = en.Categories.ToDictionary(c => c.Id, StringComparer.OrdinalIgnoreCase);
        var result = new List<PatternCategory>(ru.Categories.Count);

        foreach (var ruCat in ru.Categories)
        {
            if (!enById.TryGetValue(ruCat.Id, out var enCat))
                throw new InvalidOperationException($"Category '{ruCat.Id}' missing in EN document.");

            var patterns = MapPatterns(ruCat, enCat);
            result.Add(new PatternCategory(
                ruCat.Id,
                new LocString(ruCat.Name, enCat.Name),
                new LocString(ruCat.Description, enCat.Description),
                patterns));
        }

        return result;
    }

    private static IReadOnlyList<PatternInfo> MapPatterns(CategoryDto ruCat, CategoryDto enCat)
    {
        var enById = enCat.Patterns.ToDictionary(p => p.Id, StringComparer.OrdinalIgnoreCase);
        var list = new List<PatternInfo>(ruCat.Patterns.Count);

        foreach (var ruP in ruCat.Patterns)
        {
            if (!enById.TryGetValue(ruP.Id, out var enP))
                throw new InvalidOperationException(
                    $"Pattern '{ruP.Id}' (category '{ruCat.Id}') missing in EN document.");

            // Диаграмма языконезависима по структуре; берём RU как источник истины,
            // но убеждаемся, что структура совпадает (валидатор это проверил).
            var diagram = MapDiagram(ruP.Diagram);

            list.Add(new PatternInfo(
                ruP.Id,
                ruCat.Id,
                new LocString(ruP.Name, enP.Name),
                new LocString(ruP.Intent, enP.Intent),
                new LocString(ruP.Problem, enP.Problem),
                new LocString(ruP.Solution, enP.Solution),
                new LocString(JoinBullets(ruP.Applicability), JoinBullets(enP.Applicability)),
                ZipLists(ruP.Pros, enP.Pros),
                ZipLists(ruP.Cons, enP.Cons),
                diagram,
                new CodeExample(ruP.Code.Title, ruP.Code.Language, ruP.Code.Source)));
        }

        return list;
    }

    private static UmlDiagram MapDiagram(DiagramDto dto) =>
        new(
            dto.Title,
            dto.Nodes.Select(MapNode).ToList(),
            dto.Links.Select(MapLink).ToList());

    private static UmlClassNode MapNode(NodeDto n) => new(
        n.Id,
        n.Name,
        ParseKind(n.Kind),
        n.Members ?? [],
        n.Col,
        n.Row);

    private static UmlLink MapLink(LinkDto l) => new(l.From, l.To, ParseLinkKind(l.Kind), l.Label);

    private static UmlClassKind ParseKind(string kind) => kind.ToLowerInvariant() switch
    {
        "class" => UmlClassKind.Class,
        "abstract" => UmlClassKind.AbstractClass,
        "interface" => UmlClassKind.Interface,
        "enum" => UmlClassKind.Enum,
        _ => throw new InvalidOperationException($"Unknown node kind '{kind}'."),
    };

    private static UmlLinkKind ParseLinkKind(string kind) => kind.ToLowerInvariant() switch
    {
        "inheritance" => UmlLinkKind.Inheritance,
        "realization" => UmlLinkKind.Realization,
        "association" => UmlLinkKind.Association,
        "aggregation" => UmlLinkKind.Aggregation,
        "composition" => UmlLinkKind.Composition,
        "dependency" => UmlLinkKind.Dependency,
        _ => throw new InvalidOperationException($"Unknown link kind '{kind}'."),
    };

    private static string JoinBullets(IReadOnlyList<string> items) =>
        string.Join("\n", items.Select(i => "• " + i));

    private static IReadOnlyList<LocString> ZipLists(IReadOnlyList<string> ru, IReadOnlyList<string> en)
    {
        if (ru.Count != en.Count)
            throw new InvalidOperationException(
                $"RU list length {ru.Count} != EN list length {en.Count}.");

        return ru.Select((s, i) => new LocString(s, en[i])).ToList();
    }
}
