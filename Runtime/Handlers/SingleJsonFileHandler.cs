using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace NekoSerialize
{
    /// <summary>
    /// Handles saving and loading data to a single JSON file.
    /// </summary>
    public class SingleJsonFileHandler : SaveDataHandler
    {
        public SingleJsonFileHandler(SaveLoadSettings settings) : base(settings) { }

        private string SaveDirectory => Path.Combine(Application.persistentDataPath, _settings.FolderName);

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

        public override IEnumerable<string> Keys()
        {
            if (!Directory.Exists(SaveDirectory))
                return Array.Empty<string>();

            var files = Directory.EnumerateFiles(SaveDirectory, "*.json", SearchOption.TopDirectoryOnly);
            return files.Select(file => Path.GetFileNameWithoutExtension(file));
        }

        public override bool Exists(string key)
        {
            return File.Exists(GetFilePath(key));
        }

        public override void DeleteAll()
        {
            if (!Directory.Exists(SaveDirectory))
                return;

            foreach (var file in Directory.EnumerateFiles(SaveDirectory, "*.json", SearchOption.TopDirectoryOnly))
            {
                File.Delete(file);
            }
        }
    }
}
