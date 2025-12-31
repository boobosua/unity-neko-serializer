#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace NekoSerializer
{
    [CustomEditor(typeof(SerializerSettings))]
    public class SerializerSettingsInspector : Editor
    {
        private SerializedProperty _storageOptionProp;
        private SerializedProperty _saveDirectoryProp;
        private SerializedProperty _useEncryptionProp;
        private SerializedProperty _encryptionKeyProp;
        private SerializedProperty _prettyPrintJsonProp;

        private void OnEnable()
        {
            _storageOptionProp = serializedObject.FindProperty("<StorageOption>k__BackingField");
            _saveDirectoryProp = serializedObject.FindProperty("<SaveDirectory>k__BackingField");
            _useEncryptionProp = serializedObject.FindProperty("<UseEncryption>k__BackingField");
            _encryptionKeyProp = serializedObject.FindProperty("<EncryptionKey>k__BackingField");
            _prettyPrintJsonProp = serializedObject.FindProperty("<PrettyPrintJson>k__BackingField");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // EditorGUILayout.LabelField("Serializer Settings", EditorStyles.boldLabel);

            // Save Location
            EditorGUILayout.PropertyField(_storageOptionProp, new GUIContent("Storage Option"));

            var saveLocation = (StorageOption)_storageOptionProp.enumValueIndex;

            if (saveLocation == StorageOption.JsonFile)
            {
                EditorGUILayout.PropertyField(_saveDirectoryProp, new GUIContent("Save Directory"));
            }

            // EditorGUILayout.Space();
            // EditorGUILayout.LabelField("Security", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_useEncryptionProp, new GUIContent("Use Encryption"));

            if (_useEncryptionProp.boolValue)
            {
                EditorGUILayout.PropertyField(_encryptionKeyProp, new GUIContent("Encryption Key"));
                EditorGUILayout.HelpBox("Keep your encryption key secure! Consider using environment variables in production.", MessageType.Info);
            }

            // EditorGUILayout.Space();
            // EditorGUILayout.LabelField("Formatting", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_prettyPrintJsonProp, new GUIContent("Pretty Print JSON"));

            // Show hints based on save location
            // EditorGUILayout.Space();
            string hint = saveLocation switch
            {
                StorageOption.PlayerPrefs => "PlayerPrefs: Values are saved per key.",
                StorageOption.JsonFile => "JSON File: Values are saved per key as separate files in the save directory.",
                _ => ""
            };

            if (!string.IsNullOrEmpty(hint))
            {
                EditorGUILayout.HelpBox(hint, MessageType.Info);
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif