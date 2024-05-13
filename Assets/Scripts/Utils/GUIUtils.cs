using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

public static class GUIUtils
{
    public static bool SelectButtonList(ref Type selectedType, Type[] typesToDisplay)
    {
        Rect rect = GUILayoutUtility.GetRect(0, 25);

        for (int i = 0; i < typesToDisplay.Length; i++)
        {
            string name = typesToDisplay[i].Name;
            Rect btnRect = rect.Split(i, typesToDisplay.Length);

            if (GUIUtils.SelectButton(btnRect, name, typesToDisplay[i] == selectedType))
            {
                selectedType = typesToDisplay[i];
                return true;
            }
        }

        return false;
    }

    public static bool SelectButtonList(ref string selectedType, string[] typesToDisplay)
    {
        Rect rect = GUILayoutUtility.GetRect(0, 25);

        for (int i = 0; i < typesToDisplay.Length; i++)
        {
            string name = typesToDisplay[i];
            Rect btnRect = rect.Split(i, typesToDisplay.Length);

            if (GUIUtils.SelectButton(btnRect, name, typesToDisplay[i] == selectedType))
            {
                selectedType = typesToDisplay[i];
                return true;
            }
        }

        return false;
    }

    public static bool SelectButton(Rect rect, string name, bool selected)
    {
        if (GUI.Button(rect, GUIContent.none, GUIStyle.none))
        {
            return true;
        }

        if (Event.current.type == EventType.Repaint)
        {
            GUIStyle style = new GUIStyle(EditorStyles.miniButtonMid);
            style.stretchHeight = true;
            style.fixedHeight = rect.height;
            style.Draw(rect, GUIHelper.TempContent(name), false, false, selected, false);
        }

        return false;
    }

    public static void DrawRefreshButton<TList, TElement>(TList list, ScriptableObject dataHolder) where TList : IList<TElement> where TElement : ScriptableObject
    {
        if (SirenixEditorGUI.ToolbarButton(EditorIcons.Refresh))
        {
            #if UNITY_EDITOR
            list.Clear();
            List<TElement> allItems = AssetUtils.FindAssetsByType<TElement>();
            foreach (TElement item in allItems)
            {
                if (!list.Contains(item))
                {
                    list.Add(item);
                }
            }
            EditorUtility.SetDirty(dataHolder);
            AssetDatabase.SaveAssets();
            #endif
        }
    }

    public static void CreateDataButton<TList, TElement>(TList list) where TList : IList<TElement> where TElement : ScriptableObject
    {
        if (SirenixEditorGUI.ToolbarButton(EditorIcons.TriangleRight))
        {
            GenericSelector<Type> selector;
            var types = TypeCache.GetTypesDerivedFrom<TElement>();
            selector = new GenericSelector<Type>(types.Where(type => !type.IsGenericType));
            selector.EnableSingleClickToSelect();
            selector.SelectionConfirmed += selections =>
            {
                var selected = selections.First();
                var so = ScriptableObject.CreateInstance(selected);
                string currentDirectory = Selection.activeObject != null ? Path.GetDirectoryName(AssetDatabase.GetAssetPath(Selection.activeObject)) : "Assets/";
                var uniquePath = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(currentDirectory, $"{selected.GetNiceName()}.asset"));
                AssetDatabase.CreateAsset(so, uniquePath);
                list.Add((TElement)so);
            };
            selector.ShowInPopup();
        }
    }
}