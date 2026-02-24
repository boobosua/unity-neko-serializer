#if UNITY_EDITOR

using System;
using UnityEditor;
using UnityEngine;

namespace NekoSerializer
{
#if ODIN_INSPECTOR
    using Sirenix.Utilities.Editor;
#endif

    public partial class DataSerializationViewer
    {
        private bool CheckViewPrerequisites()
        {
            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Enter Play Mode to view and manage save data.", MessageType.Info);
                return false;
            }

            return true;
        }

        private void DrawTabs()
        {
#if ODIN_INSPECTOR
            // Odin toolbar tabs (GroupTab-like styling)
            SirenixEditorGUI.BeginHorizontalToolbar(24f, 0);
            {
                for (int i = 0; i < tabs.Length; i++)
                {
                    bool isActive = selectedTab == i;
                    bool nowActive = SirenixEditorGUI.ToolbarTab(isActive, tabs[i]);
                    if (nowActive && !isActive)
                    {
                        selectedTab = i;
                        if (Application.isPlaying)
                        {
                            if (selectedTab == 0) Refresh();
                            else RefreshJsonView();
                        }
                    }
                }
            }
            SirenixEditorGUI.EndHorizontalToolbar();
#else
            // Use unified tab bar style
            int next = NekoLib.Core.NekoEditorTabBar.Draw(selectedTab, tabs, 24f);
            if (next != selectedTab)
            {
                selectedTab = next;
                if (Application.isPlaying)
                {
                    if (selectedTab == 0) Refresh();
                    else RefreshJsonView();
                }
            }
#endif
        }

        private void DrawRedButton(string text, Action onClickAction, params GUILayoutOption[] options)
        {
            // Store original background color
            Color originalBgColor = GUI.backgroundColor;

            // Set lighter, more noticeable red background color
            GUI.backgroundColor = new Color(1.0f, 0.4f, 0.4f, 1f);

            // Create style with white text
            var redStyle = new GUIStyle(GUI.skin.button);
            redStyle.fontSize = 12;
            redStyle.fontStyle = FontStyle.Bold;
            redStyle.normal.textColor = Color.white;
            redStyle.hover.textColor = Color.white;
            redStyle.active.textColor = Color.white;

            if (GUILayout.Button(text, redStyle, options))
            {
                onClickAction?.Invoke();
            }

            // Restore original background color
            GUI.backgroundColor = originalBgColor;
        }

        private void DrawContent()
        {
            EditorGUILayout.BeginVertical();

            if (selectedTab == 0)
            {
                DisplayDataView();
            }
            else
            {
                DisplayJsonView();
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawBottomButtons()
        {
            GUILayout.FlexibleSpace();
            EditorGUILayout.Space(5);

            // Separator line
            var rect = GUILayoutUtility.GetRect(0, 1);
            EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.3f));
            EditorGUILayout.Space(5);

            EditorGUILayout.BeginHorizontal();

#if ODIN_INSPECTOR
            if (SirenixEditorGUI.Button("Refresh", Sirenix.OdinInspector.ButtonSizes.Large))
#else
            if (GUILayout.Button("Refresh", GUILayout.Height(30)))
#endif
            {
                if (selectedTab == 0)
                    Refresh();
                else
                    RefreshJsonView();
            }

            if (selectedTab == 0 && Application.isPlaying)
            {
#if ODIN_INSPECTOR
                EditorGUI.BeginDisabledGroup(!HasStagedChanges());
                if (SirenixEditorGUI.Button("Save & Restart", Sirenix.OdinInspector.ButtonSizes.Large))
#else
                EditorGUI.BeginDisabledGroup(!HasStagedChanges());
                if (GUILayout.Button("Save & Restart", GUILayout.Height(30)))
#endif
                {
                    SaveAndRestart();
                }

                EditorGUI.EndDisabledGroup();
            }

            // Delete All only available in play mode for Data View - RED BUTTON
            if (selectedTab == 0 && Application.isPlaying)
            {
#if ODIN_INSPECTOR
                var prevBg = GUI.backgroundColor;
                GUI.backgroundColor = new Color(1.0f, 0.4f, 0.4f, 1f);
                if (SirenixEditorGUI.Button("Delete All", Sirenix.OdinInspector.ButtonSizes.Large))
                    DeleteAll();
                GUI.backgroundColor = prevBg;
#else
                DrawRedButton("Delete All", DeleteAll, GUILayout.Height(30));
#endif
            }

            // Copy button available for JSON view when there's data
            if (selectedTab == 1 && !string.IsNullOrEmpty(rawJsonData) && rawJsonData != "{}")
            {
#if ODIN_INSPECTOR
                if (SirenixEditorGUI.Button("Copy to Clipboard", Sirenix.OdinInspector.ButtonSizes.Large))
#else
                if (GUILayout.Button("Copy to Clipboard", GUILayout.Height(30)))
#endif
                {
                    CopyJsonToClipboard();
                }
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(5);
        }

        private void SaveAndRestart()
        {
            if (!Application.isPlaying)
                return;

            const string title = "Save";
            const string message = "Save staged changes and restart Play Mode?\n\nChanges are only written when you press Save.";
            if (!EditorUtility.DisplayDialog(title, message, "Save & Restart", "Cancel"))
                return;

            CommitStagedChangesToStorage();
            RestartGame();
        }
    }
}

#endif
