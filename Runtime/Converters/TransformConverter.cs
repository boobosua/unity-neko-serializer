using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace NekoSerializer
{
    /// <summary>
    /// JSON Converter for Unity Transform using Newtonsoft.Json
    /// This converter serializes Transform data (position, rotation, scale) but not the actual Transform component
    /// </summary>
    public class TransformConverter : JsonConverter<Transform>
    {
        public override void WriteJson(JsonWriter writer, Transform value, JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }

            writer.WriteStartObject();

            writer.WritePropertyName("position");
            serializer.Serialize(writer, value.position);

            writer.WritePropertyName("rotation");
            serializer.Serialize(writer, value.rotation);

            writer.WritePropertyName("localScale");
            serializer.Serialize(writer, value.localScale);

            writer.WriteEndObject();
        }

        public override Transform ReadJson(JsonReader reader, Type objectType, Transform existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
                return null;

            JObject obj = JObject.Load(reader);

            // Note: We cannot create new Transform components at runtime
            // This converter is mainly for serializing transform data
            // If you need to apply this data to an existing transform, use the data from JSON

            // For demonstration, we'll create a TransformData class instead
            throw new NotSupportedException("Transform components cannot be directly deserialized. Use TransformDataConverter instead for transform data serialization.");
        }
    }

    /// <summary>
    /// Serializable Transform data structure for JSON serialization
    /// </summary>
    [Serializable]
    public struct TransformData
    {
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 localScale;

        public TransformData(Transform transform)
        {
            position = transform.position;
            rotation = transform.rotation;
            localScale = transform.localScale;
        }

        public void ApplyTo(Transform transform)
        {
            if (transform != null)
            {
                transform.SetPositionAndRotation(position, rotation);
                transform.localScale = localScale;
            }
        }
    }

    /// <summary>
    /// JSON Converter for TransformData using Newtonsoft.Json
    /// Use this for serializing transform data that can be applied to transforms later
    /// </summary>
    public class TransformDataConverter : JsonConverter<TransformData>
    {
        public override void WriteJson(JsonWriter writer, TransformData value, JsonSerializer serializer)
        {
            writer.WriteStartObject();

            writer.WritePropertyName("position");
            serializer.Serialize(writer, value.position);

            writer.WritePropertyName("rotation");
            serializer.Serialize(writer, value.rotation);

            writer.WritePropertyName("localScale");
            serializer.Serialize(writer, value.localScale);

            writer.WriteEndObject();
        }

        public override TransformData ReadJson(JsonReader reader, Type objectType, TransformData existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
                return default;

            JObject obj = JObject.Load(reader);

            Vector3 position = obj["position"]?.ToObject<Vector3>(serializer) ?? Vector3.zero;
            Quaternion rotation = obj["rotation"]?.ToObject<Quaternion>(serializer) ?? Quaternion.identity;
            Vector3 localScale = obj["localScale"]?.ToObject<Vector3>(serializer) ?? Vector3.one;

            return new TransformData
            {
                position = position,
                rotation = rotation,
                localScale = localScale
            };
        }
    }
}
