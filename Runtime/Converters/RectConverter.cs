using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace NekoSerializer
{
    /// <summary>
    /// JSON Converter for Unity Rect using Newtonsoft.Json
    /// </summary>
    public class RectConverter : JsonConverter<Rect>
    {
        public override void WriteJson(JsonWriter writer, Rect value, JsonSerializer serializer)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("x"); writer.WriteValue(value.x);
            writer.WritePropertyName("y"); writer.WriteValue(value.y);
            writer.WritePropertyName("width"); writer.WriteValue(value.width);
            writer.WritePropertyName("height"); writer.WriteValue(value.height);
            writer.WriteEndObject();
        }

        public override Rect ReadJson(JsonReader reader, Type objectType, Rect existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null) return new Rect();
            JObject obj = JObject.Load(reader);
            float x = obj["x"]?.Value<float>() ?? 0f;
            float y = obj["y"]?.Value<float>() ?? 0f;
            float w = obj["width"]?.Value<float>() ?? 0f;
            float h = obj["height"]?.Value<float>() ?? 0f;
            return new Rect(x, y, w, h);
        }
    }
}
