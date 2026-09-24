using UnityEditor;
using UnityEngine;
using HealerLike.Render.Creatures;

namespace HealerLike.Render.Studio.Editor
{
    // A creature recipe's inspector: the default fields, and a way into the creature studio
    [CustomEditor(typeof(CreatureRecipe))]
    public class CreatureStudioRecipeEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            CreatureRecipe recipe = (CreatureRecipe)target;
            EditorGUILayout.Space(6f);
            if (GUILayout.Button("Open in Creature Studio", GUILayout.Height(32f)))
            {
                CreatureStudioMenu.OpenRecipe(recipe);
            }

            EditorGUILayout.HelpBox("Assemble parts, edit the rig and preview this recipe in Creature Studio.",
                MessageType.Info);
            EditorGUILayout.Space(4f);
            if (DrawDefaultInspector())
            {
                CreatureStudioMenu.NotifyRecipeChanged(recipe);
            }
        }
    }
}
