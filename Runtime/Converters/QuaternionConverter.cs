using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace NekoSerializer
{
    /// <summary>
    /// JSON Converter for Unity Quaternion using Newtonsoft.Json
    /// </summary>
    public class QuaternionConverter : JsonConverter<Quaternion>
    {
        public override void WriteJson(JsonWriter writer, Quaternion value, JsonSerializer serializer)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("x");
            writer.WriteValue(value.x);
            writer.WritePropertyName("y");
            writer.WriteValue(value.y);
            writer.WritePropertyName("z");
            writer.WriteValue(value.z);
            writer.WritePropertyName("w");
            writer.WriteValue(value.w);
            writer.WriteEndObject();
        }

        public override Quaternion ReadJson(JsonReader reader, Type objectType, Quaternion existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
                return Quaternion.identity;

            JObject obj = JObject.Load(reader);

            float x = obj["x"]?.Value<float>() ?? 0f;
            float y = obj["y"]?.Value<float>() ?? 0f;
            float z = obj["z"]?.Value<float>() ?? 0f;
            float w = obj["w"]?.Value<float>() ?? 1f;

            return new Quaternion(x, y, z, w);
        }
    }
}
