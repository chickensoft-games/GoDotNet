using Godot;
using GoDotNet;
using GoDotTest;
using Shouldly;

public class StringExtensionsTest : TestClass {
  public StringExtensionsTest(Node testScene) : base(testScene) { }

  [Test]
  public void ToNameCaseWorksOnNull() {
    string str = null!;
    str.ToNameCase().ShouldBe("");
  }

  [Test]
  public void ToNameCaseWorksOnBlank() {
    var str = "";
    str.ToNameCase().ShouldBe("");
  }

  [Test]
  public void ToNameCaseWorksOnValue() {
    "hello".ToNameCase().ShouldBe("Hello");
    " ".ToNameCase().ShouldBe(" ");
    "^".ToNameCase().ShouldBe("^");
    "1".ToNameCase().ShouldBe("1");
    "a".ToNameCase().ShouldBe("A");
    "A B C".ToNameCase().ShouldBe("A B C");
  }
}
