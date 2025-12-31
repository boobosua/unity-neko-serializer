using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace NekoSerializer
{
    /// <summary>
    /// JSON Converter for Unity Bounds using Newtonsoft.Json
    /// Serializes center (Vector3) and size (Vector3).
    /// </summary>
    public class BoundsConverter : JsonConverter<Bounds>
    {
        public override void WriteJson(JsonWriter writer, Bounds value, JsonSerializer serializer)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("center");
            serializer.Serialize(writer, value.center);
            writer.WritePropertyName("size");
            serializer.Serialize(writer, value.size);
            writer.WriteEndObject();
        }

        public override Bounds ReadJson(JsonReader reader, Type objectType, Bounds existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null) return new Bounds(Vector3.zero, Vector3.zero);
            JObject obj = JObject.Load(reader);
            Vector3 center = obj["center"]?.ToObject<Vector3>(serializer) ?? Vector3.zero;
            Vector3 size = obj["size"]?.ToObject<Vector3>(serializer) ?? Vector3.zero;
            return new Bounds(center, size);
        }
    }
}
