namespace GoDotNet {
  using System;
  using System.Collections.Generic;
  using System.Linq;

  /// <summary>
  /// Extension on Godot dictionaries that allows them to be converted to dotnet
  /// dictionaries.
  /// </summary>
  public static class DictionaryX {
    /// <summary>
    /// Converts a Godot dictionary into a C# dictionary. Runs in O(n), so try
    /// not to use on huge values.
    /// </summary>
    /// <param name="dictionary">Godot dictionary (receiver).</param>
    /// <typeparam name="TKey">Type of key used in the dictionary.
    /// </typeparam>
    /// <typeparam name="TValue">Type of value used in the dictionary.
    /// </typeparam>
    /// <returns>A dotnet dictionary containing the Godot dictionary
    /// contents.</returns>
    public static Dictionary<TKey, TValue?> ToDotNet<TKey, TValue>(
      this Godot.Collections.Dictionary<TKey, TValue?> dictionary
    ) where TKey : notnull {
      var keys = dictionary.Keys.ToList();
      var values = dictionary.Values.ToList();
      var result = new Dictionary<TKey, TValue?>();
      for (var i = 0; i < keys.Count; i++) {
        var key = keys[i];
        if (key is null) {
          throw new InvalidOperationException(
            $"Dictionary contains a key of type {key?.GetType()} " +
            $"but expected type {typeof(TKey)}"
          );
        }
        dynamic? value = values[i];
        if (value is not TValue) {
          throw new InvalidOperationException(
            $"Dictionary contains a value of type {value?.GetType()} " +
            $"but expected type {typeof(TValue)}"
          );
        }
        result.Add(key, value);
      }
      return result;
    }
  }
}
