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
        /// Clears all data from the save service.
        /// </summary>
        public static void ClearAll()
        {
            SaveLoadService.DeleteAllData();
        }

        /// <summary>
        /// Bundles all saved data into a single string.
        /// </summary>
        public static string BundleData()
        {
            return SaveLoadService.Pack();
        }

        /// <summary>
        /// Unbundles data from a single string into the save service.
        /// </summary>
        public static void UnbundleData(string packedData, bool overwriteExisting = true)
        {
            SaveLoadService.Unpack(packedData, overwriteExisting);
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
