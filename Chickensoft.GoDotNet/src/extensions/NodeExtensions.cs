namespace Chickensoft.GoDotNet;

using System;
using Godot;

/// <summary>
/// A collection of extension methods for Godot nodes.
/// </summary>
public static class NodeExtensions {
  /// <summary>
  /// <para>
  /// Returns an instance of a Godot Autoload singleton. Implemented as an
  /// extension of Godot.Node, for your convenience.
  /// </para>
  /// <para>
  /// This can potentially return the root node of the scene if and only if
  /// you specify the type of the root node, which should be unlikely (an
  /// edge case that can occur since this searches the root node's children).
  /// </para>
  /// <para>
  /// This respects type inheritance. If multiple autoloads extend the same
  /// type, this returns the first autoload that is assignable to the
  /// specified type. For best results, ensure the autoload type hierarchy
  /// does not overlap.
  /// </para>
  /// </summary>
  /// <param name="node">The node (receiver) used to get the scene root.
  /// </param>
  /// <typeparam name="T">The type of autoload to find.</typeparam>
  /// <returns>The autoload instance, or throws.</returns>
  /// <exception cref="InvalidOperationException">Thrown when an autoload of the
  /// given type cannot be found.</exception>
  public static T Autoload<T>(this Node node) where T : Node
    => TryAutoload<T>(node) ?? throw new InvalidOperationException(
      "No singleton found for type " + typeof(T).Name
    );

  /// <summary>
  /// Tries to find an autoload of the specified type. If none is found,
  /// returns null.
  /// </summary>
  /// <param name="node">The node (receiver) used to get the scene root.
  /// </param>
  /// <typeparam name="T">The type of autoload to find.</typeparam>
  /// <returns>The autoload, if found.</returns>
  public static T? TryAutoload<T>(this Node node) {
    var type = typeof(T);
    if (AutoloadCache.Has(type)) { return (T)AutoloadCache.Read(type); }
    var root = node.GetNode("/root/");
    foreach (var autoload in (Godot.Collections.Array<Node>)root.GetChildren()) {
      if (autoload is T singleton) {
        AutoloadCache.Write(type, singleton);
        return singleton;
      }
    }
    return default;
  }
}
