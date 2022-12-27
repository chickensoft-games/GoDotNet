namespace GoDotNetTests;

using Godot;
using GoDotNet;
using GoDotTest;
using Shouldly;

public class NotRandomInt : System.Random {
  private readonly int _number;

  public int MinValue { get; private set; }
  public int MaxValue { get; private set; }

  public NotRandomInt(int number) : base(0) => _number = number;

  public override int Next(int minValue, int maxValue) {
    MinValue = minValue;
    MaxValue = maxValue;
    return _number;
  }

  public override int Next() => _number;
}

public class NotRandomDouble : System.Random {
  private readonly double _number;

  public NotRandomDouble(double number) : base(0) => _number = number;

  public override double NextDouble() => _number;
}

public class RngTest : TestClass {
  public RngTest(Node testScene) : base(testScene) { }

  [Test]
  public void DefaultInitializer() {
    var rng = new Rng();
    rng.Seed.ShouldBeOfType<int>();
    rng.Generator.ShouldBeOfType<System.Random>();
  }

  [Test]
  public void SeedInitializer() {
    var seed = 123;
    var rng = new Rng(seed);
    rng.Seed.ShouldBe(seed);
    rng.Generator.ShouldBeOfType<System.Random>();
  }

  [Test]
  public void ManualInitializer() {
    var seed = 123;
    var generator = new System.Random(seed);
    var rng = new Rng(seed, generator);
    rng.Seed.ShouldBe(seed);
    rng.Generator.ShouldBe(generator);
  }

  [Test]
  public void NextInt() {
    var rng = new Rng(0, new NotRandomInt(123));
    rng.NextInt().ShouldBe(123);
  }

  [Test]
  public void NextFloat() {
    var rng = new Rng(0, new NotRandomDouble(0.123d));
    rng.NextFloat().ShouldBe(0.123f);
  }

  [Test]
  public void NextDouble() {
    var rng = new Rng(0, new NotRandomDouble(0.123d));
    rng.NextDouble().ShouldBe(0.123d);
  }

  [Test]
  public void RangeInt() {
    var gen = new NotRandomInt(5);
    var rng = new Rng(0, gen);
    rng.RangeInt(0, 10).ShouldBe(5);
    gen.MinValue.ShouldBe(0);
    gen.MaxValue.ShouldBe(10);
  }

  [Test]
  public void RangeFloat() {
    var gen = new NotRandomDouble(0.5d);
    var rng = new Rng(0, gen);
    rng.RangeFloat(1f, 10f).ShouldBe(5.5f);
  }

  [Test]
  public void RangeDouble() {
    var gen = new NotRandomDouble(0.5d);
    var rng = new Rng(0, gen);
    rng.RangeDouble(1d, 10d).ShouldBe(5.5d);
  }
}
