namespace GoDotNet {
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
    /// Deserializes a GDObject from a string dictionary produced by
    /// <see cref="ToStringData"/>. This will only work on simple objects
    /// which contain serializable fields that are convertible to a string and
    /// back again. See the GoDotNet docs for examples.
    /// </summary>
    /// <param name="data">Dictionary data.</param>
    /// <typeparam name="T">Type of GDObject to deserialize to.</typeparam>
    /// <returns>An object instance.</returns>
    public static T FromStringData<T>(Dictionary<string, string> data)
      where T : GDObject => JObject.FromObject(data).ToObject<T>()!;

    /// <summary>
    /// Serializes a <see cref="GDObject"/> to a string dictionary. Only use
    /// this when all of the serializable fields in your GDObject are
    /// convertible to and from strings! See the GoDotNet docs for examples.
    /// </summary>
    /// <param name="obj">Object to serialize.</param>
    /// <typeparam name="T">Type of object to serialize.</typeparam>
    /// <returns>Dictionary data.</returns>
    public static Dictionary<string, string> ToStringData<T>(T obj)
      where T : GDObject => new GDLog(obj.GetType().Name).Run(
        () => JObject.FromObject(obj).ToObject<Dictionary<string, string>>()!
      );

    /// <summary>
    /// Computes the properties of the object and returns a set of property
    /// names. Uses Newtonsoft.Json to serialize object properties.
    /// </summary>
    /// <param name="obj">GDObject or subclass instance.</param>
    /// <typeparam name="T">GDObject or subclass.</typeparam>
    /// <returns>Set of property names in the object.</returns>
    public static HashSet<string> GetKeys<T>(T obj) where T : GDObject
      => JObject.FromObject(obj).Properties().Select(
        prop => prop.Name
      ).ToHashSet();
  }
}
