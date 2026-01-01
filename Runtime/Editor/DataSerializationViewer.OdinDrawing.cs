#if UNITY_EDITOR && ODIN_INSPECTOR

using System;
using UnityEditor;
using UnityEngine;
using Sirenix.OdinInspector.Editor;

namespace NekoSerializer
{
    public partial class DataSerializationViewer
    {
        private interface IOdinValueContainer
        {
            object ValueObj { get; set; }

            string Label { get; set; }
        }

        private sealed class OdinValueContainer<T> : IOdinValueContainer
        {
            [HideInInspector]
            public string Label;

            [Sirenix.OdinInspector.ShowInInspector]
            [Sirenix.OdinInspector.LabelText("@this.Label")]
            public T Value;

            public object ValueObj
            {
                get => Value;
                set => Value = value is T tv ? tv : default;
            }

            string IOdinValueContainer.Label
            {
                get => Label;
                set => Label = value;
            }
        }

        private bool DrawOdinValue(string label, ref object value, string path)
        {
            if (value == null)
            {
                EditorGUILayout.LabelField("null");
                return false;
            }

            var t = value.GetType();
            if (!_odinContainerByPath.TryGetValue(path, out var container) || container == null || container.ValueObj == null || container.ValueObj.GetType() != t)
            {
                var containerType = typeof(OdinValueContainer<>).MakeGenericType(t);
                container = (IOdinValueContainer)Activator.CreateInstance(containerType);
                _odinContainerByPath[path] = container;
                if (_odinTreeByPath.TryGetValue(path, out var oldTree) && oldTree != null)
                {
                    try { oldTree.Dispose(); } catch { }
                }
                _odinTreeByPath.Remove(path);
            }

            container.Label = label;
            container.ValueObj = value;

            if (!_odinTreeByPath.TryGetValue(path, out var tree) || tree == null)
            {
                tree = PropertyTree.Create(container);
                _odinTreeByPath[path] = tree;
            }

            tree.UpdateTree();
            tree.Draw(false);

            // Unity's Begin/EndChangeCheck does not reliably capture Odin collection operations
            // such as drag reordering or removing with the X button.
            tree.InvokeDelayedActions();
            bool changed = tree.ApplyChanges();
            if (changed)
                value = container.ValueObj;
            return changed;
        }
    }
}

#endif
