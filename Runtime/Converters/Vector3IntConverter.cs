using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace NekoSerializer
{
    /// <summary>
    /// JSON Converter for Unity Vector3Int using Newtonsoft.Json
    /// </summary>
    public class Vector3IntConverter : JsonConverter<Vector3Int>
    {
        public override void WriteJson(JsonWriter writer, Vector3Int value, JsonSerializer serializer)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("x");
            writer.WriteValue(value.x);
            writer.WritePropertyName("y");
            writer.WriteValue(value.y);
            writer.WritePropertyName("z");
            writer.WriteValue(value.z);
            writer.WriteEndObject();
        }

        public override Vector3Int ReadJson(JsonReader reader, Type objectType, Vector3Int existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
                return Vector3Int.zero;

            JObject obj = JObject.Load(reader);

            int x = obj["x"]?.Value<int>() ?? 0;
            int y = obj["y"]?.Value<int>() ?? 0;
            int z = obj["z"]?.Value<int>() ?? 0;

            return new Vector3Int(x, y, z);
        }
    }
}
