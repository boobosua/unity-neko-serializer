using UnityEngine;

namespace NekoSerializer
{
    [CreateAssetMenu(fileName = "SerializerSettings", menuName = "Neko Framework/Serialize/Serializer Settings")]
    public class SerializerSettings : ScriptableObject
    {
        [Header("Save Settings")]
        [field: SerializeField, Tooltip("The location where save data will be stored.")]
        public StorageOption StorageOption { get; private set; } = StorageOption.PlayerPrefs;

        [field: SerializeField, Tooltip("The name of the folder to save data to.")]
        public string SaveDirectory { get; private set; } = "SaveData";

        [Header("Security")]
        [field: SerializeField, Tooltip("Whether to use encryption for save data.")]
        public bool UseEncryption { get; private set; } = false;

        [field: SerializeField, Tooltip("The encryption key used to secure save data.")]
        public string EncryptionKey { get; private set; } = "DefaultEncryptionKey";

        [Header("Formatting")]
        [field: SerializeField, Tooltip("Whether to pretty print JSON data.")]
        public bool PrettyPrintJson { get; private set; } = true;
    }
}
