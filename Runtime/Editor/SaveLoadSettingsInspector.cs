#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace NekoSerialize
{
    [CustomEditor(typeof(SaveLoadSettings))]
    public class SaveLoadSettingsInspector : Editor
    {
        private SerializedProperty _saveLocationProp;
        private SerializedProperty _folderNameProp;
        private SerializedProperty _useEncryptionProp;
        private SerializedProperty _encryptionKeyProp;
        private SerializedProperty _prettyPrintJsonProp;

        private void OnEnable()
        {
            _saveLocationProp = serializedObject.FindProperty("<SaveLocation>k__BackingField");
            _folderNameProp = serializedObject.FindProperty("<FolderName>k__BackingField");
            _useEncryptionProp = serializedObject.FindProperty("<UseEncryption>k__BackingField");
            _encryptionKeyProp = serializedObject.FindProperty("<EncryptionKey>k__BackingField");
            _prettyPrintJsonProp = serializedObject.FindProperty("<PrettyPrintJson>k__BackingField");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("Save Settings", EditorStyles.boldLabel);

            // Save Location
            EditorGUILayout.PropertyField(_saveLocationProp, new GUIContent("Save Location"));

            var saveLocation = (SaveLocation)_saveLocationProp.enumValueIndex;

            if (saveLocation == SaveLocation.JsonFile)
            {
                EditorGUILayout.PropertyField(_folderNameProp, new GUIContent("Folder Name"));
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Security", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_useEncryptionProp, new GUIContent("Use Encryption"));

            if (_useEncryptionProp.boolValue)
            {
                EditorGUILayout.PropertyField(_encryptionKeyProp, new GUIContent("Encryption Key"));
                EditorGUILayout.HelpBox("Keep your encryption key secure! Consider using environment variables in production.", MessageType.Info);
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Formatting", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_prettyPrintJsonProp, new GUIContent("Pretty Print JSON"));

            // Show hints based on save location
            EditorGUILayout.Space();
            string hint = saveLocation switch
            {
                SaveLocation.PlayerPrefs => "PlayerPrefs: Values are saved per key.",
                SaveLocation.JsonFile => "JSON File: Values are saved per key as separate files in the folder.",
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