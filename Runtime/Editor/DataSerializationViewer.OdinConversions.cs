#if UNITY_EDITOR && ODIN_INSPECTOR

using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace NekoSerializer
{
    public partial class DataSerializationViewer
    {
        private static bool TryConvertVectorJArrayToTypedList(object value, out object typedList)
        {
            typedList = null;
            if (value is not JArray ja)
                return false;

            // Only convert when every element looks like a vector object.
            bool allV3 = true;
            bool allV2 = true;

            for (int i = 0; i < ja.Count; i++)
            {
                if (ja[i] is not JObject jo)
                {
                    allV3 = false;
                    allV2 = false;
                    break;
                }

                bool looksV2 = jo.ContainsKey("x") && jo.ContainsKey("y") && !jo.ContainsKey("z");
                bool looksV3 = jo.ContainsKey("x") && jo.ContainsKey("y") && jo.ContainsKey("z") && !jo.ContainsKey("w");

                allV3 &= looksV3;
                allV2 &= looksV2;
                if (!allV3 && !allV2)
                    break;
            }

            if (allV3)
            {
                var list = new List<Vector3>(ja.Count);
                for (int i = 0; i < ja.Count; i++)
                {
                    var jo = (JObject)ja[i];
                    float x = jo["x"]?.ToObject<float>() ?? 0f;
                    float y = jo["y"]?.ToObject<float>() ?? 0f;
                    float z = jo["z"]?.ToObject<float>() ?? 0f;
                    list.Add(new Vector3(x, y, z));
                }

                typedList = list;
                return true;
            }

            if (allV2)
            {
                var list = new List<Vector2>(ja.Count);
                for (int i = 0; i < ja.Count; i++)
                {
                    var jo = (JObject)ja[i];
                    float x = jo["x"]?.ToObject<float>() ?? 0f;
                    float y = jo["y"]?.ToObject<float>() ?? 0f;
                    list.Add(new Vector2(x, y));
                }

                typedList = list;
                return true;
            }

            return false;
        }

        private static bool TryConvertJTokenRootToOdinValue(object value, out object odinValue, out Func<object, object> convertBack)
        {
            odinValue = null;
            convertBack = null;

            if (value is JArray ja)
            {
                // Prefer specific inferred types when possible.
                if (TryConvertVectorJArrayToTypedList(ja, out var typedVecList))
                {
                    odinValue = typedVecList;
                    convertBack = edited => JTokenFromObject(edited) as JArray ?? new JArray();
                    return true;
                }

                // Homogeneous primitive arrays -> typed lists
                if (TryConvertPrimitiveJArrayToTypedList(ja, out var typedPrimitiveList))
                {
                    odinValue = typedPrimitiveList;
                    convertBack = edited => JTokenFromObject(edited) as JArray ?? new JArray();
                    return true;
                }

                // Fallback: recursively convert into List<object>
                var list = new List<object>(ja.Count);
                for (int i = 0; i < ja.Count; i++)
                    list.Add(JTokenToDotNet(ja[i]));

                odinValue = list;
                convertBack = edited => JTokenFromObject(edited) as JArray ?? new JArray();
                return true;
            }

            if (value is JObject jo)
            {
                // Vector root -> true Vector2/Vector3 so Odin draws a normal vector field.
                if (LooksLikeVector3JObject(jo))
                {
                    float x = jo["x"]?.ToObject<float>() ?? 0f;
                    float y = jo["y"]?.ToObject<float>() ?? 0f;
                    float z = jo["z"]?.ToObject<float>() ?? 0f;
                    odinValue = new Vector3(x, y, z);
                    convertBack = edited => JTokenFromObject(edited) as JObject ?? new JObject();
                    return true;
                }

                if (LooksLikeVector2JObject(jo))
                {
                    float x = jo["x"]?.ToObject<float>() ?? 0f;
                    float y = jo["y"]?.ToObject<float>() ?? 0f;
                    odinValue = new Vector2(x, y);
                    convertBack = edited => JTokenFromObject(edited) as JObject ?? new JObject();
                    return true;
                }

                // Try to infer dictionary value type.
                if (TryConvertVectorJObjectToTypedDictionary(jo, out var typedVecDict))
                {
                    odinValue = typedVecDict;
                    convertBack = edited => JTokenFromObject(edited) as JObject ?? new JObject();
                    return true;
                }

                if (TryConvertPrimitiveJObjectToTypedDictionary(jo, out var typedPrimitiveDict))
                {
                    odinValue = typedPrimitiveDict;
                    convertBack = edited => JTokenFromObject(edited) as JObject ?? new JObject();
                    return true;
                }

                // Fallback: recursively convert into Dictionary<string, object>
                var dict = new Dictionary<string, object>(StringComparer.Ordinal);
                foreach (var prop in jo.Properties())
                    dict[prop.Name] = JTokenToDotNet(prop.Value);

                odinValue = dict;
                convertBack = edited => JTokenFromObject(edited) as JObject ?? new JObject();
                return true;
            }

            return false;
        }

        private static bool TryConvertPrimitiveJArrayToTypedList(JArray ja, out object typedList)
        {
            typedList = null;
            if (ja == null || ja.Count == 0)
                return false;

            bool allString = true;
            bool allBool = true;
            bool allInteger = true;
            bool allNumber = true;

            for (int i = 0; i < ja.Count; i++)
            {
                if (ja[i] is not JValue jv)
                {
                    allString = false;
                    allBool = false;
                    allInteger = false;
                    allNumber = false;
                    break;
                }

                allString &= jv.Type == JTokenType.String;
                allBool &= jv.Type == JTokenType.Boolean;
                allInteger &= jv.Type == JTokenType.Integer;
                allNumber &= jv.Type == JTokenType.Integer || jv.Type == JTokenType.Float;
            }

            if (allString)
            {
                var list = new List<string>(ja.Count);
                for (int i = 0; i < ja.Count; i++)
                    list.Add(((JValue)ja[i]).ToObject<string>());
                typedList = list;
                return true;
            }

            if (allBool)
            {
                var list = new List<bool>(ja.Count);
                for (int i = 0; i < ja.Count; i++)
                    list.Add(((JValue)ja[i]).ToObject<bool>());
                typedList = list;
                return true;
            }

            if (allInteger)
            {
                var list = new List<long>(ja.Count);
                for (int i = 0; i < ja.Count; i++)
                    list.Add(((JValue)ja[i]).ToObject<long>());
                typedList = list;
                return true;
            }

            if (allNumber)
            {
                var list = new List<double>(ja.Count);
                for (int i = 0; i < ja.Count; i++)
                    list.Add(((JValue)ja[i]).ToObject<double>());
                typedList = list;
                return true;
            }

            return false;
        }

        private static bool TryConvertVectorJObjectToTypedDictionary(JObject jo, out object typedDict)
        {
            typedDict = null;
            if (jo == null)
                return false;

            bool allV3 = true;
            bool allV2 = true;
            foreach (var prop in jo.Properties())
            {
                if (prop.Value is not JObject vObj)
                {
                    allV3 = false;
                    allV2 = false;
                    break;
                }

                bool looksV2 = vObj.ContainsKey("x") && vObj.ContainsKey("y") && !vObj.ContainsKey("z");
                bool looksV3 = vObj.ContainsKey("x") && vObj.ContainsKey("y") && vObj.ContainsKey("z") && !vObj.ContainsKey("w");

                allV3 &= looksV3;
                allV2 &= looksV2;
                if (!allV3 && !allV2)
                    break;
            }

            if (allV3)
            {
                var dict = new Dictionary<string, Vector3>(StringComparer.Ordinal);
                foreach (var prop in jo.Properties())
                {
                    var v = (JObject)prop.Value;
                    float x = v["x"]?.ToObject<float>() ?? 0f;
                    float y = v["y"]?.ToObject<float>() ?? 0f;
                    float z = v["z"]?.ToObject<float>() ?? 0f;
                    dict[prop.Name] = new Vector3(x, y, z);
                }

                typedDict = dict;
                return true;
            }

            if (allV2)
            {
                var dict = new Dictionary<string, Vector2>(StringComparer.Ordinal);
                foreach (var prop in jo.Properties())
                {
                    var v = (JObject)prop.Value;
                    float x = v["x"]?.ToObject<float>() ?? 0f;
                    float y = v["y"]?.ToObject<float>() ?? 0f;
                    dict[prop.Name] = new Vector2(x, y);
                }

                typedDict = dict;
                return true;
            }

            return false;
        }

        private static bool TryConvertPrimitiveJObjectToTypedDictionary(JObject jo, out object typedDict)
        {
            typedDict = null;
            if (jo == null || !jo.HasValues)
                return false;

            bool allString = true;
            bool allBool = true;
            bool allInteger = true;
            bool allNumber = true;

            foreach (var prop in jo.Properties())
            {
                if (prop.Value is not JValue jv)
                {
                    allString = false;
                    allBool = false;
                    allInteger = false;
                    allNumber = false;
                    break;
                }

                allString &= jv.Type == JTokenType.String;
                allBool &= jv.Type == JTokenType.Boolean;
                allInteger &= jv.Type == JTokenType.Integer;
                allNumber &= jv.Type == JTokenType.Integer || jv.Type == JTokenType.Float;
            }

            if (allString)
            {
                var dict = new Dictionary<string, string>(StringComparer.Ordinal);
                foreach (var prop in jo.Properties())
                    dict[prop.Name] = ((JValue)prop.Value).ToObject<string>();
                typedDict = dict;
                return true;
            }

            if (allBool)
            {
                var dict = new Dictionary<string, bool>(StringComparer.Ordinal);
                foreach (var prop in jo.Properties())
                    dict[prop.Name] = ((JValue)prop.Value).ToObject<bool>();
                typedDict = dict;
                return true;
            }

            if (allInteger)
            {
                var dict = new Dictionary<string, long>(StringComparer.Ordinal);
                foreach (var prop in jo.Properties())
                    dict[prop.Name] = ((JValue)prop.Value).ToObject<long>();
                typedDict = dict;
                return true;
            }

            if (allNumber)
            {
                var dict = new Dictionary<string, double>(StringComparer.Ordinal);
                foreach (var prop in jo.Properties())
                    dict[prop.Name] = ((JValue)prop.Value).ToObject<double>();
                typedDict = dict;
                return true;
            }

            return false;
        }

        private static object JTokenToDotNet(JToken token)
        {
            if (token == null)
                return null;

            if (token is JValue jv)
                return jv.Value;

            if (token is JObject jo)
            {
                // Prefer real Unity vectors over dictionaries for {x,y[,z]}.
                if (LooksLikeVector3JObject(jo))
                {
                    float x = jo["x"]?.ToObject<float>() ?? 0f;
                    float y = jo["y"]?.ToObject<float>() ?? 0f;
                    float z = jo["z"]?.ToObject<float>() ?? 0f;
                    return new Vector3(x, y, z);
                }

                if (LooksLikeVector2JObject(jo))
                {
                    float x = jo["x"]?.ToObject<float>() ?? 0f;
                    float y = jo["y"]?.ToObject<float>() ?? 0f;
                    return new Vector2(x, y);
                }

                var dict = new Dictionary<string, object>(StringComparer.Ordinal);
                foreach (var prop in jo.Properties())
                    dict[prop.Name] = JTokenToDotNet(prop.Value);
                return dict;
            }

            if (token is JArray ja)
            {
                var list = new List<object>(ja.Count);
                for (int i = 0; i < ja.Count; i++)
                    list.Add(JTokenToDotNet(ja[i]));
                return list;
            }

            return token.ToObject<object>();
        }

        private static JToken JTokenFromObject(object value)
        {
            if (value == null)
                return JValue.CreateNull();

            if (value is JToken jt)
                return jt;

            // Dictionaries -> JObject
            if (value is System.Collections.IDictionary dict)
            {
                var jo = new JObject();
                foreach (System.Collections.DictionaryEntry entry in dict)
                {
                    string key = entry.Key?.ToString() ?? string.Empty;
                    jo[key] = JTokenFromObject(entry.Value);
                }
                return jo;
            }

            // Enumerables -> JArray (but avoid treating string as enumerable)
            if (value is System.Collections.IEnumerable enumerable && value is not string)
            {
                var ja = new JArray();
                foreach (var item in enumerable)
                    ja.Add(JTokenFromObject(item));
                return ja;
            }

            // IMPORTANT: Use our configured serializer (Unity converters + ReferenceLoopHandling.Ignore).
            // This prevents issues like Vector3's reflected properties (e.g., 'normalized') causing
            // a self-referencing loop when Json.NET tries to serialize Unity structs by reflection.
            return JToken.FromObject(value, JsonSerializerUtils.GetSerializer());
        }

        private static bool LooksLikeVector2JObject(JObject jo)
        {
            if (jo == null) return false;
            return jo.ContainsKey("x") && jo.ContainsKey("y") && !jo.ContainsKey("z") && !jo.ContainsKey("w");
        }

        private static bool LooksLikeVector3JObject(JObject jo)
        {
            if (jo == null) return false;
            return jo.ContainsKey("x") && jo.ContainsKey("y") && jo.ContainsKey("z") && !jo.ContainsKey("w");
        }
    }
}

#endif
