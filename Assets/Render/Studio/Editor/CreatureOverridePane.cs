using UnityEditor;
using UnityEngine;
using HealerLike.Render.Creatures;

namespace HealerLike.Render.Studio.Editor
{
    // The grammar inspector's last section: which view the game resolves for the source entity, and a switch to
    // audition the authored override in place of the grammar
    public static class CreatureOverridePane
    {
        public static void Draw(CreatureStudioWindow window, StudioStyles styles)
        {
            CreatureGrammarMode grammar = window.grammar;
            CreatureGrammarPreset selected = grammar.selected;
            if (selected.sourceEntity == null)
            {
                return;
            }

            styles.Section("GAME VIEW RESOLUTION");
            EditorGUI.BeginChangeCheck();
            grammar.creatureLooks = (CreatureLooks)EditorGUILayout.ObjectField("Creature looks", grammar.creatureLooks,
                typeof(CreatureLooks), false);
            if (EditorGUI.EndChangeCheck())
            {
                grammar.Regenerate();
            }

            CreatureLooks looks = grammar.creatureLooks;
            if (looks == null)
            {
                EditorGUILayout.HelpBox("Assign CreatureLooks to inspect the actual game override resolution.",
                    MessageType.Info);
                return;
            }

            GameObject resolved = null;
            if (looks.entities != null)
            {
                resolved = looks.GetView(selected.sourceEntity, selected.sourceSide);
            }

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.ObjectField("Resolved view", resolved, typeof(GameObject), false);
            }

            if (grammar.HasSourceOverride())
            {
                DrawAudition(grammar, styles);
            }
            else
            {
                GUILayout.Label("No authored entity override. The game uses the side’s derived host and its "
                    + "configured vocabulary.", styles.small);
            }

            if (looks.vocabulary != selected.vocabulary)
            {
                EditorGUILayout.HelpBox("This preset's vocabulary differs from the game CreatureLooks vocabulary.",
                    MessageType.Warning);
            }

            if (GUILayout.Button("Open source override table"))
            {
                RenderGrammarLibraryWindow.OpenAsset(looks);
            }
        }

        static void DrawAudition(CreatureGrammarMode grammar, StudioStyles styles)
        {
            EditorGUILayout.HelpBox("An authored entity override wins in the game. The grammar output is an audition "
                + "until the native override table is changed.", MessageType.Warning);
            CreatureRecipe authored = grammar.GetOverrideRecipe();
            using (new EditorGUI.DisabledScope(authored == null))
            {
                EditorGUI.BeginChangeCheck();
                grammar.isOverrideShown = EditorGUILayout.Toggle("Preview game override", grammar.isOverrideShown);
                if (EditorGUI.EndChangeCheck())
                {
                    grammar.Regenerate();
                }
            }

            if (authored == null)
            {
                GUILayout.Label("This override does not expose a CreatureBuilder recipe; inspect its prefab in the "
                    + "native table.", styles.small);
            }
        }
    }
}
