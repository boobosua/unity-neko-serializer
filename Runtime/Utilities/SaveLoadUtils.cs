using System;
using System.Threading.Tasks;

namespace NekoSerialize
{
    public static class NSR
    {
        /// <summary>
        /// Saves the specified data directly to storage (PlayerPrefs or per-key JSON file).
        /// </summary>
        public static void Save<T>(string key, T data)
        {
            SaveLoadService.Save(key, data);
        }

        /// <summary>
        /// Saves the specified data asynchronously to storage (PlayerPrefs or per-key JSON file).
        /// </summary>
        public static Task SaveAsync<T>(string key, T data)
        {
            return SaveLoadService.SaveAsync(key, data);
        }

        /// <summary>
        /// Loads the specified data from the save service.
        /// </summary>
        public static T Load<T>(string key, T defaultValue = default)
        {
            return SaveLoadService.Load(key, defaultValue);
        }

        /// <summary>
        /// Loads the specified data asynchronously from the save service.
        /// </summary>
        public static Task<T> LoadAsync<T>(string key, T defaultValue = default)
        {
            return SaveLoadService.LoadAsync(key, defaultValue);
        }

        /// <summary>
        /// Checks if the specified data exists in the save service.
        /// </summary>
        public static bool Exists(string key)
        {
            return SaveLoadService.HasData(key);
        }

        /// <summary>
        /// Deletes the specified data from the save service.
        /// </summary>
        public static void Delete(string key)
        {
            SaveLoadService.DeleteData(key);
        }

        /// <summary>
        /// Bundles all saved data into a single string.
        /// </summary>
        public static string Pack(params string[] keys)
        {
            return SaveLoadService.Pack(keys);
        }

        /// <summary>
        /// Unbundles data from a single string into the save service.
        /// </summary>
        public static void Unpack(string packedData, bool overwriteExisting = true)
        {
            SaveLoadService.Unpack(packedData, overwriteExisting);
        }

        /// <summary>
        /// Serializes an object to a JSON string.
        /// </summary>
        public static string Serialize(object obj)
        {
            return JsonSerializerUtils.SerializeObject(obj);
        }

        /// <summary>
        /// Deserializes a JSON string to an object of type T.
        /// </summary>
        public static T Deserialize<T>(string json)
        {
            return JsonSerializerUtils.DeserializeObject<T>(json);
        }

        /// <summary>
        /// Gets the last save time in UTC.
        /// </summary>
        public static DateTime LastSaveTimeUtc
        {
            get { return SaveLoadService.GetLastSaveTimeUtc(); }
        }

        /// <summary>
        /// Gets the last save time in local time.
        /// </summary>
        public static DateTime LastSaveTimeLocal
        {
            get { return SaveLoadService.GetLastSaveTimeLocal(); }
        }
    }
}
