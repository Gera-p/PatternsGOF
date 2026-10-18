using DesignPatterns.Tools.ExampleCompiler;
using FluentAssertions;

namespace DesignPatterns.UnitTests.Content;

/// <summary>
/// Поведенческие тесты примеров Этапа 4 (батч 4 — поведенческие, волна 1):
/// Chain of Responsibility, Command, Iterator, Mediator, Observer.
/// Каждый пример компилируется Roslyn'ом и исполняется через Demo.Run(...).
/// </summary>
public class PatternExamplesBatch4BehavioralTests
{
    private static string Source(string id) => ExampleCompilationService.LoadAllExamples()[id];

    [Theory]
    [InlineData(1, "first-line")]
    [InlineData(2, "second-line")]
    [InlineData(3, "manager")]
    [InlineData(9, "none")]
    public void ChainOfResponsibility_RequestTravelsUntilHandled(int severity, string expected)
    {
        // Запрос проходит по цепочке, пока не найдётся способный обработчик.
        ExampleCompilationService.ExecuteDemoRunWithArgs(
                "chain-of-responsibility", Source("chain-of-responsibility"), severity)
            .Should().Be(expected);
    }

    [Theory]
    [InlineData("on", "on")]
    [InlineData("on,off", "off")]
    [InlineData("on,off,undo", "on")]           // undo откатывает выключение
    [InlineData("on,on,undo,undo", "off")]      // стек истории команд
    [InlineData("undo", "off")]                 // пустая история — без падения
    public void Command_UndoRestoresPreviousState(string script, string expected)
    {
        ExampleCompilationService.ExecuteDemoRunWithArgs(
                "command", Source("command"), script)
            .Should().Be(expected);
    }

    [Fact]
    public void Iterator_TraversesWithoutExposingStorage()
    {
        ExampleCompilationService.ExecuteDemoRunWithArgs(
                "iterator", Source("iterator"), "a|b|c")
            .Should().Be("a,b,c");
    }

    [Theory]
    [InlineData("A>hi", "0-1-0")]
    [InlineData("A>hi,B>yo", "0-1-1")]
    [InlineData("C>hey,A>hi", "1-1-0")]
    public void Mediator_UsersCommunicateOnlyThroughRoom(string script, string expected)
    {
        // Ann/Bob/Cat не знают друг о друге — маршрутизирует ChatRoom.
        ExampleCompilationService.ExecuteDemoRunWithArgs(
                "mediator", Source("mediator"), script)
            .Should().Be(expected);
    }

    [Theory]
    [InlineData(false, "1-1")] // оба подписчика получили событие
    [InlineData(true, "1-0")]  // sms отписался до рассылки
    public void Observer_SubscribersNotifiedDynamically(bool unsubscribeBeforeNotify, string expected)
    {
        ExampleCompilationService.ExecuteDemoRunWithArgs(
                "observer", Source("observer"), unsubscribeBeforeNotify)
            .Should().Be(expected);
    }

    [Theory]
    [InlineData("chain-of-responsibility")]
    [InlineData("command")]
    [InlineData("iterator")]
    [InlineData("mediator")]
    [InlineData("observer")]
    public void Batch4_ExampleCompilesCleanly(string patternId)
    {
        var result = ExampleCompilationService.Compile(patternId, Source(patternId));
        result.Success.Should().BeTrue(string.Join("; ", result.Errors));
    }
}
