namespace GoDotNetTests;
using System;
using System.Threading.Tasks;
using Godot;
using GoDotNet;
using GoDotTest;
using Shouldly;

public partial class TestAutoloadNode : Node { }
public class NodeExtensionsTest : TestClass {

  public NodeExtensionsTest(Node testScene) : base(testScene) { }

  [Test]
  public async Task TryAutoloadFindsNothing() {
    // Must wait until the next frame so that we are running inside "_Process"
    // This ensures that the root node children are initialized.
    await TestScene.ToSignal(TestScene.GetTree(), "idle_frame");
    var foundAutoload = TestScene.TryAutoload<TestAutoloadNode>();
    foundAutoload.ShouldBeNull();
  }

  [Test]
  public async Task AutoloadThrowsOnNotFound() {
    await TestScene.ToSignal(TestScene.GetTree(), "idle_frame");
    Should.Throw<InvalidOperationException>(
      () => TestScene.Autoload<TestAutoloadNode>()
    );
  }

  [Test]
  public async Task TryAutoloadFindsAutoloadFromRootAndCache() {
    await TestScene.ToSignal(TestScene.GetTree(), "idle_frame");
    var autoload = new TestAutoloadNode();
    var root = TestScene.GetNode("/root/");
    root.AddChild(autoload);
    var foundAutoload = TestScene.Autoload<TestAutoloadNode>();
    foundAutoload.ShouldNotBeNull();
    foundAutoload.ShouldBeSameAs(autoload);
    var cachedAutoload = TestScene.Autoload<TestAutoloadNode>();
    cachedAutoload.ShouldNotBeNull();
    cachedAutoload.ShouldBeSameAs(autoload);
    root.RemoveChild(autoload); // as of Godot 3.5, this doesn't seem to take
    // place right away. :( That's why this test runs after the two above.
    autoload.QueueFree();
  }

}
