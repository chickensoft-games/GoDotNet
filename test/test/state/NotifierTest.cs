namespace GoDotNetTests;
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
    void OnChanged(string value, string? previous) {
      previous.ShouldBeNull();
      value.ShouldBe("a");
      called = true;
    }
    var notifier = new Notifier<string>("a", OnChanged);
    called.ShouldBeTrue();
    notifier.Value.ShouldBe("a");
    notifier.Previous.ShouldBeNull();
  }

  [Test]
  public void DoesNothingOnSameValue() {
    var notifier = new Notifier<string>("a");
    var called = false;
    void OnChanged(string value, string? previous) => called = true;
    notifier.OnChanged += OnChanged;
    notifier.Update("a");
    called.ShouldBeFalse();
    notifier.Previous.ShouldBeNull();
  }

  [Test]
  public void UpdatesValue() {
    var notifier = new Notifier<string>("a");
    var calledOnChanged = false;
    var calledOnUpdated = false;
    void OnChanged(string value, string? previous) {
      calledOnChanged = true;
      value.ShouldBe("b");
      previous.ShouldBe("a");
      notifier.Previous.ShouldBe("a");
    }
    void OnUpdated(string value) {
      calledOnUpdated = true;
      value.ShouldBe("b");
      notifier.Previous.ShouldBe("a");
    }
    notifier.OnChanged += OnChanged;
    notifier.OnUpdated += OnUpdated;
    notifier.Update("b");
    calledOnChanged.ShouldBeTrue();
    calledOnUpdated.ShouldBeTrue();
  }

}
