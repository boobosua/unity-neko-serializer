#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace NekoSerialize
{
    public class SaveLoadSettingsWindow : EditorWindow
    {
        private SaveLoadSettings _settings;
        private SerializedObject _serializedSettings;
        private Vector2 _scrollPosition;

        private SerializedProperty _saveLocationProp;
        private SerializedProperty _folderNameProp;
        private SerializedProperty _useEncryptionProp;
        private SerializedProperty _encryptionKeyProp;
        private SerializedProperty _prettyPrintJsonProp;

        [MenuItem("Tools/Neko Framework/Save Load Settings")]
        public static void ShowWindow()
        {
            var window = GetWindow<SaveLoadSettingsWindow>("Save Load Settings");
            window.minSize = new Vector2(400, 500);
            window.Show();
        }

        private void OnEnable()
        {
            LoadSettings();
        }

        private void LoadSettings()
        {
            _settings = Resources.Load<SaveLoadSettings>("SaveLoadSettings");

            if (_settings != null)
            {
                _serializedSettings = new SerializedObject(_settings);

                _saveLocationProp = _serializedSettings.FindProperty("<SaveLocation>k__BackingField");
                _folderNameProp = _serializedSettings.FindProperty("<FolderName>k__BackingField");
                _useEncryptionProp = _serializedSettings.FindProperty("<UseEncryption>k__BackingField");
                _encryptionKeyProp = _serializedSettings.FindProperty("<EncryptionKey>k__BackingField");
                _prettyPrintJsonProp = _serializedSettings.FindProperty("<PrettyPrintJson>k__BackingField");
            }
        }

        private void CreateDefaultSettings()
        {
            _settings = CreateInstance<SaveLoadSettings>();

            // Create the directory structure for library use
            string pluginPath = "Assets/Plugins";
            string nekoSerializePath = "Assets/Plugins/NekoSerialize";
            string resourcesPath = "Assets/Plugins/NekoSerialize/Resources";

            if (!AssetDatabase.IsValidFolder(pluginPath))
                AssetDatabase.CreateFolder("Assets", "Plugins");

            if (!AssetDatabase.IsValidFolder(nekoSerializePath))
                AssetDatabase.CreateFolder("Assets/Plugins", "NekoSerialize");

            if (!AssetDatabase.IsValidFolder(resourcesPath))
                AssetDatabase.CreateFolder("Assets/Plugins/NekoSerialize", "Resources");

            // Save the settings asset
            string assetPath = "Assets/Plugins/NekoSerialize/Resources/SaveLoadSettings.asset";
            AssetDatabase.CreateAsset(_settings, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[SaveLoadSettings] Created default settings at: {assetPath}");
        }

        private void ReloadScene()
        {
            if (Application.isPlaying)
            {
                // Removed runtime controls from this window.
            }
        }

        private void OnGUI()
        {
            if (_settings == null || _serializedSettings == null)
            {
                EditorGUILayout.HelpBox("SaveLoadSettings not found. Create settings for this project.", MessageType.Warning);
                EditorGUILayout.Space();

                if (GUILayout.Button("Create New Save Load Settings", GUILayout.Height(30)))
                {
                    CreateDefaultSettings();
                    LoadSettings();
                }
                return;
            }

            var scrollStarted = false;
            try
            {
                _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
                scrollStarted = true;

                EditorGUILayout.Space(5);

                // Settings UI
                _serializedSettings.Update();

                DrawSettingsSection();

                if (_serializedSettings.ApplyModifiedProperties())
                {
                    EditorUtility.SetDirty(_settings);
                    AssetDatabase.SaveAssets();

                    // No runtime service refresh needed (direct-to-storage model)
                }

                // Intentionally no utility/runtime buttons in this window.
            }
            finally
            {
                if (scrollStarted)
                    EditorGUILayout.EndScrollView();
            }
        }

        private void DrawSettingsSection()
        {
            EditorGUILayout.LabelField("Save Settings", EditorStyles.boldLabel);

            if (_saveLocationProp == null || _folderNameProp == null || _useEncryptionProp == null || _encryptionKeyProp == null || _prettyPrintJsonProp == null)
            {
                EditorGUILayout.HelpBox(
                    "SaveLoadSettings fields could not be found. This window is out of sync with SaveLoadSettings.\n" +
                    "Try reimporting scripts or regenerate the SaveLoadSettings asset.",
                    MessageType.Error);
                return;
            }

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
        }

        // Utility/runtime controls intentionally removed.
    }
}
#endif