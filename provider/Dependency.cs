namespace GoDotNet {
  using System;
  using Godot;

  /// <summary>
  /// Dependency interface.
  /// </summary>
  public interface IDependency {
    /// <summary>
    /// Returns a dependency.
    /// </summary>
    /// <param name="node">Node requesting the dependency.</param>
    /// <returns></returns>
    object? Get(Node node);
  }

  /// <summary>
  /// Represents a node dependency. Nodes can depend on other nodes above them
  /// in the tree (providers) for certain values.
  /// </summary>
  /// <typeparam name="TValue">Dependency value type.</typeparam>
  public class Dependency<TValue> : IDependency {
    private object? _provider;

    /// <summary>
    /// Use the cached provider (or find it in the tree) to get the dependency's
    /// value. Throws an exception if a provider can't be found when first
    /// trying to resolve the provider.
    /// </summary>
    /// <param name="node">The node who depends on the value.</param>
    /// <returns>The value dependend upon.</returns>
    public object? Get(Node node) {
      if (_provider is IProvider<TValue> foundProvider) {
        return foundProvider.Get();
      }
      else if (_provider is Dependency<TValue> foundDependency) {
        return foundDependency.Get(node);
      }
      // Search the node and its ancestors for a provider of the required type.
      var parent = node;
      do {
        if (parent == null) { break; }
        if (parent is IProvider<TValue> provider) {
          _provider = provider;
          return provider.Get();
        }
        else if (
          !System.Object.ReferenceEquals(parent, node) &&
          parent is IDependent dependent &&
          dependent.Deps.ContainsKey(typeof(TValue))
        ) {
          // Parent is a dependent which depends on the same type of value.
          // Instead of walking all the way back up to the provider, we can
          // just borrow the dependency from the parent.
          var parentDependency = (
            dependent.Deps[typeof(TValue)] as Dependency<TValue>
          )!;
          _provider = parentDependency;
          return parentDependency.Get(node);
        }
        parent = parent.GetParent();
      } while (parent != null);
      // Didn't find a provider above us in the tree, search for the
      // first autoload which might implement IProvider<TValue>.
      var autoloadProvider = node.TryAutoload<IProvider<TValue>>();
      if (autoloadProvider != null) {
        _provider = autoloadProvider;
        return autoloadProvider.Get();
      }
      throw new Exception($"No provider found for {typeof(TValue)}.");
    }
  }

}
