using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NekoLib.Core;
using NekoLib.Extensions;
using NekoLib.Logger;
using NekoLib.Services;
using NekoLib.Utilities;
using UnityEngine;

namespace NekoSerialize
{
    /// <summary>
    /// Core save/load service with separated memory and storage operations.
    /// </summary>
    internal static class SaveLoadService
    {
        private const string LastSaveTimeKey = "LastSaveTime";

        private static SaveDataHandler s_dataHandler;
        private static SaveLoadSettings s_settings;

        private static SaveLoadSettings Settings
        {
            get
            {
                if (s_settings == null)
                {
                    LoadSettings();
                }

                return s_settings;
            }
        }

        private static SaveDataHandler Handler
        {
            get
            {
                if (s_dataHandler == null)
                {
                    CreateDataHandler();
                }

                return s_dataHandler;
            }
        }

        /// <summary>
        /// Load settings from Resources folder.
        /// </summary>
        private static void LoadSettings()
        {
            s_settings = Resources.Load<SaveLoadSettings>("SaveLoadSettings");
            if (s_settings == null)
            {
                Log.Warn("[SaveLoadService] No SaveLoadSettings found in Resources folder. Using default settings in memory.");
                s_settings = ScriptableObject.CreateInstance<SaveLoadSettings>();
            }
        }

        /// <summary>
        /// Initialize the data handler based on current settings.
        /// </summary>
        private static void CreateDataHandler()
        {
            var settings = Settings;
            s_dataHandler = settings.SaveLocation switch
            {
                SaveLocation.PlayerPrefs => new PlayerPrefsHandler(settings),
                SaveLocation.JsonFile => new SingleJsonFileHandler(settings),
                _ => new PlayerPrefsHandler(settings)
            };
        }

        /// <summary>
        /// Save data directly to persistent storage immediately.
        /// </summary>
        public static void Save<T>(string key, T data)
        {
            Handler.Save(key, data);
            Handler.Save(LastSaveTimeKey, DateTimeService.UtcNow);
        }

        /// <summary>
        /// Save data asynchronously to persistent storage.
        /// </summary>
        public static async Task SaveAsync<T>(string key, T data)
        {
            await Task.Run(() => Save(key, data));
        }

        /// <summary>
        /// Load data for the specified key.
        /// </summary>
        public static T Load<T>(string key, T defaultValue = default)
        {
            if (Handler.TryLoad<T>(key, out var value))
                return value;

            return defaultValue;
        }

        public static async Task<T> LoadAsync<T>(string key, T defaultValue = default)
        {
            return await Task.Run(() => Load(key, defaultValue));
        }

        /// <summary>
        /// Check if data exists for the specified key.
        /// </summary>
        public static bool HasData(string key)
        {
            return Handler.Exists(key);
        }

        /// <summary>
        /// Delete data for the specified key.
        /// </summary>
        public static void DeleteData(string key)
        {
            Handler.Delete(key);
        }

        /// <summary>
        /// Delete all save data.
        /// </summary>
        public static void DeleteAllData()
        {
            Handler.DeleteAll();
        }

        /// <summary>
        /// Bundle all saved data into a single string.
        /// </summary>
        public static string Pack()
        {
            return Handler.Pack();
        }

        /// <summary>
        /// Unbundle data from a single string into the save service.
        /// </summary>
        public static void Unpack(string packedData, bool overwriteExisting = true)
        {
            Handler.Unpack(packedData, overwriteExisting);
        }

        /// <summary>
        /// Gets the last save time in UTC.
        /// </summary>
        public static DateTime GetLastSaveTimeUtc()
        {
            return Load(LastSaveTimeKey, DateTime.MinValue);
        }

        /// <summary>
        /// Gets the last save time in local time.
        /// </summary>
        public static DateTime GetLastSaveTimeLocal()
        {
            var utc = GetLastSaveTimeUtc();
            return utc == DateTime.MinValue ? utc : utc.ToLocalTime();
        }

#if UNITY_EDITOR
        /// <summary>
        /// Handle cleanup when play mode exits without domain reload (for editor use).
        /// </summary>
        [UnityEditor.InitializeOnLoadMethod]
        private static void HandlePlayModeStateChanged()
        {
            UnityEditor.EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(UnityEditor.PlayModeStateChange state)
        {
            if (state == UnityEditor.PlayModeStateChange.ExitingPlayMode && Utils.IsReloadDomainDisabled())
            {
                DisposeService();
            }
        }

        /// <summary>
        /// Dispose and cleanup the service (for editor use).
        /// </summary>
        private static void DisposeService()
        {
            try
            {
                // Reset state.
                s_dataHandler = null;
                s_settings = null;

                Log.Info("[SaveLoadService] Service disposed and cleaned up.");
            }
            catch (Exception e)
            {
                Log.Error($"[SaveLoadService] Error during disposal: {e.Message.Colorize(Swatch.VR)}");
            }
        }

        /// <summary>
        /// Get all save data (for editor use).
        /// </summary>
        public static Dictionary<string, object> GetAllSaveData()
        {
            var result = new Dictionary<string, object>();
            foreach (var key in Handler.Keys())
            {
                if (Handler.TryLoad<object>(key, out var value))
                {
                    result[key] = value;
                }
            }
            return result;
        }

        /// <summary>
        /// Check if data is persisted to storage (for editor use).
        /// </summary>
        public static bool IsDataPersisted(string key)
        {
            try
            {
                return Handler.Exists(key);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Get current settings (for editor and runtime use).
        /// </summary>
        public static SaveLoadSettings GetSettings()
        {
            return Settings;
        }

        /// <summary>
        /// Refresh settings (for editor use).
        /// </summary>
        public static void RefreshSettings()
        {
            LoadSettings();
            CreateDataHandler();
            Log.Info("[SaveLoadService] Settings refreshed");
        }
#endif
    }
}
