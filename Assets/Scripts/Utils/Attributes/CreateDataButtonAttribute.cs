using System;
using System.IO;
using System.Linq;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

public class CreateDataButtonAttribute : System.Attribute { }

[DrawerPriority(0, 0, 3001)]
public class CreateDataButtonAttributeDrawer<T> : OdinAttributeDrawer<CreateDataButtonAttribute, T> where T : ScriptableObject
{
    private GenericSelector<Type> selector;

    protected override void Initialize()
    {
        var types = TypeCache.GetTypesDerivedFrom<T>();
        this.selector = new GenericSelector<Type>(types.Where(type => !type.IsGenericType));
        this.selector.EnableSingleClickToSelect();
        this.selector.SelectionConfirmed += selections =>
        {
            this.ValueEntry.SmartValue = CreateSO(selections.First());
        };
    }

    protected override void DrawPropertyLayout(GUIContent label)
    {
        EditorGUILayout.BeginHorizontal();

        this.CallNextDrawer(label);

        if (this.ValueEntry.SmartValue != null)
        {
            if (SirenixEditorGUI.ToolbarButton(EditorIcons.X.ActiveGUIContent))
            {
                // Tricks to show "None" instead of "Missing"
                var tmp = this.ValueEntry.SmartValue;
                this.ValueEntry.SmartValue = null;
                DeleteSO(tmp);
            }
        }
        else
        {
            if (this.selector.SelectionTree.MenuItems.Count > 0)
            {
                if (SirenixEditorGUI.ToolbarButton(EditorIcons.TriangleRight.ActiveGUIContent))
                {
                    this.selector.ShowInPopup();
                }
            }
            else
            {
                if (SirenixEditorGUI.ToolbarButton(EditorIcons.Plus.ActiveGUIContent))
                {
                    this.ValueEntry.SmartValue = CreateSO(typeof(T));
                }
            }
        }

        EditorGUILayout.EndHorizontal();
    }

    private T CreateSO(Type type)
    {
        var so = ScriptableObject.CreateInstance(type);
        string currentDirectory = Selection.activeObject != null ? Path.GetDirectoryName(AssetDatabase.GetAssetPath(Selection.activeObject)) : "Assets/";
        var uniquePath = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(currentDirectory, $"{type.GetNiceName()}.asset"));
        AssetDatabase.CreateAsset(so, uniquePath);
        return (T)so;
    }

    private void DeleteSO(T so)
    {
        string assetPath = AssetDatabase.GetAssetPath(so);
        AssetDatabase.DeleteAsset(assetPath);
        AssetDatabase.Refresh();
    }
}