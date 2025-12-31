using UnityEngine;

namespace NekoSerializer
{
    /// <summary>
    /// Handles saving and loading data using PlayerPrefs.
    /// </summary>
    internal class PlayerPrefsHandler : DataSerializationHandler
    {
        public PlayerPrefsHandler(SerializerSettings settings) : base(settings) { }

        protected override void SaveString(string key, string value)
        {
            PlayerPrefs.SetString(key, value);
            PlayerPrefs.Save();
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
        }

        public override bool Exists(string key)
        {
            return PlayerPrefs.HasKey(key);
        }
    }
}
