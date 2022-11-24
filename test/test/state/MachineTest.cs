namespace GoDotNetTests;

using Godot;
using GoDotNet;
using GoDotTest;
using Shouldly;

public class MachineTest : TestClass {
  private interface ITestState : IMachineState<ITestState> { }
  private record TestStateA : ITestState {
    public bool CanTransitionTo(ITestState state)
      => state is TestStateB;
  }
  private record TestStateB : ITestState {
    public bool CanTransitionTo(ITestState state)
      => state is TestStateC;
  }
  private record TestStateC : ITestState {
    public bool CanTransitionTo(ITestState state)
      => state is TestStateA;
  }

  private record TestStateD : ITestState { }

  public MachineTest(Node testScene) : base(testScene) { }

  [Test]
  public void Instantiates() {
    var machine = new Machine<ITestState>(new TestStateA());
    machine.State.ShouldBe(new TestStateA());
  }

  [Test]
  public void InstantiatesWithListener() {
    var called = false;
    void OnChanged(ITestState state) {
      state.ShouldBe(new TestStateA());
      called = true;
    }
    var machine = new Machine<ITestState>(new TestStateA(), OnChanged);
    called.ShouldBeTrue();
    machine.State.ShouldBe(new TestStateA());
  }

  [Test]
  public void UpdatesStateAndAnnounces() {
    var machine = new Machine<ITestState>(new TestStateA());

    var called = false;
    void OnChanged(ITestState state) {
      state.ShouldBe(new TestStateB());
      called = true;
    }

    machine.OnChanged += OnChanged;
    machine.Update(new TestStateB());
    machine.State.ShouldBe(new TestStateB());
    called.ShouldBeTrue();
  }

  [Test]
  public void DoesNothingOnSameState() {
    var machine = new Machine<ITestState>(new TestStateA());
    var called = false;
    void OnChanged(ITestState state) => called = true;
    machine.OnChanged += OnChanged;
    machine.Update(new TestStateA());
    called.ShouldBeFalse();
  }

  [Test]
  public void ThrowsWhenTransitioningToInvalidNextState() {
    var machine = new Machine<ITestState>(new TestStateA());
    machine.State.CanTransitionTo(new TestStateC()).ShouldBeFalse();
    Should.Throw<InvalidStateTransitionException<ITestState>>(
      () => machine.Update(new TestStateC())
    );
  }

  [Test]
  public void DefaultStateTransitionsToAnything() {
    var machine = new Machine<ITestState>(new TestStateD());
    machine.State.CanTransitionTo(new TestStateA()).ShouldBeTrue();
    machine.State.CanTransitionTo(new TestStateB()).ShouldBeTrue();
    machine.State.CanTransitionTo(new TestStateC()).ShouldBeTrue();
  }

  [Test]
  public void UpdatesStateFromAnnouncementPreservesOrdering() {
    var machine = new Machine<ITestState>(new TestStateA());

    void OnChanged(ITestState state) {
      if (state is TestStateB) {
        machine.Update(new TestStateC());
      }
    }

    machine.OnChanged += OnChanged;
    machine.Update(new TestStateB());
    machine.State.ShouldBe(new TestStateC());
  }
}
