namespace GoDotNet {
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using GoDotLog;
  using Newtonsoft.Json;
  using Newtonsoft.Json.Linq;

  /// <summary>
  /// A base object that can be marshalled through Godot.
  ///
  /// Instead of inheriting from <see cref="Godot.Object"/> or
  /// <see cref="Godot.Reference"/>, classes which need to be marshalled through
  /// Godot can extend this class. This class also provides static methods which
  /// aid in the serialization of the object to dynamic dictionaries and string
  /// dictionaries.
  /// </summary>
  [JsonObject(MemberSerialization.OptIn)]
  public class GDObject : Godot.Reference {
    /// <summary>
    /// Deserializes a GDObject from a dictionary produced by
    /// <see cref="ToData"/>.
    /// </summary>
    /// <param name="data">Dictionary data.</param>
    /// <typeparam name="T">Type of GDObject to deserialize to.</typeparam>
    /// <returns>GDObject instance.</returns>
    public static T FromData<T>(Dictionary<string, object?> data)
      where T : GDObject => JObject.FromObject(data).ToObject<T>()!;

    /// <summary>
    /// Serializes a <see cref="GDObject"/> to a dictionary.
    /// </summary>
    /// <param name="obj">Object to serialize.</param>
    /// <typeparam name="T">Type of object to serialize.</typeparam>
    /// <returns>Dictionary data.</returns>
    public static Dictionary<string, object?> ToData<T>(T obj)
      where T : GDObject => new GDLog(obj.GetType().Name).Run(
        () => JObject.FromObject(obj).ToObject<Dictionary<string, object?>>()!
      );

    /// <summary>
    /// Deserializes a GDObject from a string dictionary produced by
    /// <see cref="ToStringData"/>.
    /// </summary>
    /// <param name="data">Dictionary data.</param>
    /// <typeparam name="T">Type of GDObject to deserialize to.</typeparam>
    /// <returns>An object instance.</returns>
    public static T FromStringData<T>(Dictionary<string, string> data)
      where T : GDObject => JObject.FromObject(data).ToObject<T>()!;

    /// <summary>
    /// Serializes a <see cref="GDObject"/> to a string dictionary.
    /// </summary>
    /// <param name="obj">Object to serialize.</param>
    /// <typeparam name="T">Type of object to serialize.</typeparam>
    /// <returns>Dictionary data.</returns>
    public static Dictionary<string, string> ToStringData<T>(T obj)
      where T : GDObject => new GDLog(obj.GetType().Name).Run(
        () => JObject.FromObject(obj).ToObject<Dictionary<string, string>>()!
      );

    /// <inheritdoc/>
    public static HashSet<string> GetKeys<T>(T obj) where T : GDObject
      => JObject.FromObject(obj).Properties().Select(
        prop => prop.Name
      ).ToHashSet();

    #region Equality

    /// <inheritdoc/>
    public override bool Equals(object obj)
      => obj is GDObject other && this == other;

    /// <inheritdoc/>
    public override int GetHashCode() {
      var hashCode = new HashCode();
      foreach (var property in GetType().GetProperties()) {
        hashCode.Add(property.GetValue(this).GetHashCode());
      }
      return hashCode.ToHashCode();
    }

    /// <inheritdoc/>
    public static bool operator ==(GDObject? a, GDObject? b)
      => a?.GetHashCode() == b?.GetHashCode();

    /// <inheritdoc/>
    public static bool operator !=(GDObject? a, GDObject? b) => !(a == b);

    #endregion
  }
}
