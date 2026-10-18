using DesignPatterns.Tools.ExampleCompiler;
using FluentAssertions;

namespace DesignPatterns.UnitTests.Content;

/// <summary>
/// Поведенческие тесты примеров Этапа 4 (батч 1): каждый пример не только
/// компилируется (PatternExampleCompilationTests), но и исполняется через
/// Demo.Run(...) с проверкой канонического результата.
/// Это выполняет требование «Unit: проверка логики каждого примера паттерна».
/// </summary>
public class PatternExamplesBatch1BehaviorTests
{
    [Fact]
    public void AbstractFactory_Run_ReturnsDarkFamilyProducts()
    {
        var source = ExampleCompilationService.LoadAllExamples()["abstract-factory"];

        // Тёмное семейство: продукты не смешиваются со светлыми.
        var dark = RunAbstractFactory(source, dark: true);
        dark.Should().Be("[dark button] {dark menu}");

        var light = RunAbstractFactory(source, dark: false);
        light.Should().Be("[light button] {light menu}");
    }

    [Fact]
    public void Adapter_Run_TranslatesCallsToLegacySdk()
    {
        var source = ExampleCompilationService.LoadAllExamples()["adapter"];

        var output = ExampleCompilationService.ExecuteDemoRun("adapter", source);

        // Адаптер транслирует Log("hello") в OldLogSdk.WriteLine(..., level:1).
        output.Should().Be("L1: hello");
    }

    [Fact]
    public void Strategy_Run_SelectsAlgorithmDynamically()
    {
        var source = ExampleCompilationService.LoadAllExamples()["strategy"];

        // Контракт ExecuteDemoRun: int[] -> [], bool -> false (ascending).
        // Пустой вход должен возвращаться как пустой массив без исключений.
        var asc = ExampleCompilationService.ExecuteDemoRun("strategy", source);
        asc.Should().Be("");

        // Демонстрация смены алгоритма на лету — вызываем Demo.Run напрямую
        // через компиляцию и reflection, передавая реальные данные.
        var results = ExampleCompilationService.ExecuteDemoRunWithArgs(
            "strategy", source,
            args: [new int[] { 3, 1, 2 }, false]);
        results.Should().Be("1,2,3");

        var descResults = ExampleCompilationService.ExecuteDemoRunWithArgs(
            "strategy", source,
            args: [new int[] { 3, 1, 2 }, true]);
        descResults.Should().Be("3,2,1");
    }

    private static string RunAbstractFactory(string source, bool dark)
    {
        // DarkFactory/LightFactory выбираются по имени реализации IUIFactory:
        // используем перегрузку ExecuteDemoRunWithArgument, чтобы явно указать фабрику.
        return ExampleCompilationService.ExecuteDemoRun(
            "abstract-factory", source,
            argFilter: t => t.Name == (dark ? "DarkFactory" : "LightFactory"));
    }
}
