using DesignPatterns.Tools.ExampleCompiler;
using FluentAssertions;

namespace DesignPatterns.UnitTests.Content;

/// <summary>
/// Поведенческие тесты примеров Этапа 4 (батч 3 — структурные):
/// Bridge, Composite, Decorator, Facade, Proxy.
/// Каждый пример компилируется Roslyn'ом и исполняется через Demo.Run().
/// </summary>
public class PatternExamplesBatch3StructuralTests
{
    private static string Source(string id) => ExampleCompilationService.LoadAllExamples()[id];

    [Fact]
    public void Bridge_AbstractionDelegatesToIndependentImplementor()
    {
        // Одна и та же абстракция Shape работает с двумя реализациями IDrawApi.
        ExampleCompilationService.ExecuteDemoRun("bridge", Source("bridge"))
            .Should().Be("r16 circle 2.0|vec circle 2.0|vec dot 1.0,1.0");
    }

    [Fact]
    public void Composite_RecursiveCostAndDescribe()
    {
        // Рекурсивная агрегация: стоимость дерева и описание части-целого.
        ExampleCompilationService.ExecuteDemoRun("composite", Source("composite"))
            .Should().Be("18|big[sword:10,small[ring:5,coin:3]]");
    }

    [Fact]
    public void Decorator_OrderOfWrappingMatters()
    {
        // Наслаивание декораторов: порядок обёрток меняет результат.
        ExampleCompilationService.ExecuteDemoRun("decorator", Source("decorator"))
            .Should().Be("Hello!--sysadmin|[CRIT]Hello!--sysadmin--sysadmin");
    }

    [Fact]
    public void Facade_SingleEntryThroughSubsystem()
    {
        // Фасад инкапсулирует конвейер из трёх классов подсистемы.
        ExampleCompilationService.ExecuteDemoRun("facade", Source("facade"))
            .Should().Be("fun.avi->mp4+fun.avi.mp3");
    }

    [Fact]
    public void Proxy_SecondCallServedFromCache()
    {
        // Заместитель обращается к реальному объекту только один раз.
        ExampleCompilationService.ExecuteDemoRun("proxy", Source("proxy"))
            .Should().Be("video-7|video-7(cached)|requests:1");
    }

    [Theory]
    [InlineData("bridge")]
    [InlineData("composite")]
    [InlineData("decorator")]
    [InlineData("facade")]
    [InlineData("proxy")]
    public void Batch3_PatternPresentInBothLanguagesWithIdenticalCode(string id)
    {
        var ru = Source(id);
        // LoadAllExamples уже объединяет RU/EN и проверяет идентичность кода
        // через ValidatePair; дополнительно убеждаемся, что диаграмма не пустая.
        ru.Should().Contain("class");
        ru.Should().Contain("Demo");
    }
}
