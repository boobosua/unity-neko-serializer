#if UNITY_EDITOR && UNITY_6000_0_OR_NEWER

using UnityEngine.UIElements;

namespace NekoSerializer
{
    public partial class DataSerializationViewer
    {
        private void CreateGUI()
        {
            _useUIToolkitHost = true;

            rootVisualElement.Clear();

            var imgui = new IMGUIContainer(DrawIMGUIRoot)
            {
                style =
                {
                    flexGrow = 1
                }
            };

            rootVisualElement.Add(imgui);
        }
    }
}

#endif
