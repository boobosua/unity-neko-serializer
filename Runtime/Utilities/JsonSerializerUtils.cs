using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace NekoSerialize
{
    /// <summary>
    /// Utility class to configure JsonSerializer with Unity type converters
    /// </summary>
    internal static class JsonSerializerUtils
    {
        private static readonly JsonSerializerSettings s_settings;
        private static readonly JsonSerializer s_serializer;

        static JsonSerializerUtils()
        {
            s_settings = CreateSettings();
            s_serializer = JsonSerializer.Create(s_settings);
        }

        /// <summary>
        /// Creates JsonSerializerSettings with all Unity type converters configured
        /// </summary>
        private static JsonSerializerSettings CreateSettings()
        {
            var settings = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                NullValueHandling = NullValueHandling.Ignore,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            };

            // Add Unity type converters.
            settings.Converters.Add(new Vector3Converter());
            settings.Converters.Add(new Vector2Converter());
            settings.Converters.Add(new Vector4Converter());
            settings.Converters.Add(new Vector3IntConverter());
            settings.Converters.Add(new Vector2IntConverter());
            settings.Converters.Add(new QuaternionConverter());
            settings.Converters.Add(new ColorConverter());
            settings.Converters.Add(new RectConverter());
            settings.Converters.Add(new BoundsConverter());
            settings.Converters.Add(new TransformDataConverter());

            return settings;
        }

        /// <summary>
        /// Gets the configured JsonSerializerSettings.
        /// </summary>
        public static JsonSerializerSettings GetSettings()
        {
            return s_settings;
        }

        /// <summary>
        /// Gets the configured JsonSerializer.
        /// </summary>
        public static JsonSerializer GetSerializer()
        {
            return s_serializer;
        }

        /// <summary>
        /// Serializes an object to JSON string using the configured settings.
        /// </summary>
        public static string SerializeObject(object obj)
        {
            return JsonConvert.SerializeObject(obj, s_settings);
        }

        /// <summary>
        /// Deserializes a JSON string to an object of type T using the configured settings.
        /// </summary>
        public static T DeserializeObject<T>(string json)
        {
            return JsonConvert.DeserializeObject<T>(json, s_settings) ?? default;
        }

        /// <summary>
        /// Deserializes a JObject to an object of type T using the configured settings.
        /// </summary>
        public static T DeserializeJObject<T>(JObject jObject)
        {
            using var reader = jObject.CreateReader();
            return s_serializer.Deserialize<T>(reader) ?? default;
        }

        /// <summary>
        /// Deserializes a JToken to an object of type T using the configured settings.
        /// </summary>
        public static T DeserializeJToken<T>(object data)
        {
            var jToken = JToken.FromObject(data);
            return jToken.ToObject<T>(s_serializer) ?? default;
        }
    }
}
