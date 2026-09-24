using UnityEditor;
using UnityEngine;

namespace HealerLike.Render.Studio.Editor
{
    // A grammar preset's inspector: the default fields, and a way into the creature studio's grammar mode
    [CustomEditor(typeof(CreatureGrammarPreset))]
    public class CreatureGrammarPresetEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            CreatureGrammarPreset preset = (CreatureGrammarPreset)target;
            if (GUILayout.Button("Open grammar in Creature Studio", GUILayout.Height(32f)))
            {
                CreatureStudioMenu.OpenGrammar(preset);
            }

            EditorGUILayout.HelpBox("This preset stores editable grammar channels. Composed parts are generated for "
                + "preview; bake explicitly to make a manual recipe.", MessageType.Info);
            if (DrawDefaultInspector())
            {
                CreatureStudioMenu.NotifyGrammarChanged(preset);
            }
        }
    }
}
