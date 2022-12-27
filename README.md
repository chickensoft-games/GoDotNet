# GoDotNet

[![Chickensoft][chickensoft-badge]][chickensoft-website] [![Discord][discord-badge]][discord] ![line coverage][line-coverage] ![branch coverage][branch-coverage]

State machines, notifiers, and other utilities for C# Godot development.

> 🚨 Looking for node-based dependency injection with providers and dependents? That functionality has been moved to it's own package, [GoDotDep][go_dot_dep]!

While developing our own game, we couldn't find any simple C# solutions for simple state machines, notifiers, and mechanisms for avoiding unnecessary marshalling with Godot. So, we built our own systems — hopefully you can benefit from them, too!

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

## Autoloads

An autoload can be fetched easily from any node. Once an autoload is found on the root child, GoDotNet caches it's type, allowing it to be looked up instantaneously without calling into Godot. 

```csharp
public class MyEntity : Node {
  private MyAutoloadType _myAutoload => this.Autoload<MyAutoloadType>();

  public override void _Ready() {
    _myAutoload.DoSomething();
    var otherAutoload = this.Autoload<OtherAutoload>();
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

GoDotNet provides a simple state machine implementation that emits a C# event when the state changes (since [Godot signals are more fragile](#signals-and-events)). If you try to update the machine to a state that isn't a valid transition from the current state, it throws an exception. The machine requires an initial state to avoid nullability issues during construction.

State machines are not extensible — instead, GoDotNet almost always prefers the pattern of [composition over inheritance][composition-inheritance]. The state machine relies on state equality to determine if the state has changed to avoid issuing unnecessary events. Using `record` or other value types for the state makes equality checking work automatically for free. 

States used with a state machine must implement `IMachineState<T>`, where T is just the type of the machine state. Your machine states can optionally implement the method `CanTransitionTo(IMachineState state)`, which should return true if the given "next state" is a valid transition. Otherwise, the default implementation returns `true` to allow transitions to any state.

To create states for use with a machine, create an interface which implements `IMachineState<IYourInterface>`. Then, create record types for each state which implement your interface, optionally overriding `CanTransitionTo` for any states which only allow transitions to specific states.

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

Simply omit implementing `CanTransitionTo` for any states which should allow transitions to any other state.

```csharp
public interface GameSuspended : IGameState { }
```

Machines are fairly simple to use: create one with an initial state (and optionally register a machine state change event handler). A state machine will announce the state has changed as soon as it is constructed.

```csharp
public class GameManager : Node {
  private readonly Machine<IGameState> _machine;

  public override void _Ready() {
    _machine = new Machine<IGameState>(new GameMainMenuState(), OnChanged);
  }

  /// <summary>Starts the game.</summary>
  public void Start(string name) {
    _machine.Update(new GameLoadingState());
    // do your loading...
    // ...
    // start the game!
    _machine.Update(new GamePlayingState(name);
    // Read the current state at any time:
    var state = _machine.State;
    if (state is GamePlayingState) { /* ... */ }
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

### Read-only State Machines

If you want another object to only be able to read the current state of a state machine and subscribe to changes, but not be able to update the state of the machine, you can expose the machine as an `IReadOnlyMachine<TState>` instead of as a `Machine<TState>`.

```csharp
public class AnObjectThatOnlyListensToAMachine {
  public IReadOnlyMachine<string> Machine { get; set; }

  // This object can listen and respond to state changes in Machine, but
  // cannot mutate the state of the machine via `Machine.Update` :)  
}
```

## Notifiers

A notifier is an object which emits a signal when its value changes. Notifiers are similar to state machines, but they don't care about transitions. Any update that changes the value (determined by comparing the new value with the previous value using `Object.Equals`) will emit a signal. Like state machines, the value of a notifier can never be `null` — make sure you initialize with a valid value!

Using "value" types (primitive types, records, and structs) with a notifier is a natural fit since notifiers check equality to determine if the value has changed. Like state machines, notifiers also invoke an event to announce their value as soon as they are constructed.

```csharp
var notifier = new Notifier<string>("Player", OnPlayerNameChanged);
notifier.Update("Godot");

// Elsewhere...

private void OnPlayerNameChanged(string name, string previous) {
  _log.Print($"Player name changed from ${previous} to ${name}");
}
```

> To easily inject dependencies to descendent nodes, check out [go_dot_dep].

### Read-only Notifiers

As with state machines, a notifiers can be referenced as an `IReadOnlyNotifier<TValue>` to prevent objects using them from causing unwanted changes. The object(s) that own the notifier can reference it as a `Notifier<TValue>` and mutate it accordingly, while the listener objects can simply reference it as an `IReadOnlyNotifier<TValue>`.

```csharp
public class AnObjectThatOnlyListensToANotifier {
  public IReadOnlyNotifier<string> Notifier { get; set; }

  // This object can listen and respond to changes in the Notifier's value, but
  // cannot mutate the value of the notifier via `Notifier.Update` :)  
}
```

## Signals and Events

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

## Random Number Generator

GoDotNet includes a testable convenience wrapper for `System.Random`.

```csharp
public partial class MyNode : Node {
  public IRng Random { get; set; } = new Rng();

  public override void _Ready() {
    int i = Rng.NextInt(); // 0 (inclusive) to int.MaxValue (exclusive)
    float f = Rng.NextFloat(); // 0 (inclusive) to 1 (exclusive)
    double d = Rng.NextDouble(); // 0 (inclusive) to 1 (exclusive)

    // 0 (inclusive) to 25 (exclusive)
    int ir = Rng.RangeInt(0, 25);

    // 0.0f (inclusive) to 25.0f (exclusive)
    float fr = Rng.RangeFloat(0f, 25f); 

    // 0.0d (inclusive) to 25.0d (exclusive)
    double dr = Rng.RangeDouble(0d, 25d); 
  }
}
```

<!-- References -->

<!-- <Badges> -->
[chickensoft-badge]: https://chickensoft.games/images/chickensoft/chickensoft_badge.svg
[chickensoft-website]: https://chickensoft.games
[discord]: https://discord.gg/gSjaPgMmYW
[discord-badge]: https://img.shields.io/badge/Chickensoft%20Discord-%237289DA.svg?style=flat&logo=discord&logoColor=white
[line-coverage]: https://raw.githubusercontent.com/chickensoft-games/go_dot_net/main/test/reports/line_coverage.svg
[branch-coverage]: https://raw.githubusercontent.com/chickensoft-games/go_dot_net/main/test/reports/branch_coverage.svg
<!-- </Badges> -->

[go_dot_dep]: https://github.com/chickensoft-games/go_dot_dep
[net-5-0]: https://github.com/godotengine/godot/issues/43458#issuecomment-725550284
[go_dot_net_nuget]: https://www.nuget.org/packages/Chickensoft.GoDotNet/
[GoDotLog]: https://github.com/chickensoft-games/go_dot_log
[godot-dictionary-iterable-issue]: https://github.com/godotengine/godot/issues/56733
[call-deferred]: https://docs.godotengine.org/en/stable/classes/class_object.html#class-object-method-call-deferred
[signals]: https://docs.godotengine.org/en/stable/tutorials/scripting/c_sharp/c_sharp_features.html#c-signals
[composition-inheritance]: https://en.wikipedia.org/wiki/Composition_over_inheritance
[export-default-values]: https://github.com/godotengine/godot/issues/37703#issuecomment-877406433
