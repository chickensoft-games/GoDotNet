namespace GoDotNet {
  using System;
  using System.Diagnostics;
  using Godot;

  /// <summary>
  /// Default log which outputs to Godot using GD.Print, GD.PushWarning,
  /// and GD.Error. Warnings and errors also print the message since
  /// `GD.PushWarning` and `GD.PushError` don't always show up in the output
  /// when debugging.
  /// </summary>
  public class GDLog : GDObject, ILog {
    private string _prefix { get; set; }

    /// <summary>
    /// Parameterless constructor, don't use this. Required for all
    /// Godot.Objects:
    /// https://github.com/godotengine/godot/issues/37703#issuecomment-877406433
    /// </summary>
    public GDLog() => _prefix = "";

    /// <summary>
    /// Creates a new GDLog with the given prefix.
    /// </summary>
    /// <param name="prefix">Log prefix, displayed at the start of each message.
    /// </param>
    public GDLog(string prefix) => _prefix = prefix;

    /// <inheritdoc/>
    public void Print(string message) => GD.Print(_prefix + ": " + message);

    /// <inheritdoc/>
    public void Print(StackTrace stackTrace) {
      foreach (var frame in stackTrace.GetFrames()) {
        var fileName = frame.GetFileName() ?? "**";
        var lineNumber = frame.GetFileLineNumber();
        var method = frame.GetMethod();
        var className = method.DeclaringType?.Name ?? "UnknownClass";
        var methodName = method.Name;
        Print($"{className}.{methodName} ({fileName}:{lineNumber})");
      }
    }

    /// <inheritdoc/>
    public void Warn(string message) {
      GD.Print(_prefix + ": " + message);
      GD.PushWarning(_prefix + ": " + message);
    }

    /// <inheritdoc/>
    public void Error(string message) {
      GD.Print(_prefix + ": " + message);
      GD.PushError(_prefix + ": " + message);
    }

    /// <inheritdoc/>
    public void Assert(bool condition, string message) {
      if (!condition) {
        Error(message);
        throw new AssertionException(message);
      }
    }

    /// <inheritdoc/>
    public T Run<T>(Func<T> call, Action<Exception>? onError = null) {
      try {
        return call();
      }
      catch (Exception e) {
        onError?.Invoke(e);
        OutputError(e);
        throw;
      }
    }

    /// <inheritdoc/>
    public void Run(Action call, Action<Exception>? onError = null) {
      try {
        call();
      }
      catch (Exception e) {
        OutputError(e);
        onError?.Invoke(e);
        throw;
      }
    }

    /// <inheritdoc/>
    public T Always<T>(Func<T> call, T fallback) {
      try {
        return call();
      }
      catch (Exception e) {
        Warn($"An error ocurred. Using fallback value `{fallback}`.");
        Warn(e.ToString());
        return fallback;
      }
    }

    private void OutputError(Exception e) {
      Error("An error ocurred.");
      Error(e.ToString());
    }
  }
}
