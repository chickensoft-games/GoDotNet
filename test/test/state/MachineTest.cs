using Godot;
using GoDotNet;
using GoDotTest;
using Shouldly;

public class MachineTest : TestClass {
  private record TestState : IMachineState<TestState> {
    public virtual bool CanTransitionTo(TestState state) => true;
  }
  private record TestStateA : TestState {
    public override bool CanTransitionTo(TestState state)
      => state is TestStateB;
  }
  private record TestStateB : TestState {
    public override bool CanTransitionTo(TestState state)
      => state is TestStateC;
  }
  private record TestStateC : TestState {
    public override bool CanTransitionTo(TestState state)
      => state is TestStateA;
  }

  public MachineTest(Node testScene) : base(testScene) { }

  [Test]
  public void Instantiates() {
    var machine = new Machine<TestState>(new TestStateA());
    machine.State.ShouldBe(new TestStateA());
  }

  [Test]
  public void InstantiatesWithListener() {
    var called = false;
    void OnChanged(TestState state) {
      state.ShouldBe(new TestStateA());
      called = true;
    }
    var machine = new Machine<TestState>(new TestStateA(), OnChanged);
    called.ShouldBeTrue();
    machine.State.ShouldBe(new TestStateA());
  }

  [Test]
  public void UpdatesStateAndAnnounces() {
    var machine = new Machine<TestState>(new TestStateA());

    var called = false;
    void OnChanged(TestState state) {
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
    var machine = new Machine<TestState>(new TestStateA());
    var called = false;
    void OnChanged(TestState state) => called = true;
    machine.OnChanged += OnChanged;
    machine.Update(new TestStateA());
    called.ShouldBeFalse();
  }

  [Test]
  public void ThrowsWhenTransitioningToInvalidNextState() {
    var machine = new Machine<TestState>(new TestStateA());
    machine.State.CanTransitionTo(new TestStateC()).ShouldBeFalse();
    Should.Throw<InvalidStateTransition<TestState>>(
      () => machine.Update(new TestStateC())
    );
  }

  [Test]
  public void UpdatesStateFromAnnouncementPreservesOrdering() {
    var machine = new Machine<TestState>(new TestStateA());

    void OnChanged(TestState state) {
      if (state is TestStateB) {
        machine.Update(new TestStateC());
      }
    }

    machine.OnChanged += OnChanged;
    machine.Update(new TestStateB());
    machine.State.ShouldBe(new TestStateC());
  }
}
