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
            var selected = selections.First();
            var so = ScriptableObject.CreateInstance(selected);
            string currentDirectory = Selection.activeObject != null ? Path.GetDirectoryName(AssetDatabase.GetAssetPath(Selection.activeObject)) : "Assets/";
            var uniquePath = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(currentDirectory, $"{selected.GetNiceName()}.asset"));
            AssetDatabase.CreateAsset(so, uniquePath);
            this.ValueEntry.SmartValue = (T)so;
        };
    }

    protected override void DrawPropertyLayout(GUIContent label)
    {
        if (this.ValueEntry.SmartValue != null)
        {
            this.CallNextDrawer(label);
            return;
        }

        EditorGUILayout.BeginHorizontal();

        this.CallNextDrawer(label);

        if (SirenixEditorGUI.ToolbarButton(new GUIContent() { image = EditorIcons.TriangleRight.Raw }))
        {
            this.selector.ShowInPopup();
        }

        EditorGUILayout.EndHorizontal();
    }
}