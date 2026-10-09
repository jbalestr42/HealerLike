using InspectorKit.Editor;
using UnityEditor;
using UnityEngine;

// A wave as a grid of its units, the front column on the left, each unit shown with its model
[CustomEditor(typeof(WavePatternData))]
public class WavePatternDataEditor : Editor
{
    GridGUIOptions _options;

    void OnEnable()
    {
        _options = new GridGUIOptions
        {
            firstColumnLabel = "Front",
            getPreview = GetPreview,
            getLabel = obj => obj is EntityData unit && !string.IsNullOrEmpty(unit.title) ? unit.title : obj.name,
        };
    }

    static Texture GetPreview(Object obj)
    {
        GameObject model = obj is EntityData unit ? unit.model : null;
        return model != null ? AssetPreview.GetAssetPreview(model) : null;
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        GridGUI.Draw(serializedObject.FindProperty("_cells"), serializedObject.FindProperty("_width"), serializedObject.FindProperty("_height"), typeof(EntityData), _options);
        EditorGUILayout.HelpBox("Drop units from the project or pick them with the button of a cell. Drag a unit to move it, right click to empty a cell, double click to select the unit.", MessageType.None);

        EditorGUILayout.Space();
        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("score"), true);
        }

        serializedObject.ApplyModifiedProperties();

        // The previews of the models load in the background
        if (AssetPreview.IsLoadingAssetPreviews())
        {
            Repaint();
        }
    }
}
