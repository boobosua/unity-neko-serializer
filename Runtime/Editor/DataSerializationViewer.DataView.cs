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

            var keys = GetDataViewRootKeysForDisplay();
            var paginationInfo = CalculatePagination(keys.Count);

            // Keep pagination frozen at the top (outside the scroll view).
            DrawPaginationControls(paginationInfo, keys.Count);

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
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

        private void DrawPaginationControls((int totalPages, int startIndex, int endIndex) paginationInfo, int totalItems)
        {
            if (paginationInfo.totalPages <= 1) return;

            // Odin-style: "{n} items" then ◀ [page] / total ▶ on one row.
            const float gap = 6f;
            const float arrowW = 22f;

            Rect rowRect = GUILayoutUtility.GetRect(1f, EditorGUIUtility.singleLineHeight + 2f, GUILayout.ExpandWidth(true));
            rowRect.y += 1f;
            rowRect.height = EditorGUIUtility.singleLineHeight;
            rowRect.xMin += 2f;
            rowRect.xMax -= 2f;

            GUIStyle miniLeft;
            GUIStyle miniCenter;
#if ODIN_INSPECTOR
            miniLeft = SirenixGUIStyles.LeftAlignedGreyMiniLabel;
            miniCenter = SirenixGUIStyles.CenteredGreyMiniLabel;
#else
            miniLeft = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleLeft };
            miniCenter = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleCenter };
#endif

            var countContent = new GUIContent($"{totalItems} items");
            float countW = Mathf.Ceil(miniLeft.CalcSize(countContent).x);

            var slashContent = new GUIContent($"/ {paginationInfo.totalPages}");
            float slashW = Mathf.Ceil(miniCenter.CalcSize(slashContent).x) + 4f;

            // Page field width: enough for the largest page number.
            var maxPageContent = new GUIContent(Mathf.Max(1, paginationInfo.totalPages).ToString());
            float fieldW = Mathf.Max(26f, Mathf.Ceil(EditorStyles.numberField.CalcSize(maxPageContent).x) + 10f);

            float rightW = arrowW + gap + fieldW + gap + slashW + gap + arrowW;
            Rect rightRect = new Rect(rowRect.xMax - rightW, rowRect.y, rightW, rowRect.height);

            Rect nextRect = new Rect(rightRect.xMax - arrowW, rightRect.y, arrowW, rightRect.height);
            Rect slashRect = new Rect(nextRect.xMin - gap - slashW, rightRect.y, slashW, rightRect.height);
            Rect fieldRect = new Rect(slashRect.xMin - gap - fieldW, rightRect.y, fieldW, rightRect.height);
            Rect prevRect = new Rect(fieldRect.xMin - gap - arrowW, rightRect.y, arrowW, rightRect.height);

            Rect countRect = new Rect(rowRect.xMin, rowRect.y, Mathf.Max(0f, prevRect.xMin - gap - rowRect.xMin), rowRect.height);

            GUI.Label(countRect, countContent, miniLeft);

            using (new EditorGUI.DisabledScope(currentPage <= 0))
            {
                if (GUI.Button(prevRect, "◀"))
                    currentPage = Mathf.Max(0, currentPage - 1);
            }

            int pageOneBased = currentPage + 1;
            EditorGUI.BeginChangeCheck();
            pageOneBased = EditorGUI.IntField(fieldRect, pageOneBased, EditorStyles.numberField);
            if (EditorGUI.EndChangeCheck())
            {
                pageOneBased = Mathf.Clamp(pageOneBased, 1, paginationInfo.totalPages);
                currentPage = pageOneBased - 1;
            }

            GUI.Label(slashRect, slashContent, miniCenter);

            using (new EditorGUI.DisabledScope(currentPage >= paginationInfo.totalPages - 1))
            {
                if (GUI.Button(nextRect, "▶"))
                    currentPage = Mathf.Min(paginationInfo.totalPages - 1, currentPage + 1);
            }

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

            bool allowJObjectConversion = odinRootValue is JObject joForOdin && !ShouldRenderJObjectRootAsStructFields(kvp.Key, joForOdin);
            if (odinRootValue is JArray || allowJObjectConversion)
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

            // Root special cases.
            bool isDateTimeRoot = false;
            DateTime dateTimeRoot = default;

            // Struct/class-like JObject roots should be rendered as fields, not as collections.
            // This preserves dictionary UI for dictionary-like JObject roots (e.g., string->Vector3 maps).
            bool isStructLikeJObjectRoot = false;
            {
                object raw = kvp.Value;
                if (raw is JValue jv2)
                    raw = jv2.Value;

                if (raw is DateTime dt)
                {
                    isDateTimeRoot = true;
                    dateTimeRoot = dt;
                }
                else if (raw is JObject jo)
                {
                    isStructLikeJObjectRoot = ShouldRenderJObjectRootAsStructFields(kvp.Key, jo);
                }
            }

            // If this key holds a single value (int/float/string/etc.), show it as a normal inspector field (no foldout).
            // DateTime roots are handled below as a foldable boxgroup.
            if (!isDateTimeRoot && IsSingleValue(kvp.Value))
            {
                object updated = kvp.Value;
                bool changed = DrawEditableAny(GetRootDisplayName(kvp.Key), ref updated, kvp.Key);
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

            // Custom foldout header.
            // - Collections: show item count + add button.
            // - DateTime: show formatted timestamp on the right.
            // - Struct/class-like JObject: show a normal header (no key/value table).
            if (isDateTimeRoot)
            {
                bool forceUtc = IsUtcDateTimeKey(kvp.Key);
                DrawRootSummaryHeaderRow(kvp.Key, FormatDateTimeSummary(dateTimeRoot, forceUtc));
            }
            else if (isStructLikeJObjectRoot)
            {
                DrawRootSummaryHeaderRow(kvp.Key, null);
            }
            else if (TryGetCollectionCount(kvp.Value, out int count))
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
                // Keep header alignment consistent with other root rows.
                DrawRootSummaryHeaderRow(kvp.Key, null);
            }

            if (foldoutStates[kvp.Key])
            {
                if (isDateTimeRoot)
                {
                    var updated = dateTimeRoot;
                    if (DrawEditableDateTimeFields(dateTimeRoot, out updated))
                    {
                        stagedSaveData[kvp.Key] = updated;
                        MarkRootDirty(kvp.Key);
                    }
                }
                else
                {
                    DisplayData(kvp.Key, kvp.Value);
                }
            }

#if ODIN_INSPECTOR
            SirenixEditorGUI.EndBox();
#else
            EditorGUILayout.EndVertical();
#endif
            EditorGUILayout.Space(2);
        }
    }
}

#endif
