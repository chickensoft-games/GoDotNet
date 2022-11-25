namespace GoDotNetTests;
using System;
using System.Diagnostics;
using Godot;
using GoDotLog;
using GoDotNet;
using GoDotTest;
using LightMock;
using LightMock.Generator;
using LightMoq;
using Shouldly;

public class SchedulerTest : TestClass {
  public SchedulerTest(Node testScene) : base(testScene) { }

  [Test]
  public void InitializesFromDefaultConstructor() {
    var scheduler = new Scheduler();
    scheduler.ShouldBeOfType(typeof(Scheduler));
  }

  [Test]
  public void InitializesWithCustomConstructor() {
    var log = new Mock<ILog>();
    var scheduler = new Scheduler(log.Object);
    scheduler.Log.ShouldBeSameAs(log.Object);
  }

  [Test]
  public void InitializesWithCustomConstructorAndDebugging() {
    var log = new Mock<ILog>();
    var isDebugging = true;
    var scheduler = new Scheduler(log.Object, isDebugging);
    scheduler.Log.ShouldBeSameAs(log.Object);
    scheduler._Ready();
    scheduler.IsDebugging.ShouldBe(isDebugging);
  }

  [Test]
  public void RunsScheduledCallback() {
    var scheduler = new Scheduler();
    var called = false;
    scheduler.NextFrame(() => called = true);
    scheduler._Process(0);
    called.ShouldBeTrue();
  }

  [Test]
  public void RunsScheduledCallbackAndHandlesErrorInDebug() {
    var log = new Mock<ILog>();
    var scheduler = new Scheduler(log.Object, true);
    log.Setup(l => l.Run(The<Action>.IsAnyValue, The<Action<Exception>>.IsAnyValue))
      .Callback<Action, Action<Exception>>(
        (action, errorHandler) => {
          try {
            action();
          }
          catch (Exception e) {
            errorHandler(e);
          }
        }
      );

    log.Setup(l => l.Print(
      "A callback scheduled in a previous frame threw " +
      "an error with the following stack trace."
    ));
    log.Setup(l => l.Print(The<StackTrace>.IsAnyValue));
    scheduler.NextFrame(() => throw new InvalidOperationException());
    scheduler._Process(0);
    log.VerifyAll();
    scheduler._Process(0);
  }

  [Test]
  public void RunsScheduledCallbackAndHandlesErrorInProduction() {
    var log = new Mock<ILog>();
    var scheduler = new Scheduler(log.Object, false);
    log.Setup(l => l.Run(
      The<Action>.IsAnyValue, The<Action<Exception>>.IsAnyValue
    )).Callback<Action, Action<Exception>>(
      (action, errorHandler) => {
        try {
          action();
        }
        catch (Exception e) {
          errorHandler(e);
        }
      }
    );
    scheduler.NextFrame(() => throw new InvalidOperationException());
    scheduler._Process(0);
    log.VerifyAll();
    scheduler._Process(0);
  }
}
