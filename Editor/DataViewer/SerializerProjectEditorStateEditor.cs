#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

#if ODIN_INSPECTOR
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
#endif

namespace NekoSerializer
{
#if ODIN_INSPECTOR
    [CustomEditor(typeof(SerializerProjectEditorState))]
    internal sealed class SerializerProjectEditorStateEditor : OdinEditor
    {
        private PropertyTree _tree;

        protected override void OnEnable()
        {
            base.OnEnable();
            RebuildTree();
        }

        protected override void OnDisable()
        {
            try { _tree?.Dispose(); } catch { }
            _tree = null;
            base.OnDisable();
        }

        private void RebuildTree()
        {
            try { _tree?.Dispose(); } catch { }
            _tree = null;

            // Draw with Odin styling (lists, groups, etc.).
            // PropertyTree is reflection-based, so it won't show Unity's m_Script field.
            _tree = PropertyTree.Create(target);
        }

        public override void OnInspectorGUI()
        {
            using (new EditorGUI.DisabledScope(true))
            {
                if (_tree == null)
                    RebuildTree();

                _tree.UpdateTree();
                _tree.Draw(false);
                _tree.InvokeDelayedActions();
                _tree.ApplyChanges();
            }
        }
    }
#else
	[CustomEditor(typeof(SerializerProjectEditorState))]
	internal sealed class SerializerProjectEditorStateEditor : Editor
	{
		public override void OnInspectorGUI()
		{
			serializedObject.Update();

			using (new EditorGUI.DisabledScope(true))
			{
				DrawPropertiesExcluding(serializedObject, "m_Script");
			}

			serializedObject.ApplyModifiedPropertiesWithoutUndo();
		}
	}
#endif
}

#endif
