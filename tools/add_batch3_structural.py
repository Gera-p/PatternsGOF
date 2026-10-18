#!/usr/bin/env python3
"""Этап 4, батч 3: добавляет 5 структурных паттернов в patterns-ru/en.json.

Идемпотентен: паттерны, уже присутствующие в документе, пропускаются.
Код (code.source) идентичен в RU и EN — это инвариант, который проверяет
PatternContentValidator.ValidatePair + тесты компиляции.
"""
import json, sys, pathlib

DATA = pathlib.Path(__file__).resolve().parents[1] / "src/DesignPatterns.Core/Content/data"

NODES = lambda *ns: [{"id": i, "name": n, "kind": k, "members": m or [], "col": c, "row": r}
                     for (i, n, k, m, c, r) in ns]
LINK = lambda f, t, k, l=None: {"from": f, "to": t, "kind": k, "label": l}

BRIDGE_CODE = '''public interface IDrawApi
{
    string DrawCircle(double radius);
    string DrawDot(double x, double y);
}

public sealed class Raster16Api : IDrawApi
{
    public string DrawCircle(double radius) => $"r16 circle {radius:F1}";
    public string DrawDot(double x, double y) => $"r16 dot {x:F1},{y:F1}";
}

public sealed class VectorApi : IDrawApi
{
    public string DrawCircle(double radius) => $"vec circle {radius:F1}";
    public string DrawDot(double x, double y) => $"vec dot {x:F1},{y:F1}";
}

// Абстракция не привязана к конкретной реализации API.
public abstract class Shape
{
    protected Shape(IDrawApi api) => Api = api;
    protected IDrawApi Api { get; }
    public abstract string Render();
}

public sealed class Circle : Shape
{
    private readonly double _radius;
    public Circle(double radius, IDrawApi api) : base(api) => _radius = radius;
    public override string Render() => Api.DrawCircle(_radius);
}

public sealed class Dot : Shape
{
    private readonly double _x, _y;
    public Dot(double x, double y, IDrawApi api) : base(api) { _x = x; _y = y; }
    public override string Render() => Api.DrawDot(_x, _y);
}

public static class Demo
{
    public static string Run()
    {
        var shapes = new List<Shape>
        {
            new Circle(2, new Raster16Api()),
            new Circle(2, new VectorApi()),
            new Dot(1, 1, new VectorApi()),
        };
        return string.Join("|", shapes.ConvertAll(s => s.Render()));
    }
}

// Demo.Run() -> "r16 circle 2.0|vec circle 2.0|vec dot 1.0,1.0"'''

COMPOSITE_CODE = '''public interface IComponent
{
    int GetCost();
    string Describe();
}

// Лист — базовый строительный блок.
public sealed class Leaf : IComponent
{
    private readonly string _name;
    private readonly int _cost;
    public Leaf(string name, int cost) { _name = name; _cost = cost; }
    public int GetCost() => _cost;
    public string Describe() => $"{_name}:{_cost}";
}

// Контейнер: хранит компоненты и рекурсивно агрегирует их поведение.
public sealed class Box : IComponent
{
    private readonly string _name;
    private readonly List<IComponent> _children = [];
    public Box(string name) => _name = name;
    public Box Add(IComponent component) { _children.Add(component); return this; }
    public int GetCost() => _children.Sum(c => c.GetCost());
    public string Describe() => $"{_name}[{string.Join(",", _children.ConvertAll(c => c.Describe()))}]";
}

public static class Demo
{
    public static string Run()
    {
        // Древо: коробка с двумя листами и вложенной коробкой.
        var small = new Box("small").Add(new Leaf("ring", 5)).Add(new Leaf("coin", 3));
        var big = new Box("big").Add(new Leaf("sword", 10)).Add(small);
        return $"{big.GetCost()}|{big.Describe()}";
    }
}

// Demo.Run() -> "18|big[sword:10,small[ring:5,coin:3]]"'''

DECORATOR_CODE = '''public interface INotification
{
    string Content { get; }
}

public sealed class SimpleNotification : INotification
{
    public string Content => "Hello!";
}

// Базовый декоратор: оборачивает компонент, реализуя тот же интерфейс.
public abstract class NotificationDecorator : INotification
{
    private readonly INotification _wrappee;
    protected NotificationDecorator(INotification wrappee) => _wrappee = wrappee;
    public virtual string Content => _wrappee.Content;
}

public sealed class CriticalDecorator : NotificationDecorator
{
    public CriticalDecorator(INotification wrappee) : base(wrappee) { }
    public override string Content => "[CRIT]" + base.Content;
}

public sealed class SignedDecorator : NotificationDecorator
{
    public SignedDecorator(INotification wrappee) : base(wrappee) { }
    public override string Content => base.Content + "--sysadmin";
}

public static class Demo
{
    public static string Run()
    {
        // Оборачивание на лету: порядок decorators меняет результат.
        INotification a = new SignedDecorator(new SimpleNotification());
        INotification b = new CriticalDecorator(new SignedDecorator(a));
        return $"{a.Content}|{b.Content}";
    }
}

// Demo.Run() -> "Hello!--sysadmin|[CRIT]Hello!--sysadmin--sysadmin"'''

FACADE_CODE = '''public sealed class VideoFile
{
    public VideoFile(string filePath) => Name = filePath;
    public string Name { get; }
}

public sealed class CodecFactory
{
    public string Extract(VideoFile file, string codec) => $"{file.Name}->{codec}";
}

public sealed class AudioExtractor
{
    public string EncodeToMp3(VideoFile file) => $"{file.Name}.mp3";
}

public sealed class FlippingWriter
{
    public void Save(string path, string data) { }
}

// Фасад: простой интерфейс к подсистеме из нескольких классов.
public sealed class VideoConverterFacade
{
    private readonly CodecFactory _codecs = new();
    private readonly AudioExtractor _audio = new();
    private readonly FlippingWriter _writer = new();

    public string Convert(string file, string format)
    {
        var source = new VideoFile(file);
        var video = _codecs.Extract(source, format);
        var audioTrack = _audio.EncodeToMp3(source);
        _writer.Save($"{source.Name}.{format}", video);
        return $"{video}+{audioTrack}";
    }
}

public static class Demo
{
    public static string Run() => new VideoConverterFacade().Convert("fun.avi", "mp4");
}

// Demo.Run() -> "fun.avi->mp4+fun.avi.mp3"'''

PROXY_CODE = '''public interface IYouTubeClient
{
    string GetVideo(int id);
}

public sealed class YouTubeApi : IYouTubeClient
{
    public static int RequestCount;
    public string GetVideo(int id) { RequestCount++; return $"video-{id}"; }
}

// Прокси: контролирует доступ + кэширует дорогие запросы.
public sealed class CachedYouTubeProxy : IYouTubeClient
{
    private readonly Dictionary<int, string> _cache = [];
    public string GetVideo(int id)
    {
        if (_cache.TryGetValue(id, out var cached)) return cached + "(cached)";
        var fresh = new YouTubeApi().GetVideo(id);
        _cache[id] = fresh;
        return fresh;
    }
}

public static class Demo
{
    public static string Run()
    {
        YouTubeApi.RequestCount = 0;
        IYouTubeClient client = new CachedYouTubeProxy();
        var first = client.GetVideo(7);
        var second = client.GetVideo(7);
        return $"{first}|{second}|requests:{YouTubeApi.RequestCount}";
    }
}

// Demo.Run() -> "video-7|video-7(cached)|requests:1"'''

RU = {
    "bridge": dict(
        name="Мост",
        intent="Разделяет абстракцию и её реализацию по двум независимым иерархиям, которые можно развивать отдельно.",
        problem="Жёсткая связь между классом-абстракцией и классами-реализациями приводит к взрыву числа подклассов при добавлении новой платформы или варианта.",
        solution="Абстракция хранит ссылку на объект реализации (интерфейс) и делегирует ей работу. Обе стороны общаются только через общий контракт и развиваются независимо.",
        applicability=["Нужно отвязать абстракцию от реализации (кроссплатформенность)",
                      "Хотите менять реализации во время выполнения",
                      "Две orthogonal'ные оси вариативности (форма × API рисования)"],
        pros=["Реализации и абстракции можно расширять независимо",
              "Скрытие деталей реализации от клиента",
              "Подмена поведения без пересборки иерархии"],
        cons=["Усложнение кода из-за разделения на две иерархии",
              "Клиенту нужно знать о обеих иерархиях"],
        title="Мост: Shape ↔ DrawAPI",
        nodes=[("abstraction","Shape","abstract",["#Api: IDrawApi","Render()"],1,0),
               ("refined1","Circle","class",["Render()"],0,1),
               ("refined2","Dot","class",["Render()"],2,1),
               ("implementor","IDrawApi","interface",["DrawCircle(r)","DrawDot(x,y)"],1,2),
               ("concrete1","Raster16Api","class",[],0,3),
               ("concrete2","VectorApi","class",[],2,3)],
        links=[LINK("refined1","abstraction","inheritance"),
               LINK("refined2","abstraction","inheritance"),
               LINK("abstraction","implementor","association","uses"),
               LINK("concrete1","implementor","realization"),
               LINK("concrete2","implementor","realization")],
        codeTitle="Фигуры поверх разных графических API",
        code=BRIDGE_CODE),
    "composite": dict(
        name="Компоновщик",
        intent="Компонурует объекты в древовидную структуру «часть-целое», позволяя клиенту одинаково работать с отдельными объектами и их композициями.",
        problem="Клиенту нужно обрабатывать и одиночные объекты, и группы объектов — при этом код дублирует логику для каждого вида, а обход дерева усложняется.",
        solution="Общий компонентный интерфейс реализуют и листья, и контейнеры. Контейнер хранит коллекцию компонентов и рекурсивно делегирует им операции.",
        applicability=["Предметная область естественным образом образует дерево (папки, UI, меню)",
                      "Клиент должен одинаково трактовать одиночные объекты и композиции",
                      "Нужна единая точка рекурсивного обхода"],
        pros=["Единый интерфейс для листьев и контейнеров",
              "Новые типы компонентов легко добавлять",
              "Рекурсивные операции пишутся один раз"],
        cons=["Сложно ограничить состав компонентов",
              "Избыочно общий интерфейс для специфичных операций"],
        title="Компоновщик: IComponent",
        nodes=[("component","IComponent","interface",["GetCost()","Describe()"],1,0),
               ("leaf","Leaf","class",["_name","_cost"],0,1),
               ("composite","Box","class",["_children: List<IComponent>","Add()"],2,1),
               ("child","IComponent*","interface",["(дочерние узлы)"],2,2)],
        links=[LINK("leaf","component","realization"),
               LINK("composite","component","realization"),
               LINK("composite","child","composition","holds")],
        codeTitle="Коробки и предметы внутри",
        code=COMPOSITE_CODE),
    "decorator": dict(
        name="Decorator",
        intent="Динамически добавляет объекту новое поведение, оборачивая его в объект-декоратор с тем же интерфейсом.",
        problem="Наследование для каждого варианта поведения порождает огромное число подклассов, которые невозможно комбинировать произвольно.",
        solution="Класс-декоратор содержит ссылку на оборачиваемый объект, реализует тот же интерфейс и дополняет/переопределяет его поведение. Декораторы можно наслаивать в любом порядке.",
        applicability=["Поведение нужно добавлять/снимать на лету",
                      "Прямое наследование неудобно или недоступно (sealed-классы)",
                      "Требуется свободная комбинация «прикрас»"],
        pros=["Гибче наследования: варианты комбинируются композицией",
              "Новое поведение без изменения существующих классов",
              "Соответствует Open/Closed"],
        cons=["Много мелких объектов-обёрток",
              "Отладка цепочек декораторов требует внимания"],
        title="Декоратор: цепочка INotification",
        nodes=[("component","INotification","interface",["Content"],1,0),
               ("concrete","SimpleNotification","class",["Content"],0,1),
               ("decorator","NotificationDecorator","abstract",["-_wrappee: INotification","+Content"],2,1),
               ("d1","CriticalDecorator","class",["Content"],1,2),
               ("d2","SignedDecorator","class",["Content"],3,2)],
        links=[LINK("concrete","component","realization"),
               LINK("decorator","component","realization"),
               LINK("decorator","concrete","association","wraps"),
               LINK("d1","decorator","inheritance"),
               LINK("d2","decorator","inheritance")],
        codeTitle="Приукрашивание уведомлений",
        code=DECORATOR_CODE),
    "facade": dict(
        name="Фасад",
        intent="Предоставляет упрощённый интерфейс к сложной подсистеме из множества классов, скрывая детали её работы.",
        problem="Клиент вынужден напрямую взаимодействовать со множеством взаимосвязанных классов подсистемы, зная о их последовательности и контрактах.",
        solution="Один класс-фасад инкапсулирует типичные сценарии обращения к подсистеме и предоставляет клиенту несколько простых методов.",
        applicability=["Нужен простой вход в сложную библиотеку/подсистему",
                      "Хотите разделить слои приложения (UI ← фасад → доменные сервисы)",
                      "Строите API над набором разрозненных модулей"],
        pros=["Клиент отвязан от деталей подсистемы",
              "Читаемость и удобство использования",
              "Точка централизации типовых сценариев"],
        cons=["Фасад может разрастись до «бога-объекта»",
              "Не даёт прямого доступа к мощным возможностям подсистемы"],
        title="Фасад: конвертация видео",
        nodes=[("facade","VideoConverterFacade","class",["Convert(file, format)"],1,0),
               ("s1","CodecFactory","class",["Extract()"],0,1),
               ("s2","AudioExtractor","class",["EncodeToMp3()"],1,1),
               ("s3","FlippingWriter","class",["Save()"],2,1)],
        links=[LINK("facade","s1","dependency"),
               LINK("facade","s2","dependency"),
               LINK("facade","s3","dependency")],
        codeTitle="Единая точка входа в конвейер конвертации",
        code=FACADE_CODE),
    "proxy": dict(
        name="Заместитель",
        intent="Provide surrogate placeholder for another object to control access to it, add laziness, caching or protection.",
        problem="Прямой доступ к объекту нежелателен: он слишком дорогой, находится удалённо или требует проверки прав.",
        solution="Заместитель реализует тот же интерфейс, что и реальный объект, перехватывает вызовы и решает — пробросить их дальше, вернуть кэш или отказать.",
        applicability=["Ленивая инициализация тяжёлых ресурсов",
                      "Кэширование результатов частых запросов",
                      "Контроль доступа / авторизация, удалённая проксия (RPC)"],
        pros=["Клиент работает с тем же интерфейсом",
              "Контроль и оптимизация доступа без изменения реального объекта",
              "Логика доступа изолирована"],
        cons=["Лишний уровень опосредования",
              "Усложнение кода, особенно для кэша и инвалидации"],
        title="Заместитель: кэш видеозапросов",
        nodes=[("subject","IYouTubeClient","interface",["GetVideo(id)"],1,0),
               ("real","YouTubeApi","class",["GetVideo(id)"],0,1),
               ("proxy","CachedYouTubeProxy","class",["-cache: Dictionary","+GetVideo(id)"],2,1)],
        links=[LINK("real","subject","realization"),
               LINK("proxy","subject","realization"),
               LINK("proxy","real","dependency","forwards")],
        codeTitle="Кэширующий прокси к внешнему API",
        code=PROXY_CODE),
}

EN = {
    "bridge": dict(
        name="Bridge",
        intent="Decouples an abstraction from its implementation so the two can vary independently.",
        problem="A tight coupling between an abstraction and its implementations explodes the number of subclasses whenever a new platform or variant is added.",
        solution="The abstraction keeps a reference to an implementation object (interface) and delegates work to it. Both sides evolve independently behind a common contract.",
        applicability=["Detach abstraction from implementation (cross-platform)",
                      "Swap implementations at runtime",
                      "Two orthogonal axes of variation (shape × drawing API)"],
        pros=["Implementations and abstractions extend independently",
              "Implementation details hidden from clients",
              "Behavior substitution without rebuilding the hierarchy"],
        cons=["Extra complexity due to two hierarchies",
              "Clients must be aware of both hierarchies"],
        title="Bridge: Shape ↔ DrawAPI",
        codeTitle="Shapes over different graphics APIs"),
    "composite": dict(
        name="Composite",
        intent="Compose objects into tree structures to represent part-whole hierarchies, letting clients treat individual objects and compositions uniformly.",
        problem="Clients need to process both single objects and groups of objects, duplicating logic for each kind while tree traversal gets messy.",
        solution="A common component interface is implemented by leaves and containers alike. A container holds child components and recursively delegates operations to them.",
        applicability=["Domain naturally forms a tree (folders, UI, menus)",
                      "Uniform treatment of single objects and compositions",
                      "Single place for recursive traversal"],
        pros=["One interface for leaves and containers",
              "New component types are easy to add",
              "Recursive operations written once"],
        cons=["Hard to restrict the composition's component types",
              "Over-generalized interface for specialized operations"],
        title="Composite: IComponent",
        codeTitle="Boxes and items inside"),
    "decorator": dict(
        name="Decorator",
        intent="Dynamically adds new behavior to an object by wrapping it in a decorator object with the same interface.",
        problem="Subclassing for every behavioral variant produces a huge class count that cannot be combined freely.",
        solution="A decorator class holds a reference to the wrapped object, implements the same interface and augments/overrides its behavior. Decorators stack in any order.",
        applicability=["Add/remove behavior on the fly",
                      "Direct inheritance is impractical (sealed classes)",
                      "Free combination of toppings"],
        pros=["More flexible than inheritance via composition",
              "New behavior without touching existing classes",
              "Open/Closed compliant"],
        cons=["Many tiny wrapper objects",
              "Debugging decorator chains takes care"],
        title="Decorator: INotification chain",
        codeTitle="Sprucing up notifications"),
    "facade": dict(
        name="Facade",
        intent="Provides a simplified interface to a complex subsystem of many classes, hiding its inner workings.",
        problem="Clients must interact directly with many interdependent subsystem classes, knowing their sequence and contracts.",
        solution="A single facade class encapsulates typical scenarios against the subsystem and exposes a few simple methods.",
        applicability=["Simple entry point into a complex library/subsystem",
                      "Layer separation (UI ← facade → domain services)",
                      "Building an API over scattered modules"],
        pros=["Clients decoupled from subsystem details",
              "Readability and usability",
              "Centralized typical workflows"],
        cons=["Facade may grow into a god object",
              "No direct access to advanced subsystem features"],
        title="Facade: video conversion",
        codeTitle="Single entry point to the conversion pipeline"),
    "proxy": dict(
        name="Proxy",
        intent="Supplies a surrogate or placeholder for another object to control access to it, adding laziness, caching or protection.",
        problem="Direct access to an object is undesirable: it is too expensive, remote, or requires permission checks.",
        solution="The proxy implements the same interface as the real subject, intercepts calls and decides whether to forward them, serve from cache, or deny.",
        applicability=["Lazy initialization of heavy resources",
                      "Caching frequent requests",
                      "Access control / authorization, remote proxies (RPC)"],
        pros=["Clients keep using the same interface",
              "Access control and optimization without changing the real object",
              "Access logic isolated"],
        cons=["Extra indirection layer",
              "Cache/invalidation complexity"],
        title="Proxy: cached video requests",
        codeTitle="Caching proxy to an external API"),
}

def build_diagram(spec):
    return {"title": spec["title"], "nodes": NODES(*spec["nodes"]), "links": spec["links"]}

def main():
    ru_path, en_path = DATA / "patterns-ru.json", DATA / "patterns-en.json"
    ru_doc = json.loads(ru_path.read_text(encoding="utf-8"))
    en_doc = json.loads(en_path.read_text(encoding="utf-8"))

    def cat(doc, cid):
        return next(c for c in doc["categories"] if c["id"] == cid)

    ru_struct, en_struct = cat(ru_doc, "structural"), cat(en_doc, "structural")
    existing = {p["id"] for p in ru_struct["patterns"]} | {p["id"] for p in en_struct["patterns"]}

    for pid, ru_spec in RU.items():
        if pid in existing:
            print(f"skip {pid} (already present)")
            continue
        en_spec = EN[pid]
        ru_pattern = {
            "id": pid, "name": ru_spec["name"], "intent": ru_spec["intent"],
            "problem": ru_spec["problem"], "solution": ru_spec["solution"],
            "applicability": ru_spec["applicability"], "pros": ru_spec["pros"],
            "cons": ru_spec["cons"], "diagram": build_diagram(ru_spec),
            "code": {"title": ru_spec["codeTitle"], "language": "csharp",
                     "source": ru_spec["code"]},
        }
        en_pattern = {
            "id": pid, "name": en_spec["name"], "intent": en_spec["intent"],
            "problem": en_spec["problem"], "solution": en_spec["solution"],
            "applicability": en_spec["applicability"], "pros": en_spec["pros"],
            "cons": en_spec["cons"], "diagram": build_diagram(ru_spec),
            "code": {"title": en_spec["codeTitle"], "language": "csharp",
                     "source": ru_spec["code"]},  # код идентичен RU/EN
        }
        # Диаграмма одинакова в RU и EN (структурная часть контента).
        ru_struct["patterns"].append(ru_pattern)
        en_struct["patterns"].append(en_pattern)
        print(f"added {pid}")

    ru_path.write_text(json.dumps(ru_doc, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    en_path.write_text(json.dumps(en_doc, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print("OK:", len(ru_struct["patterns"]), "structural patterns")

if __name__ == "__main__":
    sys.exit(main())
