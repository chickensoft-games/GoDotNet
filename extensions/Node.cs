
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
      var autoload = TryAutoload<T>(node);
      if (autoload != null) { return autoload; }
      throw new Exception("No singleton found for type " + typeof(T).Name);
    }

    /// <summary>
    /// Tries to find an autoload of the specified type. If none is found,
    /// returns null. Used by the dependency injection system.
    /// </summary>
    /// <param name="node">The node (receiver) used to get the scene root.
    /// </param>
    /// <typeparam name="T">The type of autoload to find.</typeparam>
    /// <returns>The autoload, if found.</returns>
    public static T? TryAutoload<T>(this Node node) {
      var type = typeof(T);
      if (AutoloadCache.Has(type)) { return (T)AutoloadCache.Read(type); }
      var root = node.GetNode("/root/");
      var autoloads = root.GetChildren();
      foreach (var autoload in autoloads) {
        if (autoload is T singleton) {
          AutoloadCache.Write(type, singleton);
          return singleton;
        }
      }
      return default;
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

    /// <summary>
    /// Creates a dependency on a value that originates from a provider node
    /// which is an ancestor of this node. This stops at the first ancestor
    /// node which implements the correct provider. If none are found, this
    /// examines the autoloaded singletons and returns the value from the first
    /// one it finds which implements the correct provider.
    ///
    /// For nodes to use this, they must implement <see cref="IDependent"/>.
    ///
    /// This will throw an exception if no provider is found in any of the
    /// node's ancestors or autoloaded singletons.
    ///
    /// The underlying dependency system caches the first provider it finds in
    /// the tree, so do not use this system if the node is expected to change
    /// subtrees and you are relying on dynamically resolving the provider each
    /// time. Instead, consider passing the value down manually.
    /// </summary>
    /// <param name="node">The node (receiver) which uses the dependency.
    /// </param>
    /// <typeparam name="T">The type of value the node depends on.</typeparam>
    /// <returns>The value which is depended upon.</returns>
    public static T DependOn<T>(this Node node) {
      if (node is IDependent dependent) {
        if (dependent.Deps.ContainsKey(typeof(T))) {
          if (dependent.Deps[typeof(T)] is Dependency<T> dependency) {
            var value = dependency.Get(node);
            return (T)value!;
          }
          else {
            throw new Exception("Unexpected dependency type.");
          }
        }
        else {
          var dependency = new Dependency<T>();
          dependent.Deps.Add(typeof(T), dependency);
          return (T)dependency.Get(node)!;
        }
      }
      throw new Exception(
        "Nodes using dependencies need to implement Dependent."
      );
    }
  }

}
