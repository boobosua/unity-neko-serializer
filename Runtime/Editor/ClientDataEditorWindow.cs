#if UNITY_EDITOR

using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

namespace NekoSerialize
{
    public class ClientDataEditorWindow : EditorWindow
    {
        private Vector2 scrollPosition;
        private Vector2 jsonScrollPosition;
        private Dictionary<string, object> currentSaveData = new();
        private readonly Dictionary<string, bool> foldoutStates = new();
        private readonly Dictionary<string, bool> dictionaryFoldoutStates = new();

        // Pagination
        private int currentPage = 0;
        private const int itemsPerPage = 10;

        // Tab system
        private int selectedTab = 0;
        private readonly string[] tabs = { "Data View", "JSON View" };
        private string rawJsonData = "";

        [MenuItem("Tools/Neko Framework/Client Data Editor")]
        private static void OpenWindow()
        {
            GetWindow<ClientDataEditorWindow>("Client Data Editor").Show();
        }

        void OnEnable()
        {
            EditorApplication.update += OnEditorUpdate;
            if (Application.isPlaying)
            {
                Refresh();
                RefreshJsonView();
            }
        }

        void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
        }

        void OnEditorUpdate()
        {
            if (!Application.isPlaying)
                return;
            Refresh();
        }

        void OnGUI()
        {
            EditorGUILayout.BeginVertical();
            EditorGUILayout.Space();

            // Always show tabs, but check prerequisites per tab
            DrawTabs();

            if (!CheckViewPrerequisites())
            {
                EditorGUILayout.EndVertical();
                return;
            }

            DrawContent();
            DrawBottomButtons();

            EditorGUILayout.EndVertical();
        }

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
            // Use unified tab bar style
            selectedTab = NekoLib.Core.NekoEditorTabBar.Draw(selectedTab, tabs, 24f);
        }

        // Legacy style method removed; unified tab bar used instead

        private void DrawRedButton(string text, System.Action onClickAction, params GUILayoutOption[] options)
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

            if (GUILayout.Button("Refresh", GUILayout.Height(30)))
            {
                if (selectedTab == 0)
                    Refresh(); // Use service-based refresh for Data View
                else
                    RefreshJsonView(); // Use direct storage refresh for JSON View
            }

            // Delete All only available in play mode for Data View - RED BUTTON
            if (selectedTab == 0 && Application.isPlaying)
            {
                DrawRedButton("Delete All", DeleteAll, GUILayout.Height(30));
            }

            // Copy button available for JSON view when there's data
            if (selectedTab == 1 && !string.IsNullOrEmpty(rawJsonData) && rawJsonData != "{}")
            {
                if (GUILayout.Button("Copy to Clipboard", GUILayout.Height(30)))
                {
                    CopyJsonToClipboard();
                }
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(5);
        }

        private void DisplayDataView()
        {
            if (currentSaveData.Count == 0)
            {
                EditorGUILayout.LabelField("No save data found.", EditorStyles.centeredGreyMiniLabel);
                return;
            }

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            var dataList = new List<KeyValuePair<string, object>>(currentSaveData);
            var paginationInfo = CalculatePagination(dataList.Count);

            DrawPaginationControls(paginationInfo);
            DrawDataItems(dataList, paginationInfo);

            EditorGUILayout.EndScrollView();
        }

        private (int totalPages, int startIndex, int endIndex) CalculatePagination(int totalItems)
        {
            int totalPages = Mathf.CeilToInt((float)totalItems / itemsPerPage);

            // Ensure current page is valid
            if (currentPage >= totalPages && totalPages > 0)
                currentPage = totalPages - 1;
            if (currentPage < 0)
                currentPage = 0;

            int startIndex = currentPage * itemsPerPage;
            int endIndex = Mathf.Min(startIndex + itemsPerPage, totalItems);

            return (totalPages, startIndex, endIndex);
        }

        private void DrawPaginationControls((int totalPages, int startIndex, int endIndex) paginationInfo)
        {
            if (paginationInfo.totalPages <= 1) return;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Page {currentPage + 1} of {paginationInfo.totalPages}", EditorStyles.centeredGreyMiniLabel);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();

            EditorGUI.BeginDisabledGroup(currentPage <= 0);
            if (GUILayout.Button("Previous")) currentPage--;
            EditorGUI.EndDisabledGroup();

            EditorGUI.BeginDisabledGroup(currentPage >= paginationInfo.totalPages - 1);
            if (GUILayout.Button("Next")) currentPage++;
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();
        }

        private void DrawDataItems(List<KeyValuePair<string, object>> dataList, (int totalPages, int startIndex, int endIndex) paginationInfo)
        {
            for (int i = paginationInfo.startIndex; i < paginationInfo.endIndex; i++)
            {
                var kvp = dataList[i];
                if (kvp.Value == null) continue;

                DrawDataItem(kvp);
            }
        }

        private void DrawDataItem(KeyValuePair<string, object> kvp)
        {
            var boxStyle = new GUIStyle("helpBox")
            {
                padding = new RectOffset(8, 8, 8, 8),
                margin = new RectOffset(4, 4, 2, 2)
            };

            EditorGUILayout.BeginVertical(boxStyle);

            if (!foldoutStates.ContainsKey(kvp.Key))
                foldoutStates[kvp.Key] = false;

            foldoutStates[kvp.Key] = EditorGUILayout.Foldout(foldoutStates[kvp.Key], kvp.Key, true);

            if (foldoutStates[kvp.Key])
            {
                EditorGUI.indentLevel++;
                DisplayData(kvp.Value);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(2);
        }

        private void DisplayJsonView()
        {
            if (string.IsNullOrEmpty(rawJsonData))
            {
                EditorGUILayout.LabelField("No save data found.", EditorStyles.centeredGreyMiniLabel);
                return;
            }

            EditorGUILayout.LabelField("Raw JSON Data (Read-only):", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            jsonScrollPosition = EditorGUILayout.BeginScrollView(jsonScrollPosition);

            var textAreaStyle = CreateJsonTextAreaStyle();
            var content = new GUIContent(rawJsonData);
            float height = Mathf.Max(textAreaStyle.CalcHeight(content, position.width - 30), 200f);

            EditorGUILayout.SelectableLabel(rawJsonData, textAreaStyle,
                GUILayout.Height(height), GUILayout.ExpandWidth(true));

            EditorGUILayout.EndScrollView();
        }

        private GUIStyle CreateJsonTextAreaStyle()
        {
            return new GUIStyle(EditorStyles.textArea)
            {
                wordWrap = false,
                fontSize = 11
            };
        }

        private void CopyJsonToClipboard()
        {
            if (string.IsNullOrEmpty(rawJsonData)) return;

            EditorGUIUtility.systemCopyBuffer = rawJsonData;
            Debug.Log("JSON data copied to clipboard!");
            ShowNotification(new GUIContent("JSON copied to clipboard!"));
        }

        private void DisplayData(object obj)
        {
            try
            {
                // Use shared UnityJsonSettings to ensure custom converters & loop handling apply consistently
                var settings = JsonSerializerUtils.GetSettings();
                string json = JsonConvert.SerializeObject(obj, settings);
                var jsonObj = JsonConvert.DeserializeObject<Dictionary<string, object>>(json, settings);

                EditorGUI.indentLevel++;
                foreach (var kvp in jsonObj)
                {
                    string fieldName = ObjectNames.NicifyVariableName(kvp.Key);
                    object value = kvp.Value;

                    if (value == null)
                    {
                        EditorGUILayout.LabelField(fieldName + ":", "null");
                    }
                    else if (value.ToString().Contains("/") && value.ToString().Contains(":"))
                    {
                        // Handle DateTime strings directly
                        EditorGUILayout.LabelField(fieldName + ":", value.ToString());
                    }
                    else if (IsVector3(value))
                    {
                        var vector = ParseVector3(value);
                        EditorGUILayout.LabelField(fieldName + ":", $"({vector.x:F3}, {vector.y:F3}, {vector.z:F3})");
                    }
                    else if (IsVector2(value))
                    {
                        var vector = ParseVector2(value);
                        EditorGUILayout.LabelField(fieldName + ":", $"({vector.x:F3}, {vector.y:F3})");
                    }
                    else if (IsDictionary(value))
                    {
                        if (!IsEmptyDictionary(value))
                        {
                            string dictionaryKey = $"{fieldName}_dict_{EditorGUI.indentLevel}";
                            if (!dictionaryFoldoutStates.ContainsKey(dictionaryKey))
                                dictionaryFoldoutStates[dictionaryKey] = false;

                            dictionaryFoldoutStates[dictionaryKey] = EditorGUILayout.Foldout(
                                dictionaryFoldoutStates[dictionaryKey], fieldName);

                            if (dictionaryFoldoutStates[dictionaryKey])
                            {
                                DisplayDictionary(value);
                            }
                        }
                    }
                    else if (IsArray(value))
                    {
                        EditorGUILayout.LabelField(fieldName + ":", EditorStyles.boldLabel);
                        DisplayArray(value);
                    }
                    else
                    {
                        EditorGUILayout.LabelField(fieldName + ":", value.ToString());
                    }
                }
                EditorGUI.indentLevel--;
            }
            catch (System.Exception)
            {
                // If JSON fails, try to determine the type and show meaningful information
                EditorGUI.indentLevel++;
                string typeName = GetReadableTypeName(obj);
                string valueDisplay = GetReadableValueDisplay(obj);
                EditorGUILayout.LabelField($"{typeName}:", valueDisplay);
                EditorGUI.indentLevel--;
            }
        }

        private string GetReadableTypeName(object obj)
        {
            if (obj == null) return "Null";

            System.Type type = obj.GetType();

            // Handle common Unity types
            if (type == typeof(Vector2)) return "Vector2";
            if (type == typeof(Vector3)) return "Vector3";
            if (type == typeof(Vector4)) return "Vector4";
            if (type == typeof(Quaternion)) return "Quaternion";
            if (type == typeof(Color)) return "Color";
            if (type == typeof(Color32)) return "Color32";

            // Handle primitive types
            if (type == typeof(int)) return "Integer";
            if (type == typeof(float)) return "Float";
            if (type == typeof(double)) return "Double";
            if (type == typeof(bool)) return "Boolean";
            if (type == typeof(string)) return "String";
            if (type == typeof(System.DateTime)) return "DateTime";

            // Handle collections
            if (type.IsArray) return $"{GetElementTypeName(type.GetElementType())} Array";
            if (type.IsGenericType)
            {
                var genericDef = type.GetGenericTypeDefinition();
                if (genericDef == typeof(List<>))
                    return $"{GetElementTypeName(type.GetGenericArguments()[0])} List";
                if (genericDef == typeof(Dictionary<,>))
                {
                    var args = type.GetGenericArguments();
                    return $"Dictionary<{GetElementTypeName(args[0])}, {GetElementTypeName(args[1])}>";
                }
            }

            // Check if it's a Newtonsoft.Json.Linq object (from deserialized JSON)
            if (type.Namespace == "Newtonsoft.Json.Linq")
            {
                if (type.Name == "JObject") return "Object";
                if (type.Name == "JArray") return "Array";
                if (type.Name == "JValue") return "Value";
            }

            // For custom classes, use the class name
            return ObjectNames.NicifyVariableName(type.Name);
        }

        private string GetElementTypeName(System.Type elementType)
        {
            if (elementType == null) return "Unknown";
            if (elementType == typeof(int)) return "int";
            if (elementType == typeof(float)) return "float";
            if (elementType == typeof(string)) return "string";
            if (elementType == typeof(bool)) return "bool";
            return ObjectNames.NicifyVariableName(elementType.Name);
        }

        private string GetReadableValueDisplay(object obj)
        {
            if (obj == null) return "null";

            System.Type type = obj.GetType();

            // Handle Unity types with special formatting
            if (type == typeof(Vector2))
            {
                var v = (Vector2)obj;
                return $"({v.x:F3}, {v.y:F3})";
            }
            if (type == typeof(Vector3))
            {
                var v = (Vector3)obj;
                return $"({v.x:F3}, {v.y:F3}, {v.z:F3})";
            }
            if (type == typeof(Vector4))
            {
                var v = (Vector4)obj;
                return $"({v.x:F3}, {v.y:F3}, {v.z:F3}, {v.w:F3})";
            }
            if (type == typeof(Quaternion))
            {
                var q = (Quaternion)obj;
                return $"({q.x:F3}, {q.y:F3}, {q.z:F3}, {q.w:F3})";
            }
            if (type == typeof(Color))
            {
                var c = (Color)obj;
                return $"RGBA({c.r:F3}, {c.g:F3}, {c.b:F3}, {c.a:F3})";
            }

            // Handle collections with count info
            if (obj is System.Collections.ICollection collection)
                return $"[{collection.Count} items]";

            // For everything else, use ToString but limit length
            string str = obj.ToString();
            return str.Length > 100 ? str.Substring(0, 97) + "..." : str;
        }

        private bool IsVector2(object value)
        {
            if (value is Vector2) return true;
            if (value is Newtonsoft.Json.Linq.JObject jobj)
                return jobj.ContainsKey("x") && jobj.ContainsKey("y") && !jobj.ContainsKey("z");
            return false;
        }

        private Vector2 ParseVector2(object value)
        {
            if (value is Vector2 vector2) return vector2;
            if (value is Newtonsoft.Json.Linq.JObject jobj)
            {
                float x = jobj["x"]?.ToObject<float>() ?? 0f;
                float y = jobj["y"]?.ToObject<float>() ?? 0f;
                return new Vector2(x, y);
            }
            return Vector2.zero;
        }

        private bool IsVector3(object value)
        {
            if (value is Vector3) return true;
            if (value is Newtonsoft.Json.Linq.JObject jobj)
                return jobj.ContainsKey("x") && jobj.ContainsKey("y") && jobj.ContainsKey("z") && !jobj.ContainsKey("w");
            return false;
        }

        private Vector3 ParseVector3(object value)
        {
            if (value is Vector3 vector3) return vector3;
            if (value is Newtonsoft.Json.Linq.JObject jobj)
            {
                float x = jobj["x"]?.ToObject<float>() ?? 0f;
                float y = jobj["y"]?.ToObject<float>() ?? 0f;
                float z = jobj["z"]?.ToObject<float>() ?? 0f;
                return new Vector3(x, y, z);
            }
            return Vector3.zero;
        }

        private bool IsDictionary(object value)
        {
            return value is System.Collections.IDictionary ||
                   value is Newtonsoft.Json.Linq.JObject;
        }

        private bool IsArray(object value)
        {
            if (value is System.Array) return true;
            if (value is System.Collections.IList) return true;
            if (value is Newtonsoft.Json.Linq.JArray) return true;
            return false;
        }

        private bool IsEmptyDictionary(object value)
        {
            try
            {
                if (value is Newtonsoft.Json.Linq.JObject jobj)
                {
                    return jobj.Count == 0;
                }
                else if (value is System.Collections.IDictionary dict)
                {
                    return dict.Count == 0;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        private void DisplayDictionary(object value)
        {
            EditorGUI.indentLevel++;
            try
            {
                if (value is Newtonsoft.Json.Linq.JObject jobj)
                {
                    foreach (var kvp in jobj)
                    {
                        EditorGUILayout.LabelField(kvp.Key + ":", kvp.Value?.ToString() ?? "null");
                    }
                }
                else if (value is System.Collections.IDictionary dict)
                {
                    foreach (System.Collections.DictionaryEntry entry in dict)
                    {
                        EditorGUILayout.LabelField((entry.Key?.ToString() ?? "null") + ":", entry.Value?.ToString() ?? "null");
                    }
                }
            }
            catch (System.Exception e)
            {
                EditorGUILayout.LabelField("Error displaying dictionary:", e.Message);
            }
            EditorGUI.indentLevel--;
        }

        private void DisplayArray(object value)
        {
            EditorGUI.indentLevel++;
            try
            {
                if (value is Newtonsoft.Json.Linq.JArray jarray)
                {
                    for (int i = 0; i < jarray.Count; i++)
                    {
                        EditorGUILayout.LabelField($"Element {i}", jarray[i]?.ToString() ?? "null");
                    }
                }
                else if (value is System.Collections.IList list)
                {
                    for (int i = 0; i < list.Count; i++)
                    {
                        EditorGUILayout.LabelField($"Element {i}", list[i]?.ToString() ?? "null");
                    }
                }
                else if (value is System.Array array)
                {
                    for (int i = 0; i < array.Length; i++)
                    {
                        EditorGUILayout.LabelField($"Element {i}", array.GetValue(i)?.ToString() ?? "null");
                    }
                }
            }
            catch (System.Exception e)
            {
                EditorGUILayout.LabelField("Error displaying array: " + e.Message);
            }
            EditorGUI.indentLevel--;
        }

        public void Refresh()
        {
            if (!Application.isPlaying)
                return;

            currentSaveData = GetDirectStorageData();
            UpdateRawJsonData();
            Repaint();
        }

        private void RefreshJsonView()
        {
            if (!Application.isPlaying)
                return;

            LoadDataDirectlyFromStorage();
            Repaint();
        }

        private void LoadDataDirectlyFromStorage()
        {
            try
            {
                var directData = GetDirectStorageData();
                if (directData.Count > 0)
                {
                    var settings = JsonSerializerUtils.GetSettings();
                    rawJsonData = JsonConvert.SerializeObject(directData, settings);
                }
                else
                {
                    rawJsonData = "{}";
                }
            }
            catch (System.Exception e)
            {
                rawJsonData = $"Error loading data directly from storage:\n{e.Message}";
            }
        }

        private Dictionary<string, object> GetDirectStorageData()
        {
            // In play mode, SaveLoadService warms and maintains an editor cache.
            // Use it for listing/inspection (especially for PlayerPrefs where keys can't be enumerated).
            return SaveLoadService.GetAllSaveDataCopy();
        }

        private void UpdateRawJsonData()
        {
            try
            {
                var settings = JsonSerializerUtils.GetSettings();
                rawJsonData = JsonConvert.SerializeObject(currentSaveData, settings);
            }
            catch (System.Exception e)
            {
                rawJsonData = $"Error serializing data to JSON:\n{e.Message}\n\nRaw data:\n{currentSaveData}";
            }
        }

        public void DeleteAll()
        {
            if (!Application.isPlaying)
                return;

            const string title = "Delete All";
            const string message = "Delete all save data and restart the game?";
            const string ok = "Delete & Restart";
            const string cancel = "Cancel";

            if (EditorUtility.DisplayDialog(title, message, ok, cancel))
            {
                // Delete all keys tracked by the library (from SaveLoadService editor cache).
                var keys = new List<string>(SaveLoadService.GetAllSaveData().Keys);
                foreach (var key in keys)
                {
                    SaveLoadService.DeleteData(key);
                }

                RestartGame();
            }
        }

        private void RestartGame()
        {
            // Store flag in EditorPrefs to survive domain reload
            EditorPrefs.SetBool("SaveLoadStorage_ShouldEnterPlayMode", true);

            // Use EditorApplication.delayCall to ensure the current frame completes before restarting
            EditorApplication.delayCall += () =>
            {
                // First, exit play mode
                EditorApplication.isPlaying = false;

                // Wait for play mode to fully exit, then reload domain
                EditorApplication.delayCall += () =>
                {
                    // Force domain reload
                    EditorUtility.RequestScriptReload();
                };
            };
        }

        // Static constructor to handle post-domain-reload logic
        static ClientDataEditorWindow()
        {
            // Check if we should enter play mode after domain reload
            EditorApplication.delayCall += CheckAndEnterPlayMode;
        }

        private static void CheckAndEnterPlayMode()
        {
            if (EditorPrefs.GetBool("SaveLoadStorage_ShouldEnterPlayMode", false))
            {
                EditorPrefs.DeleteKey("SaveLoadStorage_ShouldEnterPlayMode");

                // Wait a bit more to ensure everything is fully loaded
                EditorApplication.delayCall += () =>
                {
                    EditorApplication.isPlaying = true;
                };
            }
        }
    }
}

#endif