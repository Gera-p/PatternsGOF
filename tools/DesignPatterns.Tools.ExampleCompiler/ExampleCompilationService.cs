using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace DesignPatterns.Tools.ExampleCompiler;

/// <summary>
/// Извлекает C#-примеры из embedded JSON-контента Core и компилирует их
/// "в живую" через Roslyn (reference assemblies текущего рантайма).
/// Используется тестами Этапа 4: каждый пример обязан собираться без ошибок —
/// это гарантия «валидный C#, а не псевдокод».
/// </summary>
public static class ExampleCompilationService
{
    private static readonly Lazy<IReadOnlyList<string>> ReferencePaths = new(() =>
    {
        // .csproj тестового раннера не кладёт все runtime-сборки в output, поэтому
        // берём reference assemblies из установленного SDK: PathToReferences
        // указывает на папку с ref/net9.0/*.dll (корректный источник для Roslyn).
        var trustedAssembliesString = (string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES");
        if (string.IsNullOrEmpty(trustedAssembliesString))
            throw new InvalidOperationException("TRUSTED_PLATFORM_ASSEMBLIES is not available.");

        var tpa = trustedAssembliesString
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(f => !Path.GetFileName(f).StartsWith("DesignPatterns", StringComparison.OrdinalIgnoreCase))
            .Where(f => !Path.GetFileName(f).Contains(".resources.", StringComparison.OrdinalIgnoreCase))
            .ToList();

        return tpa;
    });

    /// <summary>Собирает все примеры из RU/EN контента: patternId -> source.</summary>
    public static IReadOnlyDictionary<string, string> LoadAllExamples()
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var doc in new[] { DesignPatterns.Core.Content.ContentLoader.LoadEmbeddedRu(), DesignPatterns.Core.Content.ContentLoader.LoadEmbeddedEn() })
        {
            foreach (var category in doc.Categories)
            {
                foreach (var pattern in category.Patterns)
                {
                    if (!string.IsNullOrWhiteSpace(pattern.Code.Source))
                    {
                        // RU и EN содержат идентичный код (проверяется ValidatePair),
                        // компилируем одно вхождение на паттерн.
                        result[pattern.Id] = pattern.Code.Source;
                    }
                }
            }
        }
        return result;
    }

    /// <summary>
    /// Компиляция "как в проекте с ImplicitUsings": к исходнику примера добавляются
    /// стандартные using-директивы .NET. Сами JSON-файлы остаются чистыми (только код паттерна).
    /// </summary>
    private const string ImplicitGlobalUsings = """
        global using System;
        global using System.Collections.Generic;
        global using System.IO;
        global using System.Linq;
        global using System.Net.Http;
        global using System.Threading;
        global using System.Threading.Tasks;

        """;

    private static SyntaxTree ParseExample(string source) => CSharpSyntaxTree.ParseText(
        ImplicitGlobalUsings + source, new CSharpParseOptions(LanguageVersion.Preview));

    public static CompilationResult Compile(string patternId, string source)
    {
        var syntaxTree = ParseExample(source);

        var compilation = CSharpCompilation.Create(
            assemblyName: $"PatternExample_{patternId}",
            syntaxTrees: [syntaxTree],
            references: ReferencePaths.Value.Select(r => MetadataReference.CreateFromFile(r)),
            options: new Microsoft.CodeAnalysis.CSharp.CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        using var ms = new MemoryStream();
        var emit = compilation.Emit(ms);

        var errors = emit.Diagnostics
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .Select(d => d.ToString())
            .ToList();

        return new CompilationResult(patternId, emit.Success, errors);
    }

    /// <summary>
    /// Компилирует исходник и исполняет статический метод Demo.Run(...) внутри example-assembly.
    /// Параметры Run() заполняются так: для каждого интерфейса/абстракции примера подбирается
    /// первая конкретная реализация из этого же исходника и создаётся через Activator
    /// (рекурсивно по конструктору с наименьшим числом параметров).
    /// Возвращает результат Run() как string — для поведенческих ассертов.
    /// </summary>
    public static string ExecuteDemoRun(string patternId, string source)
    {
        var syntaxTree = ParseExample(source);

        var compilation = CSharpCompilation.Create(
            assemblyName: $"PatternDemo_{patternId}_{Guid.NewGuid():N}",
            syntaxTrees: [syntaxTree],
            references: ReferencePaths.Value.Select(r => MetadataReference.CreateFromFile(r)),
            options: new Microsoft.CodeAnalysis.CSharp.CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        using var ms = new MemoryStream();
        var emit = compilation.Emit(ms);
        if (!emit.Success)
        {
            var errors = string.Join(Environment.NewLine,
                emit.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
            throw new InvalidOperationException($"Example '{patternId}' failed to compile:{Environment.NewLine}{errors}");
        }

        ms.Seek(0, SeekOrigin.Begin);
        var assembly = Assembly.Load(ms.ToArray());
        var types = assembly.GetTypes();

        var demoType = types.FirstOrDefault(t => t.Name == "Demo")
            ?? throw new InvalidOperationException($"Example '{patternId}' has no 'Demo' type.");

        var runMethod = demoType.GetMethods(BindingFlags.Public | BindingFlags.Static)
            .FirstOrDefault(m => m.Name == "Run")
            ?? throw new InvalidOperationException($"'{patternId}'.Demo has no public static Run().");

        var args = runMethod.GetParameters()
            .Select(p => CreateInstance(p.ParameterType, types, depth: 0, patternId))
            .ToArray();

        return runMethod.Invoke(null, args)?.ToString() ?? string.Empty;
    }

    private static object? CreateInstance(Type type, Type[] allTypes, int depth, string patternId)
    {
        if (depth > 6)
            throw new InvalidOperationException($"Recursion too deep constructing '{type.Name}' in '{patternId}'.");

        if (type == typeof(string)) return "";
        if (type == typeof(int)) return 0;
        if (type == typeof(bool)) return false;
        if (type.IsArray && type.GetElementType() is { } elem)
        {
            var arr = Array.CreateInstance(elem, 0);
            return arr;
        }

        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
            return Activator.CreateInstance(type)!;

        // Интерфейс/абстрактный класс — ищем конкретную реализацию в том же примере.
        if (type.IsInterface || type.IsAbstract)
        {
            var impl = allTypes.FirstOrDefault(t => !t.IsInterface && !t.IsAbstract && type.IsAssignableFrom(t))
                ?? throw new InvalidOperationException(
                    $"No concrete implementation of '{type.Name}' found in example '{patternId}'.");
            return CreateInstance(impl, allTypes, depth + 1, patternId);
        }

        // Конкретный класс: конструктор с наименьшим числом параметров.
        var ctor = type.GetConstructors()
            .OrderBy(c => c.GetParameters().Length)
            .FirstOrDefault()
            ?? throw new InvalidOperationException($"No public constructor for '{type.Name}' in '{patternId}'.");

        var parameters = ctor.GetParameters()
            .Select(p => CreateInstance(p.ParameterType, allTypes, depth + 1, patternId))
            .ToArray();

        return ctor.Invoke(parameters);
    }
}

public sealed record CompilationResult(string PatternId, bool Success, IReadOnlyList<string> Errors)
{
    public override string ToString() =>
        Success
            ? $"{PatternId}: OK"
            : $"{PatternId}: {Errors.Count} error(s){Environment.NewLine}{string.Join(Environment.NewLine, Errors)}";
}
