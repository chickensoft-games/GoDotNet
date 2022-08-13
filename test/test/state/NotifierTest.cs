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
  }

  [Test]
  public void DoesNothingOnSameValue() {
    var notifier = new Notifier<string>("a");
    var called = false;
    void OnChanged(string value, string? previous) => called = true;
    notifier.OnChanged += OnChanged;
    notifier.Update("a");
    called.ShouldBeFalse();
  }

  [Test]
  public void UpdatesValue() {
    var notifier = new Notifier<string>("a");
    var called = false;
    void OnChanged(string value, string? previous) {
      called = true;
      value.ShouldBe("b");
      previous.ShouldBe("a");
    }
    notifier.OnChanged += OnChanged;
    notifier.Update("b");
    called.ShouldBeTrue();
  }

}
