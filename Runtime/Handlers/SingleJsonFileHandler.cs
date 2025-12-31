using System.IO;
using UnityEngine;

namespace NekoSerializer
{
    /// <summary>
    /// Handles saving and loading data to a single JSON file.
    /// </summary>
    internal class SingleJsonFileHandler : DataSerializationHandler
    {
        public SingleJsonFileHandler(SerializerSettings settings) : base(settings) { }

        private string SaveDirectory => Path.Combine(Application.persistentDataPath, _settings.SaveDirectory);

        private string GetFilePath(string key)
        {
            return Path.Combine(SaveDirectory, $"{key}.json");
        }

        protected override void SaveString(string key, string value)
        {
            Directory.CreateDirectory(SaveDirectory);
            File.WriteAllText(GetFilePath(key), value);
        }

        protected override bool TryLoadString(string key, out string value)
        {
            var path = GetFilePath(key);
            if (!File.Exists(path))
            {
                value = default;
                return false;
            }

            value = File.ReadAllText(path);
            return true;
        }

        protected override void DeleteString(string key)
        {
            var path = GetFilePath(key);
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        public override bool Exists(string key)
        {
            return File.Exists(GetFilePath(key));
        }
    }
}
