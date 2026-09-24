using UnityEditor;
using UnityEngine;

namespace HealerLike.Render.Creatures.Editor.Studio
{
    [CustomEditor(typeof(CreatureRecipe))]
    public sealed class CreatureStudioRecipeEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var recipe=(CreatureRecipe)target;
            EditorGUILayout.Space(6);
            if (GUILayout.Button("Open in Creature Studio",GUILayout.Height(32))) CreatureStudioWindow.OpenRecipe(recipe);
            EditorGUILayout.HelpBox("Assemble parts, edit the rig and preview this recipe in Creature Studio.",MessageType.Info);
            EditorGUILayout.Space(4);
            if (DrawDefaultInspector()) CreatureStudioWindow.NotifyRecipeChanged(recipe);
        }
    }
}
