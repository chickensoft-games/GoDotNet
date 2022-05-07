namespace GoDotNet {
  using System;
  using Godot;

  /// <summary>
  /// Node extensions for nodes which implement <see cref="IDependent"/>.
  /// These extensions provide dependency resolution for IDependent nodes.
  /// </summary>
  public static class DependentNodeX {
    /// <summary>
    /// Creates a dependency on a value that originates from a provider node
    /// which is an ancestor of this node. This stops at the first ancestor
    /// node which implements the correct provider. If none are found, this
    /// examines the autoloaded singletons and returns the value from the first
    /// one it finds which implements the correct provider.
    ///
    /// Nodes must implement <see cref="IDependent"/> to use this.
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
        if (dependent.GetDeps().ContainsKey(typeof(T))) {
          if (dependent.GetDeps()[typeof(T)] is Dependency<T> dependency) {
            var value = dependency.Get(node);
            return (T)value!;
          }
          else {
            throw new Exception("Unexpected dependency type.");
          }
        }
        else {
          var dependency = new Dependency<T>();
          dependent.GetDeps().Add(typeof(T), dependency);
          return (T)dependency.Get(node)!;
        }
      }
      throw new Exception(
        "Nodes using dependencies need to implement Dependent."
      );
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
  }
}
