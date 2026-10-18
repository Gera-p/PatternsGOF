using System.Reflection;

namespace DesignPatterns.Core.Content;

/// <summary>
/// Загрузчик контент-документов из embedded-ресурсов Core-assembly
/// (Content/data/patterns-ru.json, patterns-en.json).
/// Имена culture-neutral (дефис вместо точки) — см. комментарий в csproj:
/// иначе MSBuild относит *.ru.*/*.en.* к satellite-сборкам.
/// Для MAUI Asset-потоков используйте перегрузку со Stream.
/// </summary>
public static class ContentLoader
{
    public const string RuResourceName = "patterns-ru.json";
    public const string EnResourceName = "patterns-en.json";

    public static ContentDocument LoadEmbeddedRu(Assembly? assembly = null)
        => LoadEmbedded(RuResourceName, assembly ?? typeof(ContentLoader).Assembly);

    public static ContentDocument LoadEmbeddedEn(Assembly? assembly = null)
        => LoadEmbedded(EnResourceName, assembly ?? typeof(ContentLoader).Assembly);

    public static ContentDocument LoadEmbedded(string fileName, Assembly assembly)
    {
        // Точное совпадение по хвосту имени (с точкой перед именем файла),
        // чтобы "patterns-ru.json" не матчился на "...data.patterns.json" из satellite-сборки.
        var resourceName = assembly.GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith("." + fileName, StringComparison.OrdinalIgnoreCase)
                              || string.Equals(n, fileName, StringComparison.OrdinalIgnoreCase));
        if (resourceName is null)
        {
            var available = string.Join(", ", assembly.GetManifestResourceNames());
            throw new InvalidOperationException(
                $"Embedded resource '{fileName}' not found in {assembly.GetName().Name}. Available: [{available}].");
        }

        using var stream = assembly.GetManifestResourceStream(resourceName)!;
        return LoadFromStream(stream, fileName);
    }

    public static ContentDocument LoadFromStream(Stream stream, string sourceName = "stream")
    {
        using var reader = new StreamReader(stream);
        var json = reader.ReadToEnd();
        var doc = PatternContentValidator.Deserialize(json, sourceName);
        PatternContentValidator.Validate(doc, sourceName);
        return doc;
    }
}
