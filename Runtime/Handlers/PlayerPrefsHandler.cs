using System;
using System.Collections.Generic;
using NekoLib.Logger;
using UnityEngine;

namespace NekoSerialize
{
    /// <summary>
    /// Handles saving and loading data using PlayerPrefs.
    /// </summary>
    public class PlayerPrefsHandler : SaveDataHandler
    {
        private const string RegistryKey = "PlayerPrefsKeyRegistry";
        private readonly List<string> _cachedKeys = new(32);

        public PlayerPrefsHandler(SaveLoadSettings settings) : base(settings)
        {
            LoadRegistry();
        }

        protected override void SaveString(string key, string value)
        {
            PlayerPrefs.SetString(key, value);
            PlayerPrefs.Save();

            if (!string.IsNullOrWhiteSpace(key))
            {
                if (!string.Equals(key, RegistryKey, StringComparison.Ordinal) && !_cachedKeys.Contains(key))
                {
                    _cachedKeys.Add(key);
                    PersistRegistry();
                }
            }
        }

        protected override bool TryLoadString(string key, out string value)
        {
            if (!PlayerPrefs.HasKey(key))
            {
                value = default;
                return false;
            }

            value = PlayerPrefs.GetString(key);
            return true;
        }

        protected override void DeleteString(string key)
        {
            if (PlayerPrefs.HasKey(key))
            {
                PlayerPrefs.DeleteKey(key);
            }
            PlayerPrefs.Save();

            if (!string.IsNullOrWhiteSpace(key))
            {
                if (!string.Equals(key, RegistryKey, StringComparison.Ordinal) && _cachedKeys.Remove(key))
                {
                    PersistRegistry();
                }
            }
        }

        public override IEnumerable<string> Keys()
        {
            return _cachedKeys.ToArray();
        }

        public override bool Exists(string key)
        {
            return PlayerPrefs.HasKey(key);
        }

        public override void DeleteAll()
        {
            foreach (var key in _cachedKeys)
            {
                if (PlayerPrefs.HasKey(key))
                {
                    PlayerPrefs.DeleteKey(key);
                }
            }

            _cachedKeys.Clear();

            if (PlayerPrefs.HasKey(RegistryKey))
            {
                PlayerPrefs.DeleteKey(RegistryKey);
            }

            PlayerPrefs.Save();
        }

        /// <summary>
        /// Loads the registry of saved keys from PlayerPrefs.
        /// </summary>
        private void LoadRegistry()
        {
            try
            {
                if (!PlayerPrefs.HasKey(RegistryKey))
                    return;

                var json = PlayerPrefs.GetString(RegistryKey, string.Empty);
                if (string.IsNullOrWhiteSpace(json))
                    return;

                var keys = DeserializeData<List<string>>(json);
                if (keys == null)
                    return;

                foreach (var key in keys)
                {
                    if (string.IsNullOrWhiteSpace(key))
                        continue;
                    if (string.Equals(key, RegistryKey, StringComparison.Ordinal))
                        continue;

                    if (!_cachedKeys.Contains(key))
                    {
                        _cachedKeys.Add(key);
                    }
                }
            }
            catch
            {
                Log.Warn("[PlayerPrefsHandler] Failed to load PlayerPrefs registry.");
            }
        }

        /// <summary>
        /// Persists the registry of saved keys to PlayerPrefs.
        /// </summary>
        private void PersistRegistry()
        {
            try
            {
                var json = SerializeData(_cachedKeys);
                PlayerPrefs.SetString(RegistryKey, json);
                PlayerPrefs.Save();
            }
            catch
            {
                Log.Warn("[PlayerPrefsHandler] Failed to persist PlayerPrefs registry.");
            }
        }
    }
}
