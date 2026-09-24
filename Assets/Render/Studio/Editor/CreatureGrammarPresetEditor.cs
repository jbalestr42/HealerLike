using UnityEditor;
using UnityEngine;
using HealerLike.Render.Creatures;
using HealerLike.Render.Grammar;
using HealerLike.Render.Spells;

namespace HealerLike.Render.Studio.Editor
{
    [CustomEditor(typeof(CreatureGrammarPreset))]
    public sealed class CreatureGrammarPresetEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var preset=(CreatureGrammarPreset)target;
            if (GUILayout.Button("Open grammar in Creature Studio",GUILayout.Height(32))) CreatureStudioWindow.OpenGrammar(preset);
            EditorGUILayout.HelpBox("This preset stores editable grammar channels. Composed parts are generated for preview; bake explicitly to make a manual recipe.",MessageType.Info);
            if (DrawDefaultInspector()) CreatureStudioWindow.NotifyGrammarChanged(preset);
        }
    }
}
