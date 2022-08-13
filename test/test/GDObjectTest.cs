using System.Linq;
using Godot;
using GoDotNet;
using GoDotTest;
using Newtonsoft.Json;
using Shouldly;

public class GDObjectTest : TestClass {
  private class TestObject : GDObject {
    [JsonProperty]
    public int SomeInt { get; init; }

    [JsonProperty]
    public double SomeDouble { get; init; }

    [JsonProperty]
    public string SomeString { get; init; }

    [JsonConstructor]
    public TestObject(int someInt, double someDouble, string someString) {
      SomeInt = someInt;
      SomeDouble = someDouble;
      SomeString = someString;
    }
  }

  public GDObjectTest(Node testScene) : base(testScene) { }

  private readonly TestObject _obj = new(
    someInt: 1,
    someDouble: 2.0,
    someString: "3"
  );

  [Test]
  public void SerializesAndDeserializes() {
    // Ensure that simple GDObjects like the one above can be converted back
    // and forth between a flat dictionary. Nesting complex objects isn't
    // supported by the GoDotNet serialization methods — they are only for
    // simple (flat) data model types whose serializable fields are all string
    // convertible.
    var data = GDObject.ToStringData(_obj);
    var objAgain = GDObject.FromStringData<TestObject>(data);
    objAgain.ShouldBeOfType(typeof(TestObject));
    _obj.SomeInt.ShouldBe(objAgain.SomeInt);
    _obj.SomeDouble.ShouldBe(objAgain.SomeDouble);
    _obj.SomeString.ShouldBe(objAgain.SomeString);
  }

  [Test]
  public void GetsKeys() {
    var keys = GDObject.GetKeys(_obj).ToHashSet();
    keys.ShouldBeEquivalentTo(new[] {
      "SomeInt",
      "SomeDouble",
      "SomeString"
    }.ToHashSet());
  }
}
