# GoDotNet

[![Chickensoft Badge][chickensoft-badge]][chickensoft-website] [![Discord](https://img.shields.io/badge/Chickensoft%20Discord-%237289DA.svg?style=flat&logo=discord&logoColor=white)][discord] ![line coverage][line-coverage] ![branch coverage][branch-coverage]

State machines, notifiers, serialization, and other utilities for C# Godot development.

> 🚨🔍 Looking for node-based dependency injection with providers and dependents? That functionality has been moved to it's own package, [GoDotDep][go_dot_dep]!

While developing our own game, we couldn't find any simple C# solutions for simple state machines, basic serialization of Godot objects, notifiers, and mechanisms for avoiding unnecessary marshalling with Godot. So, we built our own systems that were heavily inspired by other popular frameworks. Hopefully you can benefit from them, too!

> Are you on discord? If you're building games with Godot and C#, we'd love to see you in the [Chickensoft Discord server][discord]!

## Installation

Find the latest version of [GoDotNet][go_dot_net_nuget] on nuget.

In your `*.csproj`, add the following snippet in your `<ItemGroup>`, save, and run `dotnet restore`. Make sure to replace `*VERSION*` with the latest version.

```xml
<PackageReference Include="Chickensoft.GoDotNet" Version="*VERSION*" />
```

GoDotNet is itself written in C# 10 for `netstandard2.1` (the highest language version currently supported by Godot). If you want to setup your project the same way, look no further than the [`GoDotNet.csproj`](GoDotNet.csproj) file for inspiration!

## Logging

Internally, GoDotNet uses [GoDotLog] for logging. GoDotLog allows you to easily create loggers that output nicely formatted, prefixed messages (in addition to asserts and other exception-aware execution utilities).

## Extensions

GoDotNet provides a number of extensions on common Godot objects.
### Godot Collections ↔️ Dotnet Collections

A Godot Dictionary or Array can be converted to a .NET collection easily in `O(n)` time. Certain [bugs][godot-dictionary-iterable-issue] in Godot's collection types necessitate the need for converting back-and-forth occasionally. For performance reasons, try to avoid doing this often on large collections.

```csharp
using Godot.Collections;

var array = new Array().ToDotNet();
var dictionary = new Dictionary<KeyType, ValueType>().ToDotNet();
```

### Node Utilities

#### Autoloads

An autoload can be fetched from any node:

```csharp
public class MyEntity : Node {
  private MyAutoloadType _myAutoload = this.Autoload<MyAutoloadType>();

  public override void _Ready() {
    _myAutoload.DoSomething();
  }
}
```

## Scheduling

A `Scheduler` node is included which allows callbacks to be run on the next frame, similar to [CallDeferred][call-deferred]. Unlike `CallDeferred`, the scheduler uses vanilla C# to avoid marshalling types to Godot. Since Godot cannot marshal objects that don't extend `Godot.Object`/`Godot.Reference`, this utility is provided to perform the same function for records, custom types, and C# collections which otherwise couldn't be marshaled between C# and Godot.

Create a new autoload which extends the scheduler:

```csharp
using GoDotNet;

public class GameScheduler : Scheduler { }
```

Add it to your `project.godot` file (preferably the first entry):

```ini
[autoload]

GameScheduler="*res://autoload_folder/GameScheduler.cs"
```

...and simply schedule a callback to run on the next frame:

```csharp
this.Autoload<Scheduler>().NextFrame(
  () => _log.Print("I won't execute until the next frame.")
)
```

## State Machines

GoDotNet provides a simple state machine implementation that emits a C# event when the state changes (since [Godot signals are more fragile](#signals-and-events)). If you try to update the machine to a state that isn't a valid transition from the current state, it throws an exception. The machine requires that an initial state be given when the machine is constructed to avoid nullability issues.

State machines are not extensible — instead, GoDotNet almost always prefers the pattern of [composition over inheritance][composition-inheritance]. The state machine relies on state equality to determine if the state has changed to avoid issuing unnecessary events. Using `record` types for the state allows this to happen automatically. 

States used with a state machine must implement `IMachineState<T>`, where T is just the type of the machine state. Otherwise, the default implementation returns `true` to allow transitions to any state.

States can optionally implement `CanTransitionTo(IMachineState state)` to determine if the proposed state transition is valid.

```csharp
public interface IGameState : IMachineState<IGameState> { }

public record GameMainMenuState : IGameState {
  public bool CanTransitionTo(IGameState state) => state is GameLoadingState;
}

public record GameLoadingState : IGameState {
  public bool CanTransitionTo(IGameState state) => state is GamePlayingState;
}

// States can store values!
public record GamePlayingState(string PlayerName) {
  public bool CanTransitionTo(IGameState state) => state is GameMainMenuState;
}
```

Machines are fairly simple to use: create one with an initial state (and optionally register a machine state change event handler). A state machine will announce the state has changed as soon as it is constructed.

```csharp
public class GameManager : Node {
  private readonly Machine<IGameState> _machine;

  // Expose the machine's event.
  public event Machine<IGameState>.Changed OnChanged {
    add => _machine.OnChanged += value;
    remove => _machine.OnChanged -= value;
  }

  public override void _Ready() {
    _machine = new Machine<IGameState>(new GameMainMenuState(), onChanged);
  }

  /// <summary>Starts the game.</summary>
  public void Start(string name) {
    _machine.Update(new GameLoadingState());
    // do your loading...
    // ...
    // start the game!
    _machine.Update(new GamePlayingState(name);
  }

  /// <summary>Goes back to the menu.</summary>
  public void GoToMenu() => _machine.Update(new GameMainMenuState());

  public void OnChanged(IGameState state) {
    if (state is GamePlayingState playingState) {
      var playerName = playingState.Name();
      // ...
    }
  }
}
```

## Notifiers

A notifier is an object which emits a signal when its value changes. Notifiers are similar to state machines, but they don't care about transitions. Any update that changes the value (determined by comparing the new value with the previous value using `Object.Equals`) will emit a signal. It's often convenient to use record types as the value of a Notifier. Like state machines, the value of a notifier can never be `null` — make sure you initialize with a valid value!

Because notifiers check equality to determine changes, they are convenient to use with "value" types (like primitives, records, and structs). Notifiers, like state machines, also emit a signal to announce their value as soon as they are constructed.

```csharp
private var _notifier
  = new Notifier<string>("Player", OnPlayerNameChanged);

private void OnPlayerNameChanged(string name) {
  _log.Print($"Player name changed to $name");
}
```

Like state machines, notifiers should typically be kept private. Instead of letting consumers modify the value directly, you can create a manager class which provides methods that mutate the notifier value. These manager classes can provide an event which redirects to the notifier event, or they can emit their own events when certain pieces of the notifier value changes.

```csharp
public record EnemyData(string Name, int Health);

public class EnemyManager {
  private readonly Notifier<EnemyData> _notifier;

  public event Notifier<EnemyData>.Changed OnChanged {
    add => _notifier.OnChanged += value;
    remove => _notifier.OnChanged -= value;
  }

  public EnemyData Value => _notifier.Value;

  public EnemyManager(string name, int health) => _notifier = new(
    new EnemyData(name, health)
  );

  public void UpdateName(string name) =>
    _notifier.Value = _notifier.Value with { Name = name };

  public void UpdateHealth(int health) =>
    _notifier.Value = _notifier.Value with { Health = health };
}
```

The class above shows an enemy manager which manages a single enemy's state and emits an `OnChanged` event whenever any part of the enemy's state changes. You can easily modify it to emit more specific events when certain pieces of the enemy state changes.

```csharp
public class EnemyManager {
  private readonly Notifier<EnemyData> _notifier;

  public EnemyData Value => _notifier.Value;

  public event Action<string>? OnNameChanged;
  public event Action<int>? OnHealthChanged;

  public EnemyManager(string name, int health) => _notifier = new(
    new EnemyData(name, health),
    OnChanged
  );

  public void UpdateName(string name) =>
    _notifier.Value = _notifier.Value with { Name = name };

  public void UpdateHealth(int health) =>
    _notifier.Value = _notifier.Value with { Health = health };

  private void OnChanged(EnemyData enemy, EnemyData? previous) {
    // Emit different events depending on what changed.
    if (!System.Object.Equals(enemy.Name, previous?.Name)) {
      OnNameChanged?.Invoke(enemy.Name);
    }
    else if (!System.Object.Equals(enemy.Health, previous?.Health)) {
      OnHealthChanged?.Invoke(enemy.Health);
    }
  }
}
```

By providing manager classes which wrap state machines or notifiers to dependent nodes, you can create nodes which easily respond to changes in the values provided by distant ancestor nodes.

## Serialization of Godot Objects

GoDotNet provides a `GDObject` which extends `Godot.Reference` (which is itself a `Godot.Object`). Because it is a `Godot.Object`, `GDObject` can be marshalled back and forth between Godot's C++ layer without any issue, provided each of its fields is also a type that Godot supports.

GoDotNet takes this a step further and offers basic dictionary serialization for GDObjects using the popular `Newtonsoft.Json` package (if you use the appropriate annotations for constructors and fields).

When extending `GDObject` or `Godot.Object`, it is imperative to offer a default constructor with no parameters. Godot uses this default constructor [to create a default value for exported fields][export-default-values] in the editor.

```csharp
public class MyObject : GDObject {

  // Init properties are a great way to keep things immutable on GDObject.
  // Marking this with [JsonProperty] allows it to be serialized using 
  // Newtonsoft.Json.
  [JsonProperty]
  public string PlayerName { get; init; }

  // Default constructor to keep Godot happy.
  public MyObject() {
    PlayerName = default;
  }

  // Tell Newtonsoft.Json to use this constructor for deserialization.
  [JsonConstructor]
  public MyObject(string playerName) {
    PlayerName = playerName;
  }
}
```

To serialize and deserialize a `GDObject` to and from a dictionary, respectively, you can use the static methods on `GDObject.`

```csharp
var alice = new MyObject("Alice");
Dictionary<string, object?> data = GDObject.ToData(alice);
MyObject aliceAgain = GDObject.FromData<MyObject>(data);
```

Likewise, if you are convinced each key and value in the `GDObject` is string convertible, you can serialize the model to a `Dictionary<string, string>`. This is helpful when passing data around for Steamworks lobbies, for example.

```csharp
var bob = new MyObject("Bob");
Dictionary<string, string> stringData = GDObject.ToStringData(bob);
MyObject bobAgain = GDObject.FromStringData(stringData);
```

## Technical Challenges

### Signals and Events

Godot supports emitting [signals] from C#. Because Godot signals pass through the Godot engine, any arguments given to the signal must be marshalled through Godot, forcing them to be classes which extend `Godot.Object`/`Godot.Reference` (records aren't allowed). Likewise, all the fields in the class must also be the same kind of types so they can be marshalled, and so on.

It's not possible to have static typing with signal parameters, so you don't find out until runtime if you accidentally passed the wrong parameter. The closest you can do is the following, which wouldn't break at compile time if the receiving function signature happened to be wrong.

```csharp
public class ObjectType {
  [Signal]
  public delegate void DoSomething(string value); 
}

public class MyNode : Node {
  // Inside your node
  public override void _Ready() {
    _ = object.Connect(
      nameof(ObjectType.DoSomething),
      this,
      nameof(MyDoSomething)
    );
  }

  private void DoSomething(int value) {
    // WARNING: Value should be string, not int!!
  }
}
```

Because of these limitations, GoDotNet will avoid Godot signals except when necessary to interact with Godot components. For communication between C# game logic, it will typically be preferable to use C# events instead.

```csharp
// Declare an event signature — no [Signal] attribute necessary.
public delegate void Changed(Type1 value1, Type2 value2);

// Add an event field to your emitter
public event Changed? OnChanged;

// Trigger an event from your emitter:
OnChanged?.Invoke(argument1, argument2);

// Listen to an event in your receiver:
var emitter = new MyEmitterObject();
emitter.OnChanged += MyOnChangedHandler;

// Event handler in your receiver:
private void MyOnChangedHandler(Type1 value1, Type2 value2) {
  // respond to event we received
}
```

<!-- References -->

[chickensoft-badge]: https://chickensoft.games/images/chickensoft/chickensoft_badge.svg
[chickensoft-website]: https://chickensoft.games
[discord]: https://discord.gg/gSjaPgMmYW
[line-coverage]: https://raw.githubusercontent.com/chickensoft-games/go_dot_net/main/test/reports/line_coverage.svg
[branch-coverage]: https://raw.githubusercontent.com/chickensoft-games/go_dot_net/main/test/reports/branch_coverage.svg
[go_dot_dep]: https://github.com/chickensoft-games/go_dot_dep
[net-5-0]: https://github.com/godotengine/godot/issues/43458#issuecomment-725550284
[go_dot_net_nuget]: https://www.nuget.org/packages/Chickensoft.GoDotNet/
[GoDotLog]: https://github.com/chickensoft-games/go_dot_log
[godot-dictionary-iterable-issue]: https://github.com/godotengine/godot/issues/56733
[call-deferred]: https://docs.godotengine.org/en/stable/classes/class_object.html#class-object-method-call-deferred
[signals]: https://docs.godotengine.org/en/stable/tutorials/scripting/c_sharp/c_sharp_features.html#c-signals
[composition-inheritance]: https://en.wikipedia.org/wiki/Composition_over_inheritance
[export-default-values]: https://github.com/godotengine/godot/issues/37703#issuecomment-877406433
