namespace GoDotNet {
  using System;

  /// <summary>
  /// Base type for all provider nodes.
  /// </summary>
  public interface IProviderNode {
    /// <summary>
    /// Event that should be emitted by the provider when it has initialized
    /// all of the dependencies that it provides.
    /// </summary>
    public event Action<IProviderNode>? OnProvided;
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
