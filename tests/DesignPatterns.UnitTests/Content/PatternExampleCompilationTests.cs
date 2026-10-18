using DesignPatterns.Tools.ExampleCompiler;
using FluentAssertions;

namespace DesignPatterns.UnitTests.Content;

/// <summary>
/// Этап 4 (гарантия качества контента): каждый C#-пример из JSON-контента
/// обязан компилироваться через Roslyn без ошибок. Это механическая проверка
/// требования «валидный C#, а не псевдокод» для ВСЕХ паттернов — добавлять
/// новые примеры можно только если они собираются.
/// </summary>
public class PatternExampleCompilationTests
{
    [Fact]
    public void AllExamples_InContent_CompileWithoutErrors()
    {
        var examples = ExampleCompilationService.LoadAllExamples();
        examples.Should().NotBeEmpty("content must contain code examples");

        var failures = examples
            .Select(kv => ExampleCompilationService.Compile(kv.Key, kv.Value))
            .Where(r => !r.Success)
            .ToList();

        failures.Should().BeEmpty(
            $"all pattern examples must be valid compilable C#{Environment.NewLine}" +
            string.Join(Environment.NewLine + Environment.NewLine, failures));
    }

    [Theory]
    [InlineData("abstract-factory")]
    [InlineData("adapter")]
    [InlineData("strategy")]
    public void Example_ContainsDemoType(string patternId)
    {
        var examples = ExampleCompilationService.LoadAllExamples();
        examples.Should().ContainKey(patternId);

        // Demo-класс с public static Run — контракт исполняемости примера.
        examples[patternId].Should().Contain("class Demo")
            .And.Contain("static");
    }
}
