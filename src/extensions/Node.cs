
namespace GoDotNet {
  using System;
  using System.Runtime.ExceptionServices;
  using System.Threading.Tasks;
  using Godot;

  /// <summary>
  /// A collection of extension methods for Godot nodes.
  /// </summary>
  public static class NodeX {
    /// <summary>
    /// Returns an instance of a Godot Autoload singleton. Implemented as an
    /// extension of Godot.Node, for your convenience.
    ///
    /// This can potentially return the root node of the scene if and only if
    /// you specify the type of the root node, which should be unlikely (an
    /// edge case that can occur since this searches the root node's children).
    ///
    /// This respects type inheritance. If multiple autoloads extend the same
    /// type, this returns the first autoload that is assignable to the
    /// specified type. For best results, ensure the autoload type hierarchy
    /// does not overlap.
    /// </summary>
    /// <param name="node">The node (receiver) used to get the scene root.
    /// </param>
    /// <typeparam name="T">The type of autoload to find.</typeparam>
    /// <returns>The autoload instance, or throws.</returns>
    public static T Autoload<T>(this Node node) where T : Node {
      var autoload = DependentNodeX.TryAutoload<T>(node);
      if (autoload != null) { return autoload; }
      throw new Exception("No singleton found for type " + typeof(T).Name);
    }

    /// <summary>
    /// Invokes a task without waiting for it to complete. If an error occurs,
    /// the log will be used to output the error message, if any.
    /// </summary>
    /// <param name="_"></param>
    /// <param name="log">Log for outputting potential errors.</param>
    /// <param name="callback">Asynchronous callback to be invoked.</param>
    /// <typeparam name="T">Task returned by the callback.</typeparam>
    public static void Unawaited<T>(
      this Node _, ILog log, Func<T> callback
    ) where T : Task => callback().ContinueWith(
        task => {
          log.Error("An error ocurred during an unawaited task.");
          var error = task?.Exception?.ToString();
          if (error != null) { log.Error(error); }
          var exception = task?.Exception?.InnerException;
          // This *usually* seems to get the stack trace I'm interested in:
          var stackTrace = exception?.StackTrace?.ToString();
          if (stackTrace != null) {
            log.Error("Stack trace:");
            log.Error(stackTrace);
          }
          ExceptionDispatchInfo.Capture(exception).Throw();
        },
        TaskContinuationOptions.OnlyOnFaulted
      );
  }

}
