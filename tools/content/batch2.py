#!/usr/bin/env python3
"""Батч 2 Этапа 4: порождающие паттерны (Builder, Prototype, Singleton, Factory Method, DI).

Использование: python3 tools/content/batch2.py
Скрипт идемпотентен: паттерны с существующим id не дублируются.
"""
import json
import os

ROOT = os.path.join(os.path.dirname(__file__), "..", "..", "src", "DesignPatterns.Core", "Content", "data")

BUILDER_CODE = '''public sealed class MealBuilder
{
    private string _drink = "water";
    private string _side = "none";
    private bool _dessert;

    public MealBuilder WithDrink(string drink) { _drink = drink; return this; }
    public MealBuilder WithSide(string side) { _side = side; return this; }
    public MealBuilder WithDessert() { _dessert = true; return this; }

    public string Build() => $"{_drink}+{_side}{(_dessert ? "+cake" : string.Empty)}";
}

public static class Demo
{
    public static string Run() =>
        new MealBuilder().WithDrink("juice").WithSide("fries").WithDessert().Build();
}

// Demo.Run() -> "juice+fries+cake"'''

PROTOTYPE_CODE = '''public abstract class DocTemplate
{
    public string Title { get; set; } = "";
    public List<string> Tags { get; init; } = [];
    public abstract DocTemplate Clone();
}

public sealed class ReportTemplate : DocTemplate
{
    public int Year { get; set; }
    public override DocTemplate Clone() => new ReportTemplate
    {
        Title = Title,
        Year = Year,
        Tags = [.. Tags], // глубокая копия списка тегов
    };
}

public static class Demo
{
    public static string Run()
    {
        var original = new ReportTemplate { Title = "Q1", Year = 2026 };
        original.Tags.Add("finance");

        var copy = (ReportTemplate)original.Clone();
        copy.Title = "Q2";
        copy.Tags.Add("draft");

        // Оригинальные теги не затронуты — значит клонирование глубокое.
        return $"{original.Title}:{original.Tags.Count}|{copy.Title}:{copy.Tags.Count}";
    }
}

// Demo.Run() -> "Q1:1|Q2:2"'''

SINGLETON_CODE = '''public sealed class AppConfig
{
    public static readonly AppConfig Instance = new();
    private AppConfig() { Version = "1.0"; }

    public string Version { get; set; }
}

public static class Demo
{
    public static string Run()
    {
        var a = AppConfig.Instance;
        var b = AppConfig.Instance;
        a.Version = "2.0";
        // Обе ссылки указывают на один экземпляр — изменение видно всем.
        return ReferenceEquals(a, b) ? b.Version : "broken";
    }
}

// Demo.Run() -> "2.0"'''

FACTORY_METHOD_CODE = '''public interface ILogReader
{
    string Read();
}

public sealed class FileLogReader : ILogReader
{
    public string Read() => "file log";
}

public sealed class MemoryLogReader : ILogReader
{
    public string Read() => "memory log";
}

public abstract class LogSource
{
    public string Analyze() => $"analyzed: {CreateReader().Read()}";

    protected abstract ILogReader CreateReader();
}

public sealed class FileLogSource : LogSource
{
    protected override ILogReader CreateReader() => new FileLogReader();
}

public sealed class MemoryLogSource : LogSource
{
    protected override ILogReader CreateReader() => new MemoryLogReader();
}

public static class Demo
{
    public static string Run(LogSource source) => source.Analyze();
}

// Demo.Run(new FileLogSource()) -> "analyzed: file log"'''

DI_CODE = '''public interface IClock
{
    string Now();
}

public sealed class SystemClock : IClock
{
    public string Now() => DateTime.UtcNow.ToString("HH:mm");
}

public sealed class FixedClock : IClock
{
    public string Now() => "12:00";
}

public sealed class ReportService(IClock clock)
{
    public string Header() => $"report at {clock.Now()}";
}

public static class Container
{
    private static readonly Dictionary<Type, Func<object>> Registrations = [];

    public static void Register<T>(Func<T> factory) => Registrations[typeof(T)] = () => factory()!;

    public static T Resolve<T>() => (T)Registrations[typeof(T)]();
}

public static class Demo
{
    public static string Run()
    {
        Container.Register<IClock>(() => new FixedClock());
        var service = new ReportService(Container.Resolve<IClock>());
        return service.Header();
    }
}

// Demo.Run() -> "report at 12:00"'''

RU_PATTERNS = [
    dict(
        id="builder", name="Строитель",
        intent="Последовательно создаёт сложный объект, отделяя процесс сборки от его представления.",
        problem="Конструктор объекта разрастается из-за множества опциональных параметров и проверок инвариантов; появляются телескопические конструкторы.",
        solution="Вынести сборку в отдельный «строитель» с цепочкой методов With... и финальным Build(), который проверяет инварианты.",
        applicability=["Создание сложных объектов с опциями и ограничениями", "Нужны разные представления одного процесса сборки", "Требуется неизменяемый результат"],
        pros=["Пошаговая читаемая сборка", "Централизация проверок инвариантов", "Легко добавлять новые шаги"],
        cons=["Больше кода для простых объектов", "Строитель часто знает внутренности продукта"],
        diagram=dict(title="Builder", nodes=[
            dict(id="builder", name="MealBuilder", kind="class", members=["WithDrink(drink): MealBuilder", "WithSide(side): MealBuilder", "WithDessert(): MealBuilder", "Build(): string"], col=0, row=0),
            dict(id="product", name="Meal (string)", kind="class", members=[], col=1, row=1)],
            links=[{"from": "builder", "to": "product", "kind": "dependency", "label": "creates"}]),
        code=dict(title="Комбо-обед", language="csharp", source=BUILDER_CODE)),
    dict(
        id="prototype", name="Прототип",
        intent="Создаёт новые объекты путём копирования существующего экземпляра-прототипа.",
        problem="Создание объекта «с нуля» дорого или требует знания приватных полей; нужно быстро получать похожие конфигурации.",
        solution="Реализовать метод Clone(), копирующий состояние объекта (глубоко — списки/словари), и производить новые экземпляры из прототипа.",
        applicability=["Избегать подклассов ради параметризации", "Копировать сложные графы объектов", "Создавать объекты без знания их конкретных классов"],
        pros=["Быстрое создание настроенных объектов", "Не нужен код инициализации", "Работает с неизвестными классами через абстракцию"],
        cons=["Глубокое копирование нетривиально (циклы)", "Клонирование скрытых ссылок легко забыть"],
        diagram=dict(title="Prototype", nodes=[
            dict(id="proto", name="DocTemplate", kind="abstract", members=["Title: string", "Tags: List<string>", "Clone(): DocTemplate"], col=0, row=0),
            dict(id="concrete", name="ReportTemplate", kind="class", members=["Year: int", "Clone(): DocTemplate"], col=0, row=1)],
            links=[{"from": "concrete", "to": "proto", "kind": "inheritance"}]),
        code=dict(title="Шаблоны отчётов", language="csharp", source=PROTOTYPE_CODE)),
    dict(
        id="singleton", name="Одиночка",
        intent="Гарантирует единственность экземпляра класса и даёт глобальную точку доступа к нему.",
        problem="В приложении должен быть ровно один экземпляр ресурса (конфигурация, логгер, пул), доступный из любой точки.",
        solution="Закрыть конструктор и открыть статическое свойство Instance, инициализируемое однократно и потокобезопасно.",
        applicability=["Ровно один экземпляр с контролируемым доступом", "Единая точка управления общим ресурсом"],
        pros=["Простая глобальная точка доступа", "Отложенная инициализация", "Экономия ресурсов на дорогих объектах"],
        cons=["Глобальное состояние усложняет тесты", "Скрытые зависимости между модулями", "Нарушает SRP, если хранит логику"],
        diagram=dict(title="Singleton", nodes=[
            dict(id="singleton", name="AppConfig", kind="class", members=["-Instance: AppConfig", "-AppConfig()", "+Version: string"], col=0, row=0)],
            links=[]),
        code=dict(title="Конфигурация приложения", language="csharp", source=SINGLETON_CODE)),
    dict(
        id="factory-method", name="Фабричный метод",
        intent="Определяет интерфейс создания объекта, но позволяет подклассам решать, какой класс instantiate.",
        problem="Класс не может заранее знать, какие подтипы продуктов ему создавать, но хочет централизовать общий код работы с ними.",
        solution="Вынести создание продукта в защищённый абстрактный фабричный метод; подклассы переопределяют его, возвращая нужный продукт.",
        applicability=["Класс заранее не знает типов создаваемых продуктов", "Библиотека хочет отдавать пользователям точки расширения", "Переиспользование логики над разными продуктами"],
        pros=["Инкапсуляция создания в одном месте", "Лёгкое добавление новых продуктов", "Слабая связанность клиента с конкретными классами"],
        cons=["Для каждого продукта — свой подкласс", "Иерархия сложнее, чем простой new"],
        diagram=dict(title="Factory Method", nodes=[
            dict(id="creator", name="LogSource", kind="abstract", members=["Analyze(): string", "CreateReader(): ILogReader"], col=0, row=0),
            dict(id="product", name="ILogReader", kind="interface", members=["Read(): string"], col=1, row=0),
            dict(id="file", name="FileLogSource", kind="class", members=["CreateReader(): ILogReader"], col=0, row=1),
            dict(id="mem", name="MemoryLogSource", kind="class", members=["CreateReader(): ILogReader"], col=1, row=1)],
            links=[{"from": "file", "to": "creator", "kind": "inheritance"},
                   {"from": "mem", "to": "creator", "kind": "inheritance"},
                   {"from": "creator", "to": "product", "kind": "dependency", "label": "creates"},
                   {"from": "file", "to": "product", "kind": "realization"},
                   {"from": "mem", "to": "product", "kind": "realization"}]),
        code=dict(title="Источники логов", language="csharp", source=FACTORY_METHOD_CODE)),
    dict(
        id="dependency-injection", name="Внедрение зависимостей",
        intent="Передаёт зависимости объекту извне (через конструктор), вместо самостоятельного создания внутри.",
        problem="Класс сам создаёт конкретные зависимости — его нельзя переиспользовать и протестировать с заменой (например, часов).",
        solution="Описать зависимости через интерфейсы, принимать их в конструкторе и собирать объект в DI-контейнере.",
        applicability=["Тестируемость через подмены зависимостей", "Гибкая замена реализаций без правок клиента", "Явные контракты класса"],
        pros=["Разделяет высокоуровневый и низкоуровневый код", "Проще параллельная разработка", "Минимальные правки при замене реализации"],
        cons=["Инфраструктура контейнера ради простых случаев", "Риск «сервис-локатора» вместо явного внедрения"],
        diagram=dict(title="Dependency Injection", nodes=[
            dict(id="service", name="ReportService", kind="class", members=["Header(): string", "ctor(clock: IClock)"], col=0, row=0),
            dict(id="abstraction", name="IClock", kind="interface", members=["Now(): string"], col=1, row=0),
            dict(id="sys", name="SystemClock", kind="class", members=["Now(): string"], col=1, row=1),
            dict(id="fixed", name="FixedClock", kind="class", members=["Now(): string"], col=2, row=1)],
            links=[{"from": "service", "to": "abstraction", "kind": "dependency", "label": "uses"},
                   {"from": "sys", "to": "abstraction", "kind": "realization"},
                   {"from": "fixed", "to": "abstraction", "kind": "realization"}]),
        code=dict(title="Сервис отчётов с часами", language="csharp", source=DI_CODE)),
]

EN_TEXTS = [
    dict(
        id="builder", name="Builder",
        intent="Separates the construction of a complex object from its representation so the same process can create different representations.",
        problem="Object construction balloons with optional parameters and invariant checks, leading to telescoping constructors.",
        solution="Extract assembly into a separate builder with a chain of With... methods and a final Build() that validates invariants.",
        applicability=["Constructing complex objects with options and constraints", "The same assembly process must produce different representations", "An immutable result is required"],
        pros=["Step-by-step readable construction", "Invariant checks are centralized", "Easy to add new steps"],
        cons=["More code for simple objects", "The builder often knows product internals"],
        code=dict(title="Combo meal", language="csharp", source=BUILDER_CODE)),
    dict(
        id="prototype", name="Prototype",
        intent="Creates new objects by copying an existing prototype instance.",
        problem="Building an object from scratch is expensive or requires private state; similar configurations must be produced quickly.",
        solution="Implement a Clone() method that copies object state (deeply for lists/dictionaries) and produce new instances from the prototype.",
        applicability=["Avoid subclassing just for parameterization", "Copy complex object graphs", "Create objects without knowing their concrete classes"],
        pros=["Fast creation of configured objects", "No initialization code needed", "Works with unknown classes through an abstraction"],
        cons=["Deep copy is non-trivial (cycles)", "Hidden references are easy to forget when cloning"],
        code=dict(title="Report templates", language="csharp", source=PROTOTYPE_CODE)),
    dict(
        id="singleton", name="Singleton",
        intent="Guarantees a single instance of a class and provides a global access point to it.",
        problem="Exactly one instance of a resource (config, logger, pool) must exist and be reachable from anywhere.",
        solution="Seal the constructor and expose a static Instance property initialized once in a thread-safe way.",
        applicability=["Exactly one controlled instance", "A single control point over a shared resource"],
        pros=["Simple global access", "Lazy initialization", "Saves resources on expensive objects"],
        cons=["Global state complicates testing", "Hidden dependencies between modules", "Violates SRP if it holds behavior"],
        code=dict(title="Application configuration", language="csharp", source=SINGLETON_CODE)),
    dict(
        id="factory-method", name="Factory Method",
        intent="Defines an interface for creating objects but lets subclasses decide which class to instantiate.",
        problem="A class cannot know upfront which product subtypes to create yet wants to centralize common workflow code around them.",
        solution="Extract product creation into a protected abstract factory method; subclasses override it to return the desired product.",
        applicability=["A class does not know product types upfront", "A library wants to offer extension points to users", "Reuse logic across different products"],
        pros=["Creation encapsulated in one place", "New products are easy to add", "Clients decoupled from concrete classes"],
        cons=["Each product needs its own subclass", "Deeper hierarchy than plain new"],
        code=dict(title="Log sources", language="csharp", source=FACTORY_METHOD_CODE)),
    dict(
        id="dependency-injection", name="Dependency Injection",
        intent="Supplies an object's dependencies from the outside (via constructor) instead of letting it create them itself.",
        problem="A class creates its concrete dependencies itself, making reuse and testing with substitutes (e.g. clocks) impossible.",
        solution="Describe dependencies as interfaces, accept them via the constructor, and compose the object in a DI container.",
        applicability=["Testability via substitute dependencies", "Swap implementations without touching clients", "Explicit class contracts"],
        pros=["Separates high- and low-level code", "Easier parallel development", "Minimal changes when swapping implementations"],
        cons=["Container infrastructure for trivial cases", "Risk of drifting toward a service locator"],
        code=dict(title="Report service with clocks", language="csharp", source=DI_CODE)),
]


def merge(lang):
    path = os.path.normpath(os.path.join(ROOT, f"patterns-{lang}.json"))
    doc = json.load(open(path))
    creational = next(c for c in doc["categories"] if c["id"] == "creational")
    have = {p["id"] for p in creational["patterns"]}
    added = 0
    for i, ru in enumerate(RU_PATTERNS):
        pid = ru["id"]
        if pid in have:
            continue
        if lang == "ru":
            entry = dict(ru)
        else:
            entry = dict(EN_TEXTS[i])
            # Диаграммы идентичны в обоих языках (валидатор это требует).
            entry["diagram"] = ru["diagram"]
        creational["patterns"].append(entry)
        added += 1
    with open(path, "w") as f:
        json.dump(doc, f, ensure_ascii=False, indent=2)
        f.write("\n")
    total = sum(len(c["patterns"]) for c in doc["categories"])
    print(f"{lang}: +{added}, total {total}")


if __name__ == "__main__":
    merge("ru")
    merge("en")
