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
    /// <returns>The value depended upon.</returns>
    public object? Get(Node node) {
      if (_provider is IProvider<TValue> foundProvider) {
        return foundProvider.Get();
      }
      else if (_provider is Dependency<TValue> foundDependency) {
        return foundDependency.Get(node);
      }
      var provider = FindProvider(node);
      _provider = provider;
      return provider.Get();
    }

    /// <summary>
    /// Returns the cached provider (if there is one) or looks up the provider.
    /// Looking up the provider will result it in caching it for future use.
    /// </summary>
    /// <param name="node">The node who depends on the value.</param>
    /// <returns>The provider, or throws if it can't be found.</returns>
    public IProvider<TValue> ResolveProvider(Node node) {
      if (_provider is IProvider<TValue> foundProvider) {
        _provider = foundProvider;
        return foundProvider;
      }
      var provider = FindProvider(node);
      _provider = provider;
      return provider;
    }

    private IProvider<TValue> FindProvider(Node node) {
      // TODO: Start with node.parent, change do-while to while.
      var parent = node;
      do {
        if (parent == null) { break; }
        if (parent is IProvider<TValue> provider) {
          return provider;
        }
        else if (
          !System.Object.ReferenceEquals(parent, node) &&
          parent is IDependent dependent &&
          dependent.GetDeps().ContainsKey(typeof(TValue))
        ) {
          // Parent is a dependent which depends on the same type of value.
          // Instead of walking all the way back up to the provider, we can
          // just borrow the dependency from the parent.
          var parentDependency = (
            dependent.GetDeps()[typeof(TValue)] as Dependency<TValue>
          )!;
          return parentDependency.FindProvider(node);
        }
        parent = parent.GetParent();
      } while (parent != null);
      // Didn't find a provider above us in the tree, search for the
      // first autoload which might implement IProvider<TValue>.
      var autoloadProvider = node.TryAutoload<IProvider<TValue>>();
      if (autoloadProvider != null) {
        return autoloadProvider;
      }
      throw new Exception($"No provider found for {typeof(TValue)}.");
    }
  }

}
