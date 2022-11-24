namespace GoDotNet;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Godot;
using GoDotLog;

/// <summary>
/// The scheduler helps queue up callbacks and run them at the desired time.
/// </summary>
public partial class Scheduler : Node {
  internal readonly ILog Log;
  private readonly Queue<Action> _actions = new();
  private readonly Dictionary<Action, StackTrace> _stackTraces = new();
  private readonly Dictionary<Action, object> _callers = new();
  private readonly bool? _isDebuggingOverride;
  private bool _isDebugBuild;

  /// <summary>True if the scheduler is running in debug mode.</summary>
  public bool IsDebugging => _isDebuggingOverride ?? _isDebugBuild;

  /// <summary>Creates a new scheduler.</summary>
  public Scheduler() => Log = new GDLog(nameof(Scheduler));

  /// <summary>Creates a new scheduler.</summary>
  /// <param name="log">Scheduler log.</param>
  /// <param name="isDebuggingOverride">Debug mode (true or false, or null
  /// to infer).</param>
  public Scheduler(ILog log, bool? isDebuggingOverride = null) {
    Log = log;
    _isDebuggingOverride = isDebuggingOverride;
  }

  /// <inheritdoc/>
  public override void _Ready()
    => _isDebugBuild = OS.IsDebugBuild();

  /// <inheritdoc/>
  public override void _Process(double delta) {
    if (_actions.Count == 0) { return; }
    do {
      var action = _actions.Dequeue();
      Log.Run(
        action,
        IsDebugging
          ? HandleScheduledActionError(action)
          : (e) => { }
      );
    } while (_actions.Count > 0);
  }

  private Action<Exception> HandleScheduledActionError(Action action)
    => (Exception e) => {
      var stackTrace = _stackTraces[action];
      _stackTraces.Remove(action);
      Log.Print(
        "A callback scheduled in a previous frame threw an error with " +
        "the following stack trace."
      );
      Log.Print(stackTrace);
    };

  /// <summary>
  /// Schedule a callback to run on the next frame.
  /// </summary>
  /// <param name="action">Callback to run.</param>
  public void NextFrame(Action action) {
    _actions.Enqueue(action);
    if (IsDebugging) {
      _stackTraces.Add(action, new StackTrace());
    }
  }
}
