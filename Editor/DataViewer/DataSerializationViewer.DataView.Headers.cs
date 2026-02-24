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
        private static string GetRootDisplayName(string key)
        {
            if (string.Equals(key, "LastSaveTime", StringComparison.OrdinalIgnoreCase))
                return "Last Save Time (UTC)";
            return ObjectNames.NicifyVariableName(key);
        }

        private void DrawRootCollectionHeaderRow(string key, int count, out bool addClicked)
        {
            addClicked = false;

            var labelContent = new GUIContent(GetRootDisplayName(key));
            int totalPages = Mathf.Max(1, Mathf.CeilToInt(count / (float)CollectionItemsPerPage));
            int page = GetCollectionPage(key);
            if (page >= totalPages) page = totalPages - 1;
            if (page < 0) page = 0;
            _collectionPageByPath[key] = page;

            Rect rowRect = GetRootHeaderRowRect();

            const float gap = 6f;
            const float arrowW = 22f;
            const float plusW = 26f;

#if ODIN_INSPECTOR
            GUIStyle miniLeft = SirenixGUIStyles.LeftAlignedGreyMiniLabel;
            GUIStyle miniCenter = SirenixGUIStyles.CenteredGreyMiniLabel;
#else
            GUIStyle miniLeft = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleLeft };
            GUIStyle miniCenter = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleCenter };
#endif

            var pageContent = new GUIContent($"{page + 1} / {totalPages}");
            float pageW = Mathf.Ceil(miniCenter.CalcSize(pageContent).x) + 8f;

            var countContent = new GUIContent($"{count} items");
            float countW = Mathf.Ceil(miniLeft.CalcSize(countContent).x);

            // Right group: "{count} items"  ◀  1/2  ▶  +
            float rightW = countW + gap + arrowW + gap + pageW + gap + arrowW + gap + plusW;
            Rect rightRect = new Rect(rowRect.xMax - rightW, rowRect.y, rightW, rowRect.height);
            Rect leftRect = new Rect(rowRect.xMin, rowRect.y, Mathf.Max(0f, rightRect.xMin - gap - rowRect.xMin), rowRect.height);

            if (!foldoutStates.TryGetValue(key, out bool expanded))
                expanded = false;
#if ODIN_INSPECTOR
            expanded = SirenixEditorGUI.Foldout(leftRect, expanded, labelContent, SirenixGUIStyles.Foldout);
#else
            expanded = EditorGUI.Foldout(leftRect, expanded, labelContent, true);
#endif
            foldoutStates[key] = expanded;

            Rect plusRect = new Rect(rightRect.xMax - plusW, rightRect.y, plusW, rightRect.height);
            Rect nextRect = new Rect(plusRect.xMin - gap - arrowW, rightRect.y, arrowW, rightRect.height);
            Rect pageRect = new Rect(nextRect.xMin - gap - pageW, rightRect.y, pageW, rightRect.height);
            Rect prevRect = new Rect(pageRect.xMin - gap - arrowW, rightRect.y, arrowW, rightRect.height);
            Rect countRect = new Rect(rightRect.x, rightRect.y, Mathf.Max(0f, prevRect.xMin - gap - rightRect.x), rightRect.height);

            GUI.Label(countRect, countContent, miniLeft);

            using (new EditorGUI.DisabledScope(page <= 0))
            {
                if (GUI.Button(prevRect, "◀"))
                    _collectionPageByPath[key] = Mathf.Max(0, page - 1);
            }

            GUI.Label(pageRect, pageContent, miniCenter);

            using (new EditorGUI.DisabledScope(page >= totalPages - 1))
            {
                if (GUI.Button(nextRect, "▶"))
                    _collectionPageByPath[key] = Mathf.Min(totalPages - 1, page + 1);
            }

            addClicked = GUI.Button(plusRect, "+");
        }

        private void DrawRootSummaryHeaderRow(string key, string summary)
        {
            var labelContent = new GUIContent(GetRootDisplayName(key));

            Rect rowRect = GetRootHeaderRowRect();

            const float gap = 6f;
            const float plusW = 26f;

            bool hasSummary = !string.IsNullOrEmpty(summary);
            var summaryContent = hasSummary ? new GUIContent(summary) : GUIContent.none;

#if ODIN_INSPECTOR
            GUIStyle miniRight = SirenixGUIStyles.RightAlignedGreyMiniLabel;
#else
            GUIStyle miniRight = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleRight };
#endif

            float summaryW = hasSummary ? Mathf.Ceil(miniRight.CalcSize(summaryContent).x) : 0f;

            // Reserve space for the "+" button so summary text aligns with collection headers.
            Rect plusSlotRect = new Rect(rowRect.xMax - plusW, rowRect.y, plusW, rowRect.height);

            Rect summaryRect = default;
            if (hasSummary)
                summaryRect = new Rect(plusSlotRect.xMin - gap - summaryW, rowRect.y, summaryW, rowRect.height);

            float leftMax = hasSummary ? (summaryRect.xMin - gap) : (plusSlotRect.xMin - gap);
            Rect leftRect = new Rect(rowRect.xMin, rowRect.y, Mathf.Max(0f, leftMax - rowRect.xMin), rowRect.height);

            if (!foldoutStates.TryGetValue(key, out bool expanded))
                expanded = false;
#if ODIN_INSPECTOR
            expanded = SirenixEditorGUI.Foldout(leftRect, expanded, labelContent, SirenixGUIStyles.Foldout);
#else
            expanded = EditorGUI.Foldout(leftRect, expanded, labelContent, true);
#endif
            foldoutStates[key] = expanded;

            if (hasSummary)
                GUI.Label(summaryRect, summaryContent, miniRight);
        }

        private static Rect GetRootHeaderRowRect()
        {
            Rect rowRect = GUILayoutUtility.GetRect(1f, EditorGUIUtility.singleLineHeight + 2f, GUILayout.ExpandWidth(true));

#if ODIN_INSPECTOR
            // In Odin mode, keep the rect un-offset; Odin's box/padding handles layout.
            rowRect.height = EditorGUIUtility.singleLineHeight;
#else
            rowRect.y += 1f;
            rowRect.height = EditorGUIUtility.singleLineHeight;
            rowRect.xMin += 2f;
            rowRect.xMax -= 2f;
#endif

            return rowRect;
        }
    }
}

#endif
