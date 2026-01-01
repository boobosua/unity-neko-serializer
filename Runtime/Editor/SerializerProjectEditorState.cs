#if UNITY_EDITOR

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace NekoSerializer
{
    internal sealed class SerializerProjectEditorState : ScriptableObject
    {
        internal const string AssetPath = "Assets/Plugins/NekoSerializer/SerializerProjectEditorState.asset";

#if ODIN_INSPECTOR
        [ReadOnly]
#endif
        [SerializeField] private List<string> editorCacheKeys = new();

        // Internal one-shot flag used by the viewer restart flow; never user-editable.
        [SerializeField, HideInInspector] private bool dataViewerShouldEnterPlayMode;

        private static SerializerProjectEditorState s_cached;

        internal static SerializerProjectEditorState GetOrCreate()
        {
            if (s_cached != null)
                return s_cached;

            var asset = AssetDatabase.LoadAssetAtPath<SerializerProjectEditorState>(AssetPath);
            if (asset != null)
            {
                s_cached = asset;
                return asset;
            }

            EnsureFolders();

            asset = CreateInstance<SerializerProjectEditorState>();
            AssetDatabase.CreateAsset(asset, AssetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            s_cached = asset;
            return asset;
        }

        internal bool DataViewerShouldEnterPlayMode
        {
            get => dataViewerShouldEnterPlayMode;
            set
            {
                if (dataViewerShouldEnterPlayMode == value)
                    return;
                dataViewerShouldEnterPlayMode = value;
                EditorUtility.SetDirty(this);
            }
        }

        internal List<string> GetEditorCacheKeysCopy()
        {
            if (editorCacheKeys == null)
                return new List<string>();

            var copy = new List<string>(editorCacheKeys.Count);
            for (int i = 0; i < editorCacheKeys.Count; i++)
            {
                var k = editorCacheKeys[i];
                if (string.IsNullOrWhiteSpace(k))
                    continue;
                if (!copy.Contains(k))
                    copy.Add(k);
            }

            return copy;
        }

        internal void SetEditorCacheKeys(IEnumerable<string> keys)
        {
            editorCacheKeys ??= new List<string>();
            editorCacheKeys.Clear();

            if (keys != null)
            {
                foreach (var k in keys)
                {
                    if (string.IsNullOrWhiteSpace(k))
                        continue;
                    if (!editorCacheKeys.Contains(k))
                        editorCacheKeys.Add(k);
                }
            }

            EditorUtility.SetDirty(this);
        }

        internal void SaveIfDirty()
        {
            if (!EditorUtility.IsDirty(this))
                return;

            AssetDatabase.SaveAssets();
        }

        private static void EnsureFolders()
        {
            EnsureFolder("Assets", "Plugins");
            EnsureFolder("Assets/Plugins", "NekoSerializer");
        }

        private static void EnsureFolder(string parent, string folderName)
        {
            string full = parent.EndsWith("/") ? parent + folderName : parent + "/" + folderName;
            if (AssetDatabase.IsValidFolder(full))
                return;

            AssetDatabase.CreateFolder(parent, folderName);
        }
    }
}

#endif
