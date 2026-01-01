using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using NekoLib.Core;
using NekoLib.Extensions;
using NekoLib.Logger;
using NekoLib.Services;
using NekoLib.Utilities;
using UnityEngine;

namespace NekoSerializer
{
    /// <summary>
    /// Core save/load service with separated memory and storage operations.
    /// </summary>
    internal static class SerializationService
    {
        private const string LastSaveTimeKey = "LastSaveTime";

        private static DataSerializationHandler s_dataHandler;
        private static SerializerSettings s_settings;

        private static SerializerSettings Settings
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

        private static DataSerializationHandler Handler
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
            s_settings = Resources.Load<SerializerSettings>("SerializerSettings");
            if (s_settings == null)
            {
                Log.Warn("[SerializationService] No SerializerSettings found in Resources folder. Using default settings in memory.");
                s_settings = ScriptableObject.CreateInstance<SerializerSettings>();
            }
        }

        /// <summary>
        /// Initialize the data handler based on current settings.
        /// </summary>
        private static void CreateDataHandler()
        {
            var settings = Settings;
            s_dataHandler = settings.StorageOption switch
            {
                StorageOption.PlayerPrefs => new PlayerPrefsHandler(settings),
                StorageOption.JsonFile => new SingleJsonFileHandler(settings),
                _ => new PlayerPrefsHandler(settings)
            };
        }

        /// <summary>
        /// Save data directly to persistent storage immediately.
        /// </summary>
        public static void Save<T>(string key, T data)
        {
            Handler.Save(key, data);
            var nowUtc = DateTimeService.UtcNow;
            Handler.Save(LastSaveTimeKey, nowUtc);

#if UNITY_EDITOR
            TrackEditorSave(key, data);
            TrackEditorSave(LastSaveTimeKey, nowUtc);
#endif
        }

#if UNITY_EDITOR
        /// <summary>
        /// Save data directly to persistent storage immediately without updating last save time.
        /// Intended for editor tooling.
        /// </summary>
        public static void SaveWithoutUpdatingLastSaveTime<T>(string key, T data)
        {
            Handler.Save(key, data);
            TrackEditorSave(key, data);
        }
#endif

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

        /// <summary>
        /// Load data asynchronously for the specified key.
        /// </summary>
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
#if UNITY_EDITOR
            s_editorCache.Remove(key);
            UntrackEditorKey(key);
#endif
        }

        /// <summary>
        /// Bundle all saved data into a single string.
        /// </summary>
        public static string Pack(params string[] keys)
        {
            return Handler.Pack(keys);
        }

        /// <summary>
        /// Unbundle data from a single string into the save service.
        /// </summary>
        public static void Unpack(string packedData, bool overwriteExisting = true)
        {
            Handler.Unpack(packedData, overwriteExisting);

#if UNITY_EDITOR
            var dict = Handler.DeserializeData<Dictionary<string, string>>(packedData);

            if (dict != null)
            {
                foreach (var kv in dict)
                {
                    if (Handler.TryLoad<object>(kv.Key, out var value))
                    {
                        TrackEditorSave(kv.Key, value);
                    }
                }
            }
#endif
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
        private static readonly Dictionary<string, object> s_editorCache = new();

        private static void TrackEditorSave(string key, object data)
        {
            s_editorCache[key] = data;
            TrackEditorKey(key);
        }

        private static void TrackEditorKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return;

            try
            {
                var keys = LoadEditorKeys();
                if (!keys.Contains(key))
                {
                    keys.Add(key);
                    SaveEditorKeys(keys);
                }
            }
            catch
            {
                // Ignore editor cache persistence failures.
            }
        }

        private static void UntrackEditorKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return;

            try
            {
                var keys = LoadEditorKeys();
                if (keys.Remove(key))
                {
                    SaveEditorKeys(keys);
                }
            }
            catch
            {
                // Ignore editor cache persistence failures.
            }
        }

        private static List<string> LoadEditorKeys()
        {
            try
            {
                return SerializerProjectEditorState.GetOrCreate().GetEditorCacheKeysCopy();
            }
            catch
            {
                return new List<string>();
            }
        }

        private static void SaveEditorKeys(List<string> keys)
        {
            try
            {
                var state = SerializerProjectEditorState.GetOrCreate();
                state.SetEditorCacheKeys(keys ?? new List<string>());
                state.SaveIfDirty();
            }
            catch
            {
                // Ignore editor cache persistence failures.
            }
        }

        private static void WarmEditorCacheFromStorage()
        {
            try
            {
                var settings = Settings;
                if (settings.StorageOption == StorageOption.JsonFile)
                {
                    var saveDir = Path.Combine(Application.persistentDataPath, settings.SaveDirectory);
                    if (Directory.Exists(saveDir))
                    {
                        foreach (var filePath in Directory.GetFiles(saveDir, "*.json"))
                        {
                            var key = Path.GetFileNameWithoutExtension(filePath);
                            if (string.IsNullOrWhiteSpace(key))
                                continue;

                            if (Handler.TryLoad<object>(key, out var value))
                            {
                                TrackEditorSave(key, value);
                            }
                        }
                    }
                }
                else if (settings.StorageOption == StorageOption.PlayerPrefs)
                {
                    // PlayerPrefs keys cannot be enumerated; warm using previously tracked editor keys.
                    var keys = LoadEditorKeys();
                    foreach (var key in keys)
                    {
                        if (string.IsNullOrWhiteSpace(key))
                            continue;

                        if (Handler.TryLoad<object>(key, out var value))
                        {
                            TrackEditorSave(key, value);
                        }
                    }
                }
            }
            catch
            {
                // Best-effort only.
            }
        }

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
            if (state == UnityEditor.PlayModeStateChange.EnteredPlayMode)
            {
                WarmEditorCacheFromStorage();
            }

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

                Log.Info("[SerializationService] Service disposed and cleaned up.");
            }
            catch (Exception e)
            {
                Log.Error($"[SerializationService] Error during disposal: {e.Message.Colorize(Swatch.VR)}");
            }
        }

        /// <summary>
        /// Gets a live read-only view of the editor save cache.
        /// This avoids allocating a new dictionary every call.
        /// </summary>
        public static IReadOnlyDictionary<string, object> GetAllSaveData()
        {
            return s_editorCache;
        }

        /// <summary>
        /// Gets a mutable snapshot of the editor save cache.
        /// Use this if you need to modify the returned dictionary.
        /// </summary>
        public static Dictionary<string, object> GetAllSaveDataCopy()
        {
            return new Dictionary<string, object>(s_editorCache);
        }
#endif
    }
}
