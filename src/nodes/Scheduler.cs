namespace GoDotNet {
  using System;
  using System.Collections.Generic;
  using System.Diagnostics;
  using Godot;
  using GoDotLog;

  /// <summary>
  /// The scheduler helps queue up callbacks and run them at the desired time.
  /// </summary>
  public class Scheduler : Node {
    private readonly Queue<Action> _actions = new();
    private readonly Dictionary<Action, StackTrace> _stackTraces = new();
    private readonly Dictionary<Action, object> _callers = new();
    private bool _isDebugging = false;

    private readonly ILog _log = new GDLog(nameof(Scheduler));
    /// <inheritdoc/>

    public override void _Ready() => _isDebugging = OS.IsDebugBuild();

    /// <inheritdoc/>
    public override void _Process(float delta) {
      if (_actions.Count == 0) { return; }
      do {
        var action = _actions.Dequeue();
        _log.Run(
          action,
          _isDebugging
            ? HandleScheduledActionError(action)
            : (Exception e) => { }
        );
      } while (_actions.Count > 0);
    }

    private Action<Exception> HandleScheduledActionError(Action action)
      => (Exception e) => {
        var stackTrace = _stackTraces[action];
        _stackTraces.Remove(action);
        _log.Print(
          "The error was scheduled from the following stack " +
          "trace in a previous frame."
        );
        _log.Print(stackTrace);
      };

    /// <summary>
    /// Schedule a callback to run on the next frame.
    /// </summary>
    /// <param name="action">Callback to run.</param>
    public void NextFrame(Action action) {
      _actions.Enqueue(action);
      if (_isDebugging) {
        _stackTraces.Add(action, new StackTrace());
      }
    }
  }
}
