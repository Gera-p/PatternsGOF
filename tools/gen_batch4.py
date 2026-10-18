# -*- coding: utf-8 -*-
"""Генератор батча 4: поведенческие паттерны (первая волна).
Идемпотентен: удаляет паттерн с тем же id и вставляет в конец категории behavioral."""
import json, io, sys

RU = 'src/DesignPatterns.Core/Content/data/patterns-ru.json'
EN = 'src/DesignPatterns.Core/Content/data/patterns-en.json'

def node(nid, name, kind, members, col, row):
    return {"id": nid, "name": name, "kind": kind, "members": members, "col": col, "row": row}

def link(frm, to, kind, label=None):
    l = {"from": frm, "to": to, "kind": kind}
    if label: l["label"] = label
    return l

def pattern(pid, ru_name, en_name, ru_intent, en_intent, ru_problem, en_problem,
            ru_solution, en_solution, ru_app, en_app, ru_pros, en_pros, ru_cons, en_cons,
            diagram, ru_code_title, en_code_title, source, demo_entry):
    common = {
        "id": pid,
        "diagram": diagram,
        "code": {"language": "csharp", "source": source, "demoEntry": demo_entry},
    }
    ru = dict(common); ru.update({"name": ru_name, "intent": ru_intent, "problem": ru_problem,
        "solution": ru_solution, "applicability": ru_app, "pros": ru_pros, "cons": ru_cons})
    ru["code"] = dict(common["code"]); ru["code"]["title"] = ru_code_title
    en = dict(common); en.update({"name": en_name, "intent": en_intent, "problem": en_problem,
        "solution": en_solution, "applicability": en_app, "pros": en_pros, "cons": en_cons})
    en["code"] = dict(common["code"]); en["code"]["title"] = en_code_title
    return ru, en

def add_to(path, new_pairs):
    d = json.load(io.open(path, encoding='utf-8'))
    beh = next(c for c in d['categories'] if c['id'] == 'behavioral')
    ids = [p[0]['id'] for p in new_pairs]
    lang_key = d['language']
    idx = 1 if lang_key == 'en' else 0
    beh['patterns'] = [p for p in beh['patterns'] if p['id'] not in ids]
    for pair in new_pairs:
        beh['patterns'].append(pair[idx])
    io.open(path, 'w', encoding='utf-8').write(json.dumps(d, ensure_ascii=False, indent=2) + '\n')

PATTERNS = []

# ---------------- CHAIN OF RESPONSIBILITY ----------------
cor_source = r'''using System;

public abstract class SupportHandler
{
    private SupportHandler next;

    public SupportHandler SetNext(SupportHandler nxt) { next = nxt; return nxt; }

    public string Handle(int severity)
    {
        if (CanHandle(severity)) return Resolve(severity);
        if (next != null) return next.Handle(severity);
        return "none";
    }

    protected abstract bool CanHandle(int severity);
    protected abstract string Resolve(int severity);
}

public sealed class FirstLineSupport : SupportHandler
{
    protected override bool CanHandle(int severity) => severity <= 1;
    protected override string Resolve(int s) => "first-line";
}

public sealed class SecondLineSupport : SupportHandler
{
    protected override bool CanHandle(int severity) => severity <= 2;
    protected override string Resolve(int s) => "second-line";
}

public sealed class ManagerSupport : SupportHandler
{
    protected override bool CanHandle(int severity) => severity <= 3;
    protected override string Resolve(int s) => "manager";
}

public static class Demo
{
    public static string Run(int severity)
    {
        var first = new FirstLineSupport();
        var second = new SecondLineSupport();
        var manager = new ManagerSupport();
        first.SetNext(second).SetNext(manager);
        return first.Handle(severity);
    }
}'''

PATTERNS.append(pattern(
    "chain-of-responsibility",
    "Цепочка обязанностей", "Chain of Responsibility",
    "Позволяет передавать запросы по цепочке потенциальных обработчиков, пока один из них не обработает запрос.",
    "Lets you pass requests along a chain of handlers until one of them handles the request.",
    "Запрос должен обработать один из нескольких получателей, причём заранее неизвестно, какой именно. Привязывать отправителя ко всем обработчикам — хрупкое решение.",
    "A request must be handled by one of several receivers, and it is not known in advance which one. Coupling the sender to every handler is fragile.",
    "Превратить обработчиков в цепочку самостоятельных объектов. Каждый обработчик решает: обработать запрос самому или передать его следующему звену цепи.",
    "Turn handlers into a chain of independent objects. Each handler decides whether to process the request itself or pass it to the next link in the chain.",
    ["Несколько объектов могут обработать запрос, порядок важен", "Классы обработчиков должны быть независимы и переиспользуемы", "Набор обработчиков может меняться во время выполнения"],
    ["Several objects may handle a request and the order matters", "Handler classes should be independent and reusable", "The set of handlers may change at runtime"],
    ["Слабая связанность: отправитель не знает конкретного обработчика", "Единый интерфейс обработки; цепочку легко перестроить", "Открыто/закрыто: новые обработчики без изменения существующих"],
    ["Loose coupling: the sender does not know the concrete handler", "Single handling interface; the chain is easy to reorder", "Open/Closed: new handlers without changing existing ones"],
    ["Запрос может остаться необработанным", "Отладка трассировки по цепочке затруднена"],
    ["A request may end up unhandled", "Debugging request tracing through the chain is harder"],
    {"title": "Chain of Responsibility",
     "nodes": [node("handler", "SupportHandler", "abstract", ["next: SupportHandler", "Handle(severity): string"], 1, 0),
               node("first", "FirstLineSupport", "class", [], 0, 1),
               node("second", "SecondLineSupport", "class", [], 1, 1),
               node("manager", "ManagerSupport", "class", [], 2, 1)],
     "links": [link("first", "handler", "inheritance"), link("second", "handler", "inheritance"),
               link("manager", "handler", "inheritance"), link("handler", "handler", "association", "next")]},
    "Тикеты поддержки", "Support tickets", cor_source, "Demo.Run"))

# ---------------- COMMAND ----------------
cmd_source = r'''using System;
using System.Collections.Generic;

public interface ICommand
{
    void Execute();
    void Undo();
}

public sealed class LightBulb
{
    public bool On { get; private set; }
    public void TurnOn() => On = true;
    public void TurnOff() => On = false;
}

public sealed class TurnOnCommand : ICommand
{
    private readonly LightBulb bulb;
    public TurnOnCommand(LightBulb b) { bulb = b; }
    public void Execute() => bulb.TurnOn();
    public void Undo() => bulb.TurnOff();
}

public sealed class TurnOffCommand : ICommand
{
    private readonly LightBulb bulb;
    public TurnOffCommand(LightBulb b) { bulb = b; }
    public void Execute() => bulb.TurnOff();
    public void Undo() => bulb.TurnOn();
}

public sealed class RemoteControl
{
    private readonly Stack<ICommand> history = new Stack<ICommand>();
    public void Press(ICommand command)
    {
        command.Execute();
        history.Push(command);
    }
    public void ReleaseUndo()
    {
        if (history.Count > 0) history.Pop().Undo();
    }
}

public static class Demo
{
    public static string Run(string script)
    {
        var bulb = new LightBulb();
        var remote = new RemoteControl();
        foreach (var step in script.Split(','))
        {
            if (step == "on") remote.Press(new TurnOnCommand(bulb));
            else if (step == "off") remote.Press(new TurnOffCommand(bulb));
            else if (step == "undo") remote.ReleaseUndo();
        }
        return bulb.On ? "on" : "off";
    }
}'''

PATTERNS.append(pattern(
    "command",
    "Команда", "Command",
    "Превращает запросы в объекты, позволяя передавать их как аргументы, ставить запросы в очередь, логировать и отменять операции.",
    "Turns requests into objects so they can be passed as arguments, queued, logged, and undone.",
    "Нужно параметризовать объекты действиями, ставить действия в очередь или поддерживать отмену (Ctrl+Z). Встроить такую логику в классы-отправители невозможно аккуратно.",
    "You need to parameterize objects with actions, queue them, or support undo (Ctrl+Z). Embedding such logic directly in sender classes cannot be done cleanly.",
    "Каждое действие — отдельный объект-команда с методами Execute/Undo. Отправитель вызывает команду, не зная получателя; история команд даёт отмену.",
    "Each action is its own command object with Execute/Undo methods. The invoker calls a command without knowing the receiver; a command history enables undo.",
    ["Действия нужно логировать, ставить в очередь или выполнять отложенно", "Требуется поддержка отмены операций", "Конфигурируемые кнопки/меню/горячие клавиши"],
    ["Actions must be logged, queued, or executed late", "Undo support is required", "Configurable buttons/menus/hotkeys"],
    ["Чёткое разделение отправителя и получателя", "Легко добавлять новые команды и макросы из существующих", "Отмена/повтор реализуются естественно"],
    ["Clean separation between invoker and receiver", "Easy to add commands and compose macros from existing ones", "Undo/redo come naturally"],
    ["Рост числа классов для простых действий", "Дублирование кода при схожих командах"],
    ["Class explosion for trivial actions", "Code duplication among similar commands"],
    {"title": "Command",
     "nodes": [node("cmd", "ICommand", "interface", ["Execute()", "Undo()"], 1, 0),
               node("on", "TurnOnCommand", "class", ["bulb: LightBulb"], 0, 1),
               node("off", "TurnOffCommand", "class", ["bulb: LightBulb"], 2, 1),
               node("invoker", "RemoteControl", "class", ["history: Stack<ICommand>", "Press(cmd)", "ReleaseUndo()"], 1, 2),
               node("bulb", "LightBulb", "class", ["On: bool", "TurnOn()", "TurnOff()"], 0, 2)],
     "links": [link("on", "cmd", "realization"), link("off", "cmd", "realization"),
               link("invoker", "cmd", "association", "executes"), link("on", "bulb", "dependency"),
               link("off", "bulb", "dependency")]},
    "Пульт со светом", "Light remote", cmd_source, "Demo.Run"))

# ---------------- ITERATOR ----------------
it_source = r'''using System.Collections;
using System.Collections.Generic;

public interface IIterator<T>
{
    bool HasNext();
    T Next();
}

public interface IAggregate<T>
{
    IIterator<T> CreateIterator();
}

public sealed class NewsFeed : IAggregate<string>
{
    private readonly List<string> items = new List<string>();
    public void Add(string item) => items.Add(item);

    public IIterator<string> CreateIterator() => new NewsIterator(items);

    private sealed class NewsIterator : IIterator<string>
    {
        private readonly IList<string> data;
        private int index = -1;
        public NewsIterator(IList<string> d) { data = d; }
        public bool HasNext() => index + 1 < data.Count;
        public string Next()
        {
            index++;
            return data[index];
        }
    }
}

public static class Demo
{
    public static string Run(string joined)
    {
        var feed = new NewsFeed();
        foreach (var s in joined.Split('|')) feed.Add(s);
        var it = feed.CreateIterator();
        var result = new List<string>();
        while (it.HasNext()) result.Add(it.Next());
        return string.Join(",", result);
    }
}'''

PATTERNS.append(pattern(
    "iterator",
    "Итератор", "Iterator",
    "Даёт способ последовательно обходить сложную коллекцию, не раскрывая её внутреннее устройство.",
    "Provides a way to traverse a complex collection sequentially without exposing its internal representation.",
    "У коллекции может быть несколько способов обхода (по порядку, только прочитанные, рекурсивно по дереву). Встраивать каждый обход в саму коллекцию — нарушать SRP и плодить дублирование.",
    "A collection may need several traversal modes (in order, unread only, recursive over a tree). Embedding every traversal in the collection violates SRP and duplicates code.",
    "Вынести обход в отдельный объект-итератор с интерфейсом HasNext/Next. Коллекция реализует метод создания итератора и скрывает своё хранилище.",
    "Extract traversal into a separate iterator object with a HasNext/Next interface. The collection exposes a factory method for iterators and hides its storage.",
    ["Сложные структуры данных, обход которых не тривиален", "Нужно несколько вариантов обхода одной коллекции", "Желание скрыть представление коллекции от клиента"],
    ["Complex data structures whose traversal is non-trivial", "Several traversal variants for one collection", "You want to hide the collection representation from clients"],
    ["Единообразный обход разных коллекций", "Инкапсуляция: клиент не видит хранилище", "Несколько итераторов одновременно над одной коллекцией"],
    ["Uniform traversal across different collections", "Encapsulation: clients never see the storage", "Multiple simultaneous iterators over one collection"],
    ["Для простых списков — оверинжиниринг", "Итератору сложно работать с изменяемой коллекцией"],
    ["Overkill for simple lists", "Iterators struggle with concurrently mutated collections"],
    {"title": "Iterator",
     "nodes": [node("iter", "IIterator<T>", "interface", ["HasNext(): bool", "Next(): T"], 0, 0),
               node("agg", "IAggregate<T>", "interface", ["CreateIterator(): IIterator<T>"], 1, 0),
               node("news", "NewsFeed", "class", ["items: List<string>", "CreateIterator()"], 1, 1),
               node("it", "NewsIterator", "class", ["index: int", "HasNext()", "Next()"], 0, 1)],
     "links": [link("it", "iter", "realization"), link("news", "agg", "realization"),
               link("news", "it", "dependency", "creates")]},
    "Лента новостей", "News feed", it_source, "Demo.Run"))

# ---------------- MEDIATOR ----------------
med_source = r'''using System.Collections.Generic;

public abstract class ChatRoom
{
    public static void ShowMessage(User from, User to, string message)
    {
        // «центр» логирует маршрутизацию; пользователи не знают друг друга напрямую
        to.Receive(from.Name + ": " + message);
    }
}

public sealed class User
{
    public User(string name) { Name = name; Received.Clear(); }
    public string Name { get; }
    public List<string> Received { get; } = new List<string>();
    public void Send(User to, string message) => ChatRoom.ShowMessage(this, to, message);
    public void Receive(string message) => Received.Add(message);
}

public static class Demo
{
    public static string Run(string script)
    {
        var ann = new User("Ann");
        var bob = new User("Bob");
        var cat = new User("Cat");
        foreach (var step in script.Split(','))
        {
            var parts = step.Split('>');
            var msg = parts[1];
            if (parts[0] == "A") ann.Send(bob, msg);
            else if (parts[0] == "B") bob.Send(cat, msg);
            else if (parts[0] == "C") cat.Send(ann, msg);
        }
        return ann.Received.Count + "-" + bob.Received.Count + "-" + cat.Received.Count;
    }
}'''

PATTERNS.append(pattern(
    "mediator",
    "Посредник", "Mediator",
    "Ограничивает прямые связи между множеством классов, заставляя их общаться через единый посредник.",
    "Restricts direct communications among classes, forcing them to collaborate through a single mediator.",
    "20 классов общаются друг с другом напрямую — это 400 связей. Любое изменение одного класса затрагивает десятки других, переиспользовать классы по отдельности невозможно.",
    "Twenty classes communicating directly create hundreds of links. Changing one class ripples through dozens of others, and no class can be reused in isolation.",
    "Вынести групповое взаимодействие в отдельный объект-посредник. Классы общаются только с ним, а посредник знает, кто и на что должен реагировать.",
    "Extract group communication into a dedicated mediator object. Classes talk only to the mediator, and the mediator knows who should react to what.",
    ["Много классов хаотично ссылаются друг на друга", "Класс трудно переиспользовать из-за переплетённых связей", "Нужно варьировать поведение взаимодействий без правок классов"],
    ["Many classes refer to each other chaotically", "A class is hard to reuse due to tangled links", "Interaction behavior must vary without editing the classes"],
    ["Распределённая логика собирается в одном месте", "Коллеги развязаны, легче переиспользовать", "Легко добавлять новых участников"],
    ["Distributed logic concentrates in one place", "Colleagues are decoupled and easier to reuse", "New participants are easy to add"],
    ["Посредник может стать «богом»", "Централизация усложняет понимание потока событий"],
    ["The mediator can become a god object", "Centralization makes event flow harder to trace"],
    {"title": "Mediator",
     "nodes": [node("med", "ChatRoom", "class", ["ShowMessage(from,to,msg)"], 1, 0),
               node("a", "User: Ann", "class", ["Send(to,msg)", "Receive(msg)"], 0, 1),
               node("b", "User: Bob", "class", [], 1, 1),
               node("c", "User: Cat", "class", [], 2, 1)],
     "links": [link("a", "med", "dependency", "sends via"), link("b", "med", "dependency"),
               link("c", "med", "dependency")]},
    "Чат-комната", "Chat room", med_source, "Demo.Run"))

# ---------------- OBSERVER ----------------
obs_source = r'''using System;
using System.Collections.Generic;

public interface IObserver
{
    void Update(string eventType, string data);
}

public sealed class EventManager
{
    private readonly Dictionary<string, List<IObserver>> listeners =
        new Dictionary<string, List<IObserver>>();

    public void Subscribe(string type, IObserver observer)
    {
        if (!listeners.TryGetValue(type, out var list))
        {
            list = new List<IObserver>();
            listeners[type] = list;
        }
        list.Add(observer);
    }

    public void Unsubscribe(string type, IObserver observer)
    {
        if (listeners.TryGetValue(type, out var list)) list.Remove(observer);
    }

    public void Notify(string type, string data)
    {
        if (listeners.TryGetValue(type, out var list))
            foreach (var o in list.ToArray()) o.Update(type, data);
    }
}

public sealed class EmailAlert : IObserver
{
    public List<string> Inbox { get; } = new List<string>();
    public void Update(string eventType, string data) => Inbox.Add("email:" + data);
}

public sealed class SmsAlert : IObserver
{
    public List<string> Phone { get; } = new List<string>();
    public void Update(string eventType, string data) => Phone.Add("sms:" + data);
}

public static class Demo
{
    public static string Run(bool unsubscribeBeforeNotify)
    {
        var manager = new EventManager();
        var email = new EmailAlert();
        var sms = new SmsAlert();
        manager.Subscribe("user-registration", email);
        manager.Subscribe("user-registration", sms);
        if (unsubscribeBeforeNotify) manager.Unsubscribe("user-registration", sms);
        manager.Notify("user-registration", "Anna");
        return email.Inbox.Count + "-" + sms.Phone.Count;
    }
}'''

PATTERNS.append(pattern(
    "observer",
    "Наблюдатель", "Observer",
    "Описывает механизм подписки, который позволяет одним объектам следить за событиями в других и автоматически реагировать.",
    "Defines a subscription mechanism that lets objects watch events in other objects and react automatically.",
    "При изменении состояния одного объекта нужно уведомить множество зависимых, не создавая жёсткой связи между ними (например, обновить UI, отправить уведомления, записать лог).",
    "When one object's state changes, many dependents must be notified without hard-coupling them (refresh UI, send alerts, write logs).",
    "Интерфейс Наблюдателя с методом Update; субъект хранит список подписчиков и рассылает события. Подписка/отписка выполняются в любой момент.",
    "An Observer interface with an Update method; the subject keeps a subscriber list and broadcasts events. Subscribing and unsubscribing happen at any time.",
    ["Один объект должен уведомлять другие без знания их конкретных типов", "Нужно подписываться и отписываться динамически", "Изменение должно каскадно распространяться по графу объектов"],
    ["One object must notify others without knowing their concrete types", "Dynamic subscribe/unsubscribe is required", "Changes must cascade across an object graph"],
    ["Субъект и наблюдатели слабо связаны", "Управление событиями «из коробки», автоматическое обновление"],
    ["Subject and observers stay loosely coupled", "Event management built-in; updates propagate automatically"],
    ["Уведомления могут быть непредсказуемы по порядку", "Риск утечек памяти при незакрытых подписках"],
    ["Notification order can be unpredictable", "Memory leaks when subscriptions are never removed"],
    {"title": "Observer",
     "nodes": [node("subj", "EventManager", "class", ["listeners", "Subscribe()", "Notify()"], 1, 0),
               node("obs", "IObserver", "interface", ["Update(type,data)"], 0, 0),
               node("email", "EmailAlert", "class", [], 0, 1),
               node("sms", "SmsAlert", "class", [], 1, 1)],
     "links": [link("email", "obs", "realization"), link("sms", "obs", "realization"),
               link("subj", "obs", "association", "notifies"), link("subj", "subj", "dependency", "stores list")]},
    "Уведомления о регистрации", "Registration alerts", obs_source, "Demo.Run"))

for path in (RU, EN):
    add_to(path, PATTERNS)
print("added", len(PATTERNS), "behavioral patterns to both files")
