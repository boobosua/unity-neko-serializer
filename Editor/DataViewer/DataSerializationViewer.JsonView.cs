#if UNITY_EDITOR

using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace NekoSerializer
{
    public partial class DataSerializationViewer
    {
        private void DisplayJsonView()
        {
            if (string.IsNullOrEmpty(rawJsonData))
            {
                if (Application.isPlaying && _warmupJsonRefreshActive)
                    EditorGUILayout.LabelField("Loading save data...", EditorStyles.centeredGreyMiniLabel);
                else
                    EditorGUILayout.LabelField("No save data found.", EditorStyles.centeredGreyMiniLabel);
                return;
            }

            EditorGUILayout.LabelField("Raw JSON Data (Read-only):", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            jsonScrollPosition = EditorGUILayout.BeginScrollView(jsonScrollPosition);

            var textAreaStyle = CreateJsonTextAreaStyle();
            var displayText = GetColorizedJsonForDisplay(rawJsonData);
            var content = new GUIContent(rawJsonData);
            float height = Mathf.Max(textAreaStyle.CalcHeight(content, position.width - 30), 200f);

            EditorGUILayout.SelectableLabel(displayText, textAreaStyle,
                GUILayout.Height(height), GUILayout.ExpandWidth(true));

            EditorGUILayout.EndScrollView();
        }

        private GUIStyle CreateJsonTextAreaStyle()
        {
            return new GUIStyle(EditorStyles.textArea)
            {
                wordWrap = false,
                fontSize = 11,
                richText = true
            };
        }

        private string GetColorizedJsonForDisplay(string json)
        {
            if (string.IsNullOrEmpty(json))
                return json;

            if (_colorizedJsonCacheSource == json && !string.IsNullOrEmpty(_colorizedJsonCache))
                return _colorizedJsonCache;

            _colorizedJsonCacheSource = json;
            _colorizedJsonCache = JsonSyntaxHighlighter.Colorize(json);
            return _colorizedJsonCache;
        }

        private void CopyJsonToClipboard()
        {
            if (string.IsNullOrEmpty(rawJsonData)) return;

            EditorGUIUtility.systemCopyBuffer = rawJsonData;
            Debug.Log("JSON data copied to clipboard!");
            ShowNotification(new GUIContent("JSON copied to clipboard!"));
        }

        private static class JsonSyntaxHighlighter
        {
            private static readonly Color32 KeyColor = new(156, 220, 254, 255);     // VS Code JSON property
            private static readonly Color32 StringColor = new(206, 145, 120, 255);  // VS Code string
            private static readonly Color32 NumberColor = new(181, 206, 168, 255);  // VS Code number
            private static readonly Color32 KeywordColor = new(86, 156, 214, 255);  // VS Code keywords (true/false/null)
            private static readonly Color32 PunctColor = new(212, 212, 212, 255);   // VS Code punctuation

            public static string Colorize(string json)
            {
                string pretty = json;
                try
                {
                    pretty = JToken.Parse(json).ToString(Formatting.Indented);
                }
                catch
                {
                    // If parsing fails, fall back to raw.
                    pretty = json;
                }

                var sb = new StringBuilder(pretty.Length * 2);
                int i = 0;
                while (i < pretty.Length)
                {
                    char c = pretty[i];

                    // Whitespace
                    if (char.IsWhiteSpace(c))
                    {
                        sb.Append(c);
                        i++;
                        continue;
                    }

                    // Strings (keys and values)
                    if (c == '"')
                    {
                        int start = i;
                        i++;
                        bool escaped = false;
                        while (i < pretty.Length)
                        {
                            char ch = pretty[i];
                            if (escaped)
                            {
                                escaped = false;
                                i++;
                                continue;
                            }

                            if (ch == '\\')
                            {
                                escaped = true;
                                i++;
                                continue;
                            }

                            if (ch == '"')
                            {
                                i++; // include closing quote
                                break;
                            }

                            i++;
                        }

                        string token = pretty.Substring(start, i - start);

                        // Determine if this string is a property name (next non-ws char is ':')
                        int j = i;
                        while (j < pretty.Length && char.IsWhiteSpace(pretty[j])) j++;
                        bool isKey = j < pretty.Length && pretty[j] == ':';

                        sb.Append(Color(token, isKey ? KeyColor : StringColor));
                        continue;
                    }

                    // Numbers
                    if (char.IsDigit(c) || c == '-')
                    {
                        int start = i;
                        i++;
                        while (i < pretty.Length)
                        {
                            char ch = pretty[i];
                            if (char.IsDigit(ch) || ch == '.' || ch == 'e' || ch == 'E' || ch == '+' || ch == '-')
                                i++;
                            else
                                break;
                        }

                        string token = pretty.Substring(start, i - start);
                        sb.Append(Color(token, NumberColor));
                        continue;
                    }

                    // Keywords: true/false/null
                    if (char.IsLetter(c))
                    {
                        int start = i;
                        i++;
                        while (i < pretty.Length && char.IsLetter(pretty[i])) i++;
                        string token = pretty.Substring(start, i - start);
                        if (token == "true" || token == "false" || token == "null")
                            sb.Append(Color(token, KeywordColor));
                        else
                            sb.Append(token);
                        continue;
                    }

                    // Punctuation
                    sb.Append(Color(c.ToString(), PunctColor));
                    i++;
                }

                return sb.ToString();
            }

            private static string Color(string text, Color32 color)
            {
                string hex = ColorUtility.ToHtmlStringRGBA(color);
                return $"<color=#{hex}>{text}</color>";
            }
        }
    }
}

#endif
