using Godot;
using GoDotNet;
using GoDotTest;
using Shouldly;

public class TestDummyValueA { }
public class TestDummyValueB { }

public class TestProviderOneNode : Node, IProvider<TestDummyValueA> {
  private TestDummyValueA _value = null!;
  public TestDummyValueA Get() => _value;

  public override void _Ready() {
    // Typical flow of a node that initializes its dependencies in _Ready()...
    _value = new TestDummyValueA();
    this.Provided();
  }
}

public class TestProviderTwoNode : Node, IProvider<TestDummyValueB> {
  private TestDummyValueB _value = null!;
  public TestDummyValueB Get() => _value;

  public override void _Ready() {
    _value = new TestDummyValueB();
    this.Provided();
  }
}

public class TestDependentNode : Node, IDependent {
  [Dependency]
  public TestDummyValueA ValueA => this.DependOn<TestDummyValueA>();

  [Dependency]
  public TestDummyValueB ValueB => this.DependOn<TestDummyValueB>();

  // Ask to load dependencies on ready. This should result in Loaded() being
  // called after the provider's own _Ready() methods are called after us
  // (since parent _Ready invocations happen after children in Godot's tree
  // order).
  public override void _Ready() => this.Depend();

  // Create a side effect we can test.
  public void Loaded() => ProviderTest.LoadedCalled = true;
}

public class ProviderTest : TestClass {
  public static bool LoadedCalled = false;

  public ProviderTest(Node testScene) : base(testScene) { }

  [Test]
  public void DependentIsLoadedWhenProviderProvidesValue() {
    // Test that Loaded() gets called when all dependencies are provided.
    LoadedCalled = false;
    var dependent = new TestDependentNode();
    var providerOne = new TestProviderOneNode();
    var providerTwo = new TestProviderTwoNode();

    providerOne.AddChild(providerTwo);
    providerTwo.AddChild(dependent);

    dependent._Ready();
    LoadedCalled.ShouldBe(false);
    providerOne._Ready();
    LoadedCalled.ShouldBe(false);
    providerTwo._Ready();
    LoadedCalled.ShouldBe(true);
  }
}
