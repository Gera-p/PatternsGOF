using DesignPatterns.Tools.ExampleCompiler;
using FluentAssertions;

namespace DesignPatterns.UnitTests.Content;

/// <summary>
/// Поведенческие тесты примеров Этапа 4 (батч 2 — завершение порождёющих):
/// Builder, Prototype, Singleton, Factory Method, Dependency Injection.
/// Каждый пример компилируется Roslyn'ом и исполняется через Demo.Run(...).
/// </summary>
public class PatternExamplesBatch2CreationalTests
{
    private static string Source(string id) => ExampleCompilationService.LoadAllExamples()[id];

    // Экземпляры подклассов LogSource для Factory Method (типы из example-сборки).
    private static readonly Lazy<(object File, object Memory)> LogSources = new(() =>
    {
        var source = Source("factory-method");
        return (ExampleCompilationService.CreateDemoInstance(source, "FileLogSource"),
                ExampleCompilationService.CreateDemoInstance(source, "MemoryLogSource"));
    });

    [Fact]
    public void Builder_ChainedConstruction_ProducesFullCombo()
    {
        // Цепочка With... + Build(): продукт собран из шагов.
        ExampleCompilationService.ExecuteDemoRun("builder", Source("builder"))
            .Should().Be("juice+fries+cake");
    }

    [Fact]
    public void Prototype_DeepClone_LeavesOriginalUntouched()
    {
        // Клон меняет Title и теги — оригинал остаётся Q1 с одним тегом.
        ExampleCompilationService.ExecuteDemoRun("prototype", Source("prototype"))
            .Should().Be("Q1:1|Q2:2");
    }

    [Fact]
    public void Singleton_TwoAccesses_SameMutableInstance()
    {
        // Изменение через одну ссылку видно через другую — экземпляр один.
        ExampleCompilationService.ExecuteDemoRun("singleton", Source("singleton"))
            .Should().Be("2.0");
    }

    [Fact]
    public void FactoryMethod_SubclassDecidesProduct()
    {
        var source = Source("factory-method");

        // Фабричный метод в подклассе определяет продукт: File -> "file log".
        ExampleCompilationService.ExecuteDemoRunWithArgs(
                "factory-method", source, args: [LogSources.Value.File])
            .Should().Be("analyzed: file log");

        ExampleCompilationService.ExecuteDemoRunWithArgs(
                "factory-method", source, args: [LogSources.Value.Memory])
            .Should().Be("analyzed: memory log");
    }

    [Fact]
    public void DependencyInjection_ContainerResolvesAbstraction()
    {
        // Контейнер внедряет FixedClock -> детерминированный заголовок отчёта.
        ExampleCompilationService.ExecuteDemoRun("dependency-injection", Source("dependency-injection"))
            .Should().Be("report at 12:00");
    }
}
