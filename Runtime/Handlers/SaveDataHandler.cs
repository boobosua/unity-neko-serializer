using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace NekoSerialize
{
    public abstract class SaveDataHandler
    {
        protected SaveLoadSettings _settings;
        protected JsonSerializerSettings _jsonSettings;

        public SaveDataHandler(SaveLoadSettings settings)
        {
            _settings = settings;
            _jsonSettings = JsonSerializerUtils.GetSettings();
            _jsonSettings.Formatting = _settings.PrettyPrintJson ? Formatting.Indented : Formatting.None;
        }

        protected abstract void SaveString(string key, string value);
        protected abstract bool TryLoadString(string key, out string value);
        public abstract IEnumerable<string> Keys();
        public abstract bool Exists(string key);
        protected abstract void DeleteString(string key);
        public abstract void DeleteAll();

        /// <summary>
        /// Saves the specified data under the given key.
        /// </summary>
        public void Save<T>(string key, T data)
        {
            var stored = SerializeData(data);
            SaveString(key, stored);
        }

        /// <summary>   
        /// Tries to load data for the specified key.
        /// </summary>
        public bool TryLoad<T>(string key, out T value)
        {
            if (!TryLoadString(key, out var stored))
            {
                value = default;
                return false;
            }

            try
            {
                value = DeserializeData<T>(stored);
                return true;
            }
            catch
            {
                value = default;
                return false;
            }
        }

        /// <summary>
        /// Deletes data for the specified key.
        /// </summary>
        public void Delete(string key)
        {
            DeleteString(key);
        }

        /// <summary>
        /// Bundles all saved data into a single string.
        /// </summary>
        public string Pack()
        {
            var dict = new Dictionary<string, string>();
            foreach (var key in Keys())
            {
                if (TryLoadString(key, out var stored))
                {
                    dict[key] = stored;
                }
            }

            return SerializeData(dict);
        }

        /// <summary>
        /// Unbundles data from a single string into the save service.
        /// </summary>
        public void Unpack(string packedData, bool overwriteExisting = true)
        {
            if (string.IsNullOrWhiteSpace(packedData))
                return;

            var dict = DeserializeData<Dictionary<string, string>>(packedData);
            if (dict == null)
                return;

            if (overwriteExisting)
            {
                DeleteAll();
            }

            foreach (var kv in dict)
            {
                if (!overwriteExisting && Exists(kv.Key))
                    continue;

                SaveString(kv.Key, kv.Value);
            }
        }

        /// <summary>
        /// Serializes the given data object to a JSON string.
        /// </summary>
        protected string SerializeData(object data)
        {
            var json = JsonConvert.SerializeObject(data, _jsonSettings);

            if (_settings.UseEncryption)
            {
                json = EncryptString(json);
            }

            return json;
        }

        /// <summary>
        /// Deserializes the given JSON string to an object of type T.
        /// </summary>
        protected T DeserializeData<T>(string json)
        {
            if (_settings.UseEncryption)
            {
                json = DecryptString(json);
            }

            return JsonConvert.DeserializeObject<T>(json, _jsonSettings) ?? default;
        }

        /// <summary>
        /// Encrypts the given string using the specified encryption key.
        /// </summary>
        private string EncryptString(string text)
        {
            var key = _settings.EncryptionKey;
            var result = new StringBuilder();

            for (int i = 0; i < text.Length; i++)
            {
                result.Append((char)(text[i] ^ key[i % key.Length]));
            }

            return Convert.ToBase64String(Encoding.UTF8.GetBytes(result.ToString()));
        }

        /// <summary>
        /// Decrypts the given string using the specified encryption key.
        /// </summary>
        private string DecryptString(string encryptedText)
        {
            var key = _settings.EncryptionKey;
            var bytes = Convert.FromBase64String(encryptedText);
            var text = Encoding.UTF8.GetString(bytes);
            var result = new StringBuilder();

            for (int i = 0; i < text.Length; i++)
            {
                result.Append((char)(text[i] ^ key[i % key.Length]));
            }

            return result.ToString();
        }
    }
}
