using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(GridManager))]
public class GridManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.DrawDefaultInspector();

        GridManager gridManager = (GridManager)target;

        if (GUILayout.Button("Generate Grid"))
        {
            gridManager.Generate();
        }
    }
}
