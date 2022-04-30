
namespace GoDotNet {
  using System;
  using Newtonsoft.Json;
  internal class ArrayJsonConverter : JsonConverter {
    public override bool CanConvert(Type objectType)
       => objectType == typeof(Godot.Collections.Array);

    public override object ReadJson(
      JsonReader reader,
      Type objectType,
      object? existingValue,
      JsonSerializer serializer
    ) {
      var list = new Godot.Collections.Array<dynamic?>();
      if (reader.TokenType == JsonToken.Null) {
        return list;
      }

      if (reader.TokenType != JsonToken.StartObject) {
        throw new JsonSerializationException(
          $"Unexpected token {reader.TokenType} when parsing list."
        );
      }

      while (reader.Read()) {
        if (reader.TokenType == JsonToken.EndObject) {
          break;
        }

        if (reader.TokenType != JsonToken.PropertyName) {
          throw new JsonSerializationException(
            $"Unexpected token {reader.TokenType} when parsing list."
          );
        }

        var item = serializer.Deserialize(reader);
        list.Add(item);
      }
      return list;
    }

    public override void WriteJson(
      JsonWriter writer, object? value, JsonSerializer serializer
    ) {
      if (value == null) {
        return;
      }

      writer.WriteStartObject();
      var list = (value as Godot.Collections.Array<dynamic?>)!;
      for (var i = 0; i < list.Count; i++) {
        var item = list[i];
        serializer.Serialize(writer, item);
      }
      writer.WriteEndObject();
    }
  }
}
