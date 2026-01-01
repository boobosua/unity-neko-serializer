#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace NekoSerializer
{
#if ODIN_INSPECTOR
    using Sirenix.Utilities.Editor;
#endif

    public partial class DataSerializationViewer
    {
        private void DisplayDataView()
        {
            if (stagedSaveData.Count == 0)
            {
                if (Application.isPlaying && _warmupDataRefreshActive)
                    EditorGUILayout.LabelField("Loading save data...", EditorStyles.centeredGreyMiniLabel);
                else
                    EditorGUILayout.LabelField("No save data found.", EditorStyles.centeredGreyMiniLabel);
                return;
            }

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            var keys = GetDataViewRootKeysForDisplay();
            var paginationInfo = CalculatePagination(keys.Count);
            DrawPaginationControls(paginationInfo);
            DrawDataItems(keys, paginationInfo);

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

        private void DrawDataItems(IReadOnlyList<string> keys, (int totalPages, int startIndex, int endIndex) paginationInfo)
        {
            for (int i = paginationInfo.startIndex; i < paginationInfo.endIndex; i++)
            {
                string key = keys[i];
                if (!stagedSaveData.TryGetValue(key, out var value) || value == null)
                    continue;

                DrawDataItem(new KeyValuePair<string, object>(key, value));
            }
        }

        private void DrawDataItem(KeyValuePair<string, object> kvp)
        {
#if ODIN_INSPECTOR
            // In Odin mode, show native collection drawers for the entire value.
            object odinRootValue = kvp.Value;
            if (odinRootValue is JValue jv)
                odinRootValue = jv.Value;

            bool rootWasJToken = false;
            Func<object, object> rootConvertBack = null;

            if (odinRootValue is JArray || odinRootValue is JObject)
            {
                object originalToken = odinRootValue;
                if (_odinRootConversionByKey.TryGetValue(kvp.Key, out var cached) && ReferenceEquals(cached.Source, odinRootValue) && cached.Converted != null)
                {
                    rootWasJToken = true;
                    rootConvertBack = cached.ConvertBack;
                    odinRootValue = cached.Converted;
                }
                else if (TryConvertJTokenRootToOdinValue(odinRootValue, out var convertedRoot, out var convertBack))
                {
                    rootWasJToken = true;
                    rootConvertBack = convertBack;
                    odinRootValue = convertedRoot;

                    _odinRootConversionByKey[kvp.Key] = new OdinRootConversionCacheEntry
                    {
                        Source = originalToken,
                        Converted = convertedRoot,
                        ConvertBack = convertBack
                    };
                }
            }

            if (IsOdinNativeCollection(odinRootValue))
            {
                object updated = odinRootValue;
                string beforeSnapshot = null;
                if (!rootWasJToken)
                {
                    try
                    {
                        beforeSnapshot = JsonConvert.SerializeObject(odinRootValue, JsonSerializerUtils.GetSettings());
                    }
                    catch
                    {
                        beforeSnapshot = null;
                    }
                }
                bool drawChanged = DrawEditableAny(ObjectNames.NicifyVariableName(kvp.Key), ref updated, kvp.Key);

                if (rootWasJToken && rootConvertBack != null)
                {
                    // Robust change detection: compare token values.
                    // This avoids missing edits when Odin applies changes internally during Draw.
                    var beforeToken = kvp.Value as JToken;
                    var afterObj = rootConvertBack(updated);
                    var afterToken = afterObj as JToken;

                    bool changed = beforeToken != null && afterToken != null
                        ? !JToken.DeepEquals(beforeToken, afterToken)
                        : !Equals(kvp.Value, afterObj);

                    if (changed)
                    {
                        stagedSaveData[kvp.Key] = afterObj;
                        MarkRootDirty(kvp.Key);
                    }
                }
                else
                {
                    // Non-JToken roots use normal change reporting.
                    bool changed = drawChanged;
                    if (!changed && beforeSnapshot != null)
                    {
                        try
                        {
                            string afterSnapshot = JsonConvert.SerializeObject(updated, JsonSerializerUtils.GetSettings());
                            changed = !string.Equals(beforeSnapshot, afterSnapshot, StringComparison.Ordinal);
                        }
                        catch
                        {
                            // If we can't serialize, fall back to the draw flag.
                        }
                    }

                    if (changed)
                    {
                        stagedSaveData[kvp.Key] = updated;
                        MarkRootDirty(kvp.Key);
                    }
                }
                return;
            }
#endif

            // If this key holds a single value (int/float/string/etc.), show it as a normal inspector field (no foldout).
            if (IsSingleValue(kvp.Value))
            {
                object updated = kvp.Value;
                bool changed = DrawEditableAny(ObjectNames.NicifyVariableName(kvp.Key), ref updated, kvp.Key);
                if (changed)
                {
                    stagedSaveData[kvp.Key] = updated;
                    MarkRootDirty(kvp.Key);
                }
                return;
            }

#if ODIN_INSPECTOR
            SirenixEditorGUI.BeginBox();
#else
            var boxStyle = new GUIStyle("helpBox")
            {
                padding = new RectOffset(8, 8, 8, 8),
                margin = new RectOffset(4, 4, 2, 2)
            };
            EditorGUILayout.BeginVertical(boxStyle);
#endif

            if (!foldoutStates.ContainsKey(kvp.Key))
                foldoutStates[kvp.Key] = false;

            // Custom foldout header for collections: show item count + add button.
            if (TryGetCollectionCount(kvp.Value, out int count))
            {
                bool addClicked;
                DrawRootCollectionHeaderRow(kvp.Key, count, out addClicked);
                if (addClicked)
                {
                    object updated = kvp.Value;
                    if (TryAddCollectionItem(ref updated, kvp.Key))
                    {
                        stagedSaveData[kvp.Key] = updated;
                        MarkRootDirty(kvp.Key);
                    }
                }
            }
            else
            {
#if ODIN_INSPECTOR
                foldoutStates[kvp.Key] = SirenixEditorGUI.Foldout(foldoutStates[kvp.Key], ObjectNames.NicifyVariableName(kvp.Key), EditorStyles.foldout);
#else
                foldoutStates[kvp.Key] = EditorGUILayout.Foldout(foldoutStates[kvp.Key], ObjectNames.NicifyVariableName(kvp.Key), true);
#endif
            }

            if (foldoutStates[kvp.Key])
            {
                EditorGUI.indentLevel++;
                DisplayData(kvp.Key, kvp.Value);
                EditorGUI.indentLevel--;
            }

#if ODIN_INSPECTOR
            SirenixEditorGUI.EndBox();
#else
            EditorGUILayout.EndVertical();
#endif
            EditorGUILayout.Space(2);
        }

        private void DrawRootCollectionHeaderRow(string key, int count, out bool addClicked)
        {
            addClicked = false;

            var labelContent = new GUIContent(ObjectNames.NicifyVariableName(key));
            int totalPages = Mathf.Max(1, Mathf.CeilToInt(count / (float)CollectionItemsPerPage));
            int page = GetCollectionPage(key);
            if (page >= totalPages) page = totalPages - 1;
            if (page < 0) page = 0;
            _collectionPageByPath[key] = page;

            Rect rowRect = GUILayoutUtility.GetRect(1f, EditorGUIUtility.singleLineHeight + 2f, GUILayout.ExpandWidth(true));
            rowRect.y += 1f;
            rowRect.height = EditorGUIUtility.singleLineHeight;
            rowRect.xMin += 2f;
            rowRect.xMax -= 2f;

            const float gap = 6f;
            const float prevW = 50f;
            const float nextW = 50f;
            const float plusW = 26f;

            var pageContent = new GUIContent($"Page {page + 1}/{totalPages}");
            float pageW = Mathf.Ceil(EditorStyles.miniLabel.CalcSize(pageContent).x) + 10f;
            float centerW = prevW + gap + pageW + gap + nextW;

            var countContent = new GUIContent($"{count} items");
            float countW = Mathf.Ceil(EditorStyles.miniLabel.CalcSize(countContent).x);
            float rightW = countW + gap + plusW;

            Rect rightRect = new Rect(rowRect.xMax - rightW, rowRect.y, rightW, rowRect.height);
            Rect centerRect = new Rect(rowRect.center.x - (centerW * 0.5f), rowRect.y, centerW, rowRect.height);

            // Keep the center controls from overlapping the right group.
            float maxCenterX = rightRect.xMin - gap - centerRect.width;
            if (centerRect.x > maxCenterX)
                centerRect.x = maxCenterX;
            if (centerRect.x < rowRect.xMin)
                centerRect.x = rowRect.xMin;

            float leftMaxX = Mathf.Min(centerRect.xMin - gap, rightRect.xMin - gap);
            Rect leftRect = new Rect(rowRect.xMin, rowRect.y, Mathf.Max(0f, leftMaxX - rowRect.xMin), rowRect.height);

            // Left: foldout
            if (!foldoutStates.TryGetValue(key, out bool expanded))
                expanded = false;
            expanded = EditorGUI.Foldout(leftRect, expanded, labelContent, true);
            foldoutStates[key] = expanded;

            // Center: Prev / Page / Next (true-centered on the row)
            Rect prevRect = new Rect(centerRect.x, centerRect.y, prevW, centerRect.height);
            Rect pageRect = new Rect(prevRect.xMax + gap, centerRect.y, pageW, centerRect.height);
            Rect nextRect = new Rect(pageRect.xMax + gap, centerRect.y, nextW, centerRect.height);

            using (new EditorGUI.DisabledScope(page <= 0))
            {
                if (GUI.Button(prevRect, "Prev"))
                    _collectionPageByPath[key] = Mathf.Max(0, page - 1);
            }

            var centeredMiniLabel = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleCenter };
            GUI.Label(pageRect, pageContent, centeredMiniLabel);

            using (new EditorGUI.DisabledScope(page >= totalPages - 1))
            {
                if (GUI.Button(nextRect, "Next"))
                    _collectionPageByPath[key] = Mathf.Min(totalPages - 1, page + 1);
            }

            // Right: item count + add
            Rect plusRect = new Rect(rightRect.xMax - plusW, rightRect.y, plusW, rightRect.height);
            Rect countRect = new Rect(rightRect.x, rightRect.y, Mathf.Max(0f, rightRect.width - plusW - gap), rightRect.height);
            GUI.Label(countRect, countContent, new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleLeft });
            addClicked = GUI.Button(plusRect, "+");
        }
    }
}

#endif
