namespace Chickensoft.GoDotNet;

using System;
using System.Collections.Generic;

/// <summary>
/// A static class used to cache autoloads whenever they are fetched. This
/// prevents <see cref="NodeExtensions.Autoload{T}(Godot.Node)"/> from having to fetch the
/// root node children on every invocation.
/// </summary>
public class AutoloadCache {
  private static readonly Dictionary<Type, object> _cache = new();

  /// <summary>
  /// Save a value to the cache.
  /// </summary>
  /// <param name="type">Type of the node.</param>
  /// <param name="value">Node to save.</param>
  public static void Write(Type type, object value) => _cache.Add(type, value);
  /// <summary>
  /// Read a value from the cache.
  /// </summary>
  /// <param name="type">Type of the node.</param>
  /// <returns>Node in the cache.</returns>
  public static object Read(Type type) => _cache[type];
  /// <summary>
  /// Check if a value is in the cache.
  /// </summary>
  /// <param name="type">Type of the node.</param>
  /// <returns>True if the value is saved in the cache.</returns>
  public static bool Has(Type type) => _cache.ContainsKey(type);
}
