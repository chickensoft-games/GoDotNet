namespace GoDotNetTests;
using Godot;
using GoDotNet;
using GoDotTest;
using Shouldly;

public class AutoloadCacheTest : TestClass {
  private class TestClass { }

  public AutoloadCacheTest(Node testScene) : base(testScene) { }

  [Test]
  public void SavesAndLoadsAutoloadFromCache() {
    var autoload = new TestClass();
    AutoloadCache.Write(typeof(TestClass), autoload);
    AutoloadCache.Has(typeof(TestClass)).ShouldBeTrue();
    AutoloadCache.Read(typeof(TestClass)).ShouldBeSameAs(autoload);
  }
}
