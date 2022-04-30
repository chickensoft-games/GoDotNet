# GoDotNet

## Simple dependency injection, state management, serialization, and other utilities for C# Godot development.

GoDotNet is a library of code which (hopefully) makes using C# in Godot a little bit less painful.

While developing a game, we couldn't find any C# solutions to solve problems like dependency injection and simple state management. So, we built our own mechanisms (heavily inspired by other frameworks). It's now time to share this with the wider Godot community, since we will probably never finish our game anyway 🤷‍♀️.

If you've used C# with Godot, you have probably realized it is a tricky environment to work in. GoDotNet aims to make all of that a lot easier.

### Requirements

You must specify C# 9, the correct target framework, and null-safety in your `*.csproj` file.

*If you're adding this to an existing project, you may face a considerable migration effort to make everything null safe.*

Use `netstandard2.1` instead of `net5.0` for the target framework (since there are [some issues][net-5-0] with `net5.0`).


```xml
<PropertyGroup>
  <TargetFramework>netstandard2.1</TargetFramework>
  <LangVersion>9.0</LangVersion>
  <Nullable>enable</Nullable>
  <RootNamespace>YourProjectNamespace</RootNamespace>
</PropertyGroup>
```

If you're using Steamworks.NET, you will likely need to add `<PlatformTarget>x64</PlatformTarget>`.

GoDotNet depends on a few other projects, which you can easily include in your game's `*.csproj` file:

```xml
<ItemGroup>
  <!-- Required to get certain things to build. -->
  <PackageReference Include="Microsoft.CSharp" Version="4.7.0" />
  <!-- Used to support serialization for GDObjects -->
  <PackageReference Include="Newtonsoft.Json" Version="13.0.1" />
</ItemGroup>
```

### Logging

GoDotNet provides a logging interface and a default implementation to make outputting to the console convenient in a debugging environment. For production environments, just make a different implementation that outputs to a log file (or does nothing).

```csharp
public class MyEntity {
  // Create a log which outputs messages prefixed with the name of the class.
  private ILog _log = new GDLog(nameof(MyClass));
}
```

Logs can perform a variety of common actions (and output messages if they fail automatically):

```csharp
_log.Print("My message");

_log.Warn("My message");

_log.Error("My message");

// Run a potentially unsafe action. Any errors thrown from the action will
// be output by the log. An optional error handler callback can be provided
// which will be invoked before the exception is rethrown.
_log.Run(
  () => { _log.Print("Potentially unsafe action"); },
  (e) => {
    _log.Error("Better clean up after myself...whatever I did failed.");
  }
);

// Throw an assertion exception with the given message if the assertion fails.
_log.Assert(node.Name == "MyNode", "Must be valid node name.");

// Return the value of a function or a fallback value.
var result = _log.Always<T>(
  () => new Random().Next(0, 10) > 5 ? "valid value" : null,
  "fallback value"
);

// Print a decently formatted stack trace.
_log.Print(new StackTrace());
```

### Extensions

GoDotNet provides a number of extensions on common Godot objects.
#### Godot Collections ↔️ Dotnet Collections

A Godot Dictionary or Array can be converted to a .NET collection easily in `O(n)` time. Certain [bugs][godot-dictionary-iterable-issue] in Godot's collection types necessitate the need for converting back-and-forth occasionally. For performance reasons, try to avoid doing this often on large collections.

```csharp
using Godot.Collections;

var array = new Array().ToDotNet();
var dictionary = new Dictionary<KeyType, ValueType>().ToDotNet();
```

#### Node Utilities

##### Autoloads

An autoload can be fetched from any node:

```csharp
public class MyEntity : Node {
  private MyAutoloadType _myAutoload = this.Autoload<MyAutoloadType>();

  public override void _Ready() {
    _myAutoload.DoSomething();
  }
}
```

##### Dependencies

Nodes can indicate that they require a certain type of value to be provided to them by any ancestor node above them in the scene tree.

GoDotNet's dependency system is [discussed in detail below](#dependency-injection).

To fetch a dependency:

```csharp
// Inside a Node subclass:
private MyDependencyType _myDependency => this.DependOn<MyDependencyType>();
```

### Scheduling

A `Scheduler` node is included which allows callbacks to be run on the next frame, similar to [CallDeferred][call-deferred]. Unlike `CallDeferred`, the scheduler uses vanilla C# to avoid marshalling types to Godot. Since Godot cannot marshal objects that don't extend `Godot.Object`/`Godot.Reference`, this utility is provided to perform the same function for records, custom types, and C# collections which otherwise couldn't be marshaled between C# and Godot.

Create a new autoload which extends the scheduler:

```csharp
using GoDotNet;

public class GameScheduler : Scheduler { }
```

Add it to the `project.godot` file (preferably the first entry):

```ini
[autoload]

GameScheduler="*res://wherever_autoloads_are/GameScheduler.cs"
```

...and simply schedule a callback to run on the next frame:

```csharp
this.Autoload<Scheduler>().NextFrame(
  () => _log.Print("I won't execute until the next frame.")
)
```

### Dependency Injection

GoDotNet provides a simple dependency injection system which allows dependencies to be provided to child nodes, looked-up, cached, and read on demand. By `dependency`, we mean "any value or instance a node instance might need to perform its job." Oftentimes, dependencies are simply instances of custom classes (or other nodes) which perform game logic.

#### Why have a dependency system?

Why are dependencies helpful? Typically, providing values to nodes requires parent nodes to pass values to children nodes via method calls, following the ["call down, signal upwards"][call-down-signal-up] architecture rule. This creates a tight coupling between the parent and the child since the parent has to know which children need which values. If a distant descendant node of the parent also needs the same value, the parent's children have to pass it down until it reaches the correct descendent, too.

All of that requires a *lot* of boilerplate code and can result in unnecessary scripts being added to nodes which otherwise might not even need scripts. Additionally, doing all that work doesn't even guarantee that the descendants will have the most up-to-date value of the thing they need (unless you take care to create such a system).

GoDotNet's dependency system solves this by letting nodes indicate they provide values by implementing an interface, and letting the descendent nodes that need those values look for nodes above them that provide the values they need.

The dependent nodes don't actually care about what their ancestors are — they just search for the closest node above them that can provide the value they need, ensuring loose coupling.

#### Providing and Fetching Dependencies

Providing values to nodes further down the tree is based on the idea of scoping dependencies, inspired by [popular systems][provider] in other frameworks that have already demonstrated their usefulness.

To create a node which provides a value to all of its descendant nodes, implement the `IProvider<T>` interface.

```csharp
public class MySceneNode : IProvider<MyObject> {
  private MyObject _object = new MyObject();

  // IProvider<MyObject> requires us to implement a single method:
  MyObject IProvider<MyObject>.Get() => _object;
}
```

Nodes can provide multiple values just as easily. Below is a slightly more realistic example of a production use-case where the dependencies can't be created until `_Ready()`.

```csharp
public class MySceneNode : IProvider<MyObject>, IProvider<MyOtherObject> {
  // If this object has to be created on _Ready(), we can use `null!` since we
  // know the value will be present from thereon out. This is equivalent to
  // the `late` modifier in Dart or `lateinit` in Kotlin.
  private MyObject _object = null!;

  private MyOtherObject _otherObject = null!;

  MyObject IProvider<MyObject>.Get() => _object;
  MyOtherObject IProvider<MyOtherObject>.Get() => _otherObject;

  public override void _Ready() {
    _object = new MyObject(/* ... */);
    _otherObject = new MyOtherObject(/* ... */);
  }
}
```

Because C# doesn't quite support mixins, and the nature of game engines requires nodes to be subclassed, classes which use dependencies must implement an interface and a single property (which is fairly easy to type).

*The underlying dependency system uses the `Deps` property to store `Dependency<T>` objects which are used to lookup and cache dependencies. You shouldn't ever have to interact with these directly.*

```csharp
public class MyNodeWhichRequiresADependency : Node, IDependent {
  // IDependent requires us to implement this one property.
  // Don't accidentally implement it like this or you'll break caching!
  //  >> bad: public Dependencies Deps => new();
  public Dependencies Deps { get; } = new();

  // As long as there's a node which implements IProvider<MyObject> above us,
  // we will alway be able to access this object. Under the hood, this creates
  // a dependency utility object which is stored in the Deps property above.
  private MyObject _object => this.DependOn<MyObject>();

  public override void _Ready() {
    // If we need to access a dependency here that isn't technically initialized
    // until the parent's _Ready() method (which happens *after* the children
    // are ready), we can use GoDotNet's scheduler to run a function on the
    // next frame, similar to how call_deferred works.
    //
    // The provider example above is a demonstration of this use case.
    this.Autoload<Scheduler>().NextFrame(
      () => {
        // Object will not be null here, since this will run on the next frame
        // after the parent nodes are guaranteed to have initialized.
        _object.DoSomething();
      }
    )
  }
}
```

If you request a dependency from a node and the correct provider is implemented on any of the ancestor nodes, the autoloads will be searched for the correct provider. If one exists, it will be used instead.

#### Caveats

Like all dependency injection systems, there are a few corner cases you should be aware of.

If a node is removed from the tree and inserted somewhere else in the tree, it may not be able to find the correct provider. The underlying dependency will  continue to use the previous provider if it has cached the value previously, so you can clear the dependency cache when a node re-enters the tree to prevent the dependency resolution from entering an invalid state.

```csharp
// Make sure Deps has a setter if you'll be re-parenting the node.
public Dependencies Deps { get; set; } = new();

public override void _EnterTree() {
  // Reset dependency cache to avoid problems.
  Deps = new();
}
```

 By placing provider nodes above all the possible parents of a node which depends on that value, you can ensure that a node will always be able to find the dependency it requests. Clever provider hierarchies will prevent most of these headaches.
 
### State Machines

GoDotNet provides a simple state machine implementation that emits a C# event when the state changes (since [Godot signals are more fragile](#signals-and-events)). If you try to update the machine to a state that isn't a valid transition from the current state, it throws an exception. The machine requires that an initial state be given when the machine is constructed to avoid nullability issues.

State machines are not extensible — instead, GoDotNet almost always prefers the pattern of [composition over inheritance][composition-inheritance]. The state machine relies on state equality to determine if the state has changed to avoid issuing unnecessary events. Using `record` types for the state allows this to happen automatically. 

States used with a state machine must implement IMachineState<T>, where T is just the type of the machine state. Otherwise, the default implementation returns `true` to allow transitions to any state.

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

### Notifiers

A notifier is an object which emits a signal when its value changes. Notifiers are similar to state machines, but they don't care about transitions. Any update that changes the value (determined by comparing the new value with the previous value using `Object.Equals`) will emit a signal.

Because notifiers check equality to determine changes, they are convenient to use with value types (like primitives and structs). Notifiers, like state machines, also emit a signal to announce their value as soon as they are constructed.

```csharp
private var _notifier
  = new Notifier<string>("Player", OnPlayerNameChanged);

private void OnPlayerNameChanged(string name) {
  _log.Print($"Player name changed to $name");
}
```

### Serialization of Godot Objects

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
MyObject aliceAgain = GDObject.FromData(data);
```

Likewise, if you are convinced each key and value in the `GDObject` is string convertible, you can serialize the model to a `Dictionary<string, string>`. This is helpful when passing data around for Steamworks lobbies, for example.

```csharp
var bob = new MyObject("Bob");
Dictionary<string, string> stringData = GDObject.ToStringData(bob);
MyObject bobAgain = GDObject.FromStringData(stringData);
```

### Technical Challenges

#### Signals and Events

Godot supports emitting [signals] from C#. Because Godot signals pass through the Godot engine, any arguments given to the signal must be marshalled through Godot, forcing them to be classes which extend `Godot.Object`/`Godot.Reference` (structs aren't allowed). Likewise, all the fields in the class must also be the same kind of types so they can be marshalled, and so on.

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

[net-5-0]: https://github.com/godotengine/godot/issues/43458#issuecomment-725550284
[godot-dictionary-iterable-issue]: https://github.com/godotengine/godot/issues/56733
[call-deferred]: https://docs.godotengine.org/en/stable/classes/class_object.html#class-object-method-call-deferred
[provider]: https://pub.dev/packages/provider
[call-down-signal-up]: https://kidscancode.org/godot_recipes/basics/node_communication/
[signals]: https://docs.godotengine.org/en/stable/tutorials/scripting/c_sharp/c_sharp_features.html#c-signals
[composition-inheritance]: https://en.wikipedia.org/wiki/Composition_over_inheritance
[export-default-values]: https://github.com/godotengine/godot/issues/37703#issuecomment-877406433
