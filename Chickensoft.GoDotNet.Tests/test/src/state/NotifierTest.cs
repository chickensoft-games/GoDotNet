namespace Chickensoft.GoDotNet.Tests;

using Godot;
using GoDotNet;
using GoDotTest;
using Shouldly;

public class NotifierTest : TestClass {
  public NotifierTest(Node testScene) : base(testScene) { }

  [Test]
  public void Instantiates() {
    var notifier = new Notifier<string>("a");
    notifier.Value.ShouldBe("a");
    notifier.Previous.ShouldBeNull();
  }

  [Test]
  public void InstantiatesWithListener() {
    var called = false;
    void onChanged(string value, string? previous) {
      previous.ShouldBeNull();
      value.ShouldBe("a");
      called = true;
    }
    var notifier = new Notifier<string>("a", onChanged);
    called.ShouldBeTrue();
    notifier.Value.ShouldBe("a");
    notifier.Previous.ShouldBeNull();
  }

  [Test]
  public void DoesNothingOnSameValue() {
    var notifier = new Notifier<string>("a");
    var called = false;
    void onChanged(string value, string? previous) => called = true;
    notifier.OnChanged += onChanged;
    notifier.Update("a");
    called.ShouldBeFalse();
    notifier.Previous.ShouldBeNull();
  }

  [Test]
  public void UpdatesValue() {
    var notifier = new Notifier<string>("a");
    var calledOnChanged = false;
    var calledOnUpdated = false;
    void onChanged(string value, string? previous) {
      calledOnChanged = true;
      value.ShouldBe("b");
      previous.ShouldBe("a");
      notifier.Previous.ShouldBe("a");
    }
    void onUpdated(string value) {
      calledOnUpdated = true;
      value.ShouldBe("b");
      notifier.Previous.ShouldBe("a");
    }
    notifier.OnChanged += onChanged;
    notifier.OnUpdated += onUpdated;
    notifier.Update("b");
    calledOnChanged.ShouldBeTrue();
    calledOnUpdated.ShouldBeTrue();
  }
}
