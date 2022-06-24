namespace GoDotNet {
  using System;
  using System.Runtime.CompilerServices;

  /// <summary>
  /// Provider state.
  /// </summary>
  public class ProviderState {
    /// <summary>
    /// True if the provider has provided all of its values.
    /// </summary>
    public bool HasProvided { get; set; } = false;

    /// <summary>
    /// Underlying event delegate used to inform dependent nodes that the
    /// provider has initialized all of the values it provides.
    /// </summary>
    public event Action<IProviderNode>? OnProvided;

    /// <summary>
    /// Invoke the OnProvided event with the specified provider node.
    /// </summary>
    /// <param name="provider">Provider node which has finished initializing
    /// the values it provides.</param>
    public void Announce(IProviderNode provider)
      => OnProvided?.Invoke(provider);
  }

  /// <summary>
  /// Base interface for all providers.
  /// </summary>
  public interface IProviderNode { }

  /// <summary>
  /// Base type for all provider nodes.
  /// </summary>
  public static class IProviderNodeX {
    // Use a conditional weak table to store state. Allows us to create
    // stateful mixins, essentially.
    // See https://codecrafter.blogspot.com/2011/03/c-mixins-with-state.html
    private static readonly ConditionalWeakTable<IProviderNode, ProviderState>
      _providerStates = new();

    private static ProviderState GetState(IProviderNode node)
      => _providerStates.GetOrCreateValue(node);

    private static void SetState(
      IProviderNode node, ProviderState state
    ) => _providerStates.AddOrUpdate(node, state);

    /// <summary>
    /// Providers should call this when all of their provided values have been
    /// initialized.
    ///
    /// This will inform child nodes that all of the dependencies provided by
    /// this provider node are now available for use.
    ///
    /// When all of a dependent node's providers have called
    /// <see cref="Provided" />, the dependent node's
    /// <see cref="IDependent.Loaded"/> method will be called.
    /// </summary>
    public static void Provided(this IProviderNode provider) {
      var state = GetState(provider);
      state.HasProvided = true;
      state.Announce(provider);
    }

    /// <summary>
    /// Checks to see if the provider has finished providing dependencies to
    /// nodes lower in the tree.
    /// </summary>
    /// <param name="provider">Receiver provider node.</param>
    /// <returns>True if the receiver has provided values.</returns>
    public static bool HasProvided(this IProviderNode provider)
      => GetState(provider).HasProvided;

    /// <summary>
    /// Subscribes the given action to the receiver's `OnProvided` event.
    /// </summary>
    /// <param name="provider">Receiver provider node.</param>
    /// <param name="onProvided">Action to perform when the receiver has
    /// finished providing values.</param>
    public static void Listen(
      this IProviderNode provider, Action<IProviderNode>? onProvided
    ) => GetState(provider).OnProvided += onProvided;

    /// <summary>
    /// Unsubscribes the given action from the receiver's `OnProvided` event.
    /// </summary>
    /// <param name="provider">Receiver provider node.</param>
    /// <param name="onProvided">Action to perform when the receiver has
    /// finished providing values.</param>
    public static void StopListening(
      this IProviderNode provider, Action<IProviderNode>? onProvided
    ) => GetState(provider).OnProvided -= onProvided;
  }


  /// <summary>
  /// Represents a node which provides a value of a specific type to descendent
  /// nodes. Or, if implemented by an autoloaded singleton, can provide values
  /// anywhere in the scene tree.
  /// </summary>
  /// <typeparam name="T">Type of value provided by this node.</typeparam>
  public interface IProvider<T> : IProviderNode {
    /// <summary>
    /// Provider nodes should implement this method to provide a value for
    /// descendent nodes.
    /// </summary>
    /// <returns>The value to be provided.</returns>
    public T Get();
  }
}
