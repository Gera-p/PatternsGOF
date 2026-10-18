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
    public static string ExecuteDemoRun(string patternId, string source) =>
        ExecuteDemoRun(patternId, source, argFilter: null);

    /// <summary>
    /// Перегрузка с фильтром: если параметр Run() — интерфейс/абстракция и среди
    /// реализаций есть несколько подходящих, argFilter выбирает конкретный тип
    /// (например, DarkFactory vs LightFactory в Абстрактной фабрике).
    /// </summary>
    public static string ExecuteDemoRun(string patternId, string source, Func<Type, bool>? argFilter)
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
            .Select(p => CreateInstance(p.ParameterType, types, depth: 0, patternId, argFilter))
            .ToArray();

        return Format(runMethod.Invoke(null, args));
    }

    /// <summary>
    /// Исполняет Demo.Run(...) с явными аргументами (по позициям). Результат
    /// форматируется так же, как в ExecuteDemoRun: массивы — через "a,b,c".
    /// Аргументы могут быть получены из другой компиляции того же исходника
    /// (см. CreateDemoInstance): типы идентичны по имени, но reside в разных
    /// assembly, поэтому IsInstanceOfType ложно. Для таких случаев выполняется
    /// повторная загрузка байткода из кэша — тот же assembly = те же RuntimeType.
    /// </summary>
    public static string ExecuteDemoRunWithArgs(string patternId, string source, params object?[] args)
    {
        var assembly = CompileAndLoad(source, patternId);
        var demoType = assembly.GetTypes().First(t => t.Name == "Demo");
        var runMethod = demoType.GetMethods(BindingFlags.Public | BindingFlags.Static)
            .First(m => m.Name == "Run");

        var parameters = runMethod.GetParameters();
        if (parameters.Length != args.Length)
            throw new ArgumentException(
                $"Demo.Run of '{patternId}' expects {parameters.Length} arg(s), got {args.Length}.");

        var coerced = new object?[args.Length];
        for (var i = 0; i < args.Length; i++)
        {
            var value = args[i];
            var targetType = parameters[i].ParameterType;

            if (value is null || targetType.IsInstanceOfType(value))
            {
                coerced[i] = value;
                continue;
            }

            // Cross-assembly identity: значение создано из другой компиляции того же
            // примера. Если имя типа значения совпадает с именем параметра (или он
            // является его базовым классом/интерфейсом в том же исходнике) —
            // пересоздаём экземпляр внутри КЭШИРОВАННОЙ сборки этого же паттерна.
            var valueType = value.GetType();
            if (IsSameSourceDeclaredType(valueType, targetType))
            {
                coerced[i] = CreateDemoInstanceFromCache(patternId, source, valueType.Name);
                continue;
            }

            // Примитивы/строки — стандартное приведение.
            coerced[i] = Convert.ChangeType(value, Nullable.GetUnderlyingType(targetType) ?? targetType);
        }

        return Format(runMethod.Invoke(null, coerced));
    }

    private static bool IsSameSourceDeclaredType(Type valueType, Type targetType)
    {
        if (string.Equals(valueType.Name, targetType.Name, StringComparison.Ordinal))
            return true;

        // Целевой тип — абстракция из того же примера: проверяем, что valueType
        // реализует/наследует targetType ПО ИМЕНИ в графе наследования valueType.
        for (var t = valueType.BaseType; t is not null && t != typeof(object); t = t.BaseType)
            if (string.Equals(t.Name, targetType.Name, StringComparison.Ordinal))
                return true;

        foreach (var iface in valueType.GetInterfaces())
            if (string.Equals(iface.Name, targetType.Name, StringComparison.Ordinal))
                return true;

        return false;
    }

    // Кэш байткода компиляции на исходник: гарантирует, что все reflection-операции над
    // одним примером используют ОДНУ сборку (одинаковые RuntimeType'ы).
    private static readonly Dictionary<string, Assembly> AssemblyCache = new();
    private static readonly object AssemblyCacheLock = new();

    private static Assembly CompileAndLoad(string source, string patternId)
    {
        var key = patternId + "|" + source.GetHashCode(StringComparison.Ordinal);
        lock (AssemblyCacheLock)
        {
            if (AssemblyCache.TryGetValue(key, out var cached))
                return cached;

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
            AssemblyCache[key] = assembly;
            return assembly;
        }
    }

    private static object CreateDemoInstanceFromCache(string patternId, string source, string typeName)
    {
        var assembly = CompileAndLoad(source, patternId);
        var type = assembly.GetTypes().FirstOrDefault(t => t.Name == typeName)
            ?? throw new InvalidOperationException($"Type '{typeName}' not found in cached example '{patternId}'.");
        return Activator.CreateInstance(type)!;
    }

    private static string Format(object? result) => result switch
    {
        null => string.Empty,
        string s => s,
        System.Collections.IEnumerable e when result is not string =>
            string.Join(",", e.Cast<object?>().Select(x => x?.ToString() ?? string.Empty)),
        _ => result.ToString() ?? string.Empty,
    };

    /// <summary>
    /// Компилирует пример и создаёт new-экземпляр конкретного класса по имени
    /// (используется для передачи продуктов в Demo.Run с параметрами).
    /// </summary>
    public static object CreateDemoInstance(string source, string typeName)
    {
        var syntaxTree = ParseExample(source);

        var compilation = CSharpCompilation.Create(
            assemblyName: $"PatternTypes_{typeName}_{Guid.NewGuid():N}",
            syntaxTrees: [syntaxTree],
            references: ReferencePaths.Value.Select(r => MetadataReference.CreateFromFile(r)),
            options: new Microsoft.CodeAnalysis.CSharp.CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        using var ms = new MemoryStream();
        var emit = compilation.Emit(ms);
        if (!emit.Success)
        {
            var errors = string.Join(Environment.NewLine,
                emit.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
            throw new InvalidOperationException($"Failed to compile for '{typeName}':{Environment.NewLine}{errors}");
        }

        ms.Seek(0, SeekOrigin.Begin);
        var assembly = Assembly.Load(ms.ToArray());
        var type = assembly.GetTypes().FirstOrDefault(t => t.Name == typeName)
            ?? throw new InvalidOperationException($"Type '{typeName}' not found in example.");
        return Activator.CreateInstance(type)!;
    }

    private static object? CreateInstance(Type type, Type[] allTypes, int depth, string patternId, Func<Type, bool>? argFilter)
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
            var candidates = allTypes
                .Where(t => !t.IsInterface && !t.IsAbstract && type.IsAssignableFrom(t))
                .ToList();
            var impl = argFilter is null
                ? candidates.FirstOrDefault()
                : candidates.FirstOrDefault(argFilter)
                  ?? throw new InvalidOperationException(
                      $"argFilter matched no implementation of '{type.Name}' in example '{patternId}'.");
            if (impl is null)
                throw new InvalidOperationException(
                    $"No concrete implementation of '{type.Name}' found in example '{patternId}'.");
            return CreateInstance(impl, allTypes, depth + 1, patternId, argFilter: null);
        }

        // Конкретный класс: конструктор с наименьшим числом параметров.
        var ctor = type.GetConstructors()
            .OrderBy(c => c.GetParameters().Length)
            .FirstOrDefault()
            ?? throw new InvalidOperationException($"No public constructor for '{type.Name}' in '{patternId}'.");

        var parameters = ctor.GetParameters()
            .Select(p => CreateInstance(p.ParameterType, allTypes, depth + 1, patternId, argFilter: null))
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
