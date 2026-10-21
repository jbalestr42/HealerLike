using UnityEditor;
using UnityEngine;
using HealerLike.Render.Creatures;
using HealerLike.Render.Grammar;

namespace HealerLike.Render.Studio.Editor
{
    // The creature studio's right column in grammar mode: the preset's channels, set by hand or read from an entity,
    // what the grammar made of them, and the override the game would show for that entity instead
    public class CreatureGrammarInspector
    {
        CreatureStudioWindow _window;
        Vector2 _scroll;

        public void Init(CreatureStudioWindow window)
        {
            _window = window;
        }

        public void Reset()
        {
            _scroll = Vector2.zero;
        }

        public void Draw(Rect rect)
        {
            using (StudioLabelWidthScope width = new StudioLabelWidthScope(112f))
            {
                StudioStyles styles = _window.styles;
                CreatureGrammarMode grammar = _window.grammar;
                CreatureGrammarPreset selected = grammar.selected;
                EditorGUI.DrawRect(rect, StudioStyles.Panel);
                GUILayout.BeginArea(new Rect(rect.x + 12f, rect.y + 12f, rect.width - 24f, rect.height - 24f));
                GUILayout.Label("GRAMMAR CREATOR", styles.section);
                if (selected == null)
                {
                    GUILayout.Label("Choose a grammar preset.");
                    GUILayout.EndArea();
                    return;
                }

                string state = "Local channel draft · Undo supported";
                if (AssetDatabase.Contains(selected))
                {
                    state = "Saved channel preset · Undo supported";
                }

                GUILayout.Label(state, styles.small);
                _scroll = EditorGUILayout.BeginScrollView(_scroll);
                grammar.serialized.Update();
                DrawChannels(styles, grammar);
                DrawOutput(styles, grammar);
                CreatureOverridePane.Draw(_window, styles);
                GUILayout.Space(14f);
                using (new EditorGUI.DisabledScope(grammar.output == null))
                {
                    if (GUILayout.Button("Bake grammar output to editable recipe", GUILayout.Height(30f)))
                    {
                        _window.BakeGrammar();
                        EndMutation();
                    }
                }

                GUILayout.Label("Baking copies the generated grammar output, even while auditioning a game override. "
                    + "It creates an independent Parts draft.", styles.small);
                GUILayout.Space(8f);
                if (GUILayout.Button("Duplicate grammar preset"))
                {
                    _window.SelectGrammar(grammar.drafts.Duplicate(selected));
                    EndMutation();
                }

                if (GUILayout.Button("Edit vocabulary & native preset tables"))
                {
                    RenderGrammarLibraryWindow.OpenAsset(selected.vocabulary);
                }

                EditorGUILayout.EndScrollView();
                GUILayout.EndArea();
            }
        }

        void DrawChannels(StudioStyles styles, CreatureGrammarMode grammar)
        {
            SerializedObject serialized = grammar.serialized;
            EditorGUI.BeginChangeCheck();
            styles.Section("PRESET & VOCABULARY");
            Field(serialized, "displayName", "Name");
            Field(serialized, "description", "Notes");
            Field(serialized, "vocabulary", "Vocabulary");
            styles.Section("OPTIONAL GAME SOURCE");
            DrawEntityPopup(grammar);
            Field(serialized, "sourceEntity", "Entity asset");
            Field(serialized, "sourceSide", "Entity side");
            Field(serialized, "deriveFromEntity", "Derive channels");
            if (grammar.HasSourceOverride())
            {
                EditorGUILayout.HelpBox("The game has an authored override for this entity. This viewport auditions "
                    + "grammar unless you enable Preview game override below.", MessageType.Warning);
            }

            styles.Section("VISUAL CHANNELS");
            if (serialized.FindProperty("deriveFromEntity").boolValue)
            {
                DrawDerivedChannels(grammar.channels);
            }
            else
            {
                Field(serialized, "side", "Plant / stone");
                Field(serialized, "head", "Head");
                Field(serialized, "count", "Count");
                Field(serialized, "stem", "Stem / cadence");
                Field(serialized, "mass", "Mass");
                Field(serialized, "reach", "Reach");
                Field(serialized, "accessory", "Accessory");
                Field(serialized, "accessoryHead", "Accessory head");
                Field(serialized, "accent", "Accent");
            }

            bool isChanged = EditorGUI.EndChangeCheck();
            if (serialized.ApplyModifiedProperties() || isChanged)
            {
                grammar.Regenerate();
            }

            using (new EditorGUI.DisabledScope(grammar.selected.sourceEntity == null))
            {
                if (GUILayout.Button("Copy derived channels into manual controls"))
                {
                    Undo.RecordObject(grammar.selected, "Copy derived creature channels");
                    if (grammar.selected.ReadFromEntity())
                    {
                        EditorUtility.SetDirty(grammar.selected);
                        serialized.Update();
                        grammar.Regenerate();
                    }
                }
            }
        }

        // Choosing an entity also turns derivation on, choosing none turns it off
        static void DrawEntityPopup(CreatureGrammarMode grammar)
        {
            SerializedProperty source = grammar.serialized.FindProperty("sourceEntity");
            int current = grammar.entities.IndexOf(source.objectReferenceValue as EntityData) + 1;
            int chosen = EditorGUILayout.Popup("Game entity", current, grammar.entityNames);
            if (chosen == current)
            {
                return;
            }

            source.objectReferenceValue = null;
            if (chosen > 0)
            {
                source.objectReferenceValue = grammar.entities[chosen - 1];
            }

            grammar.serialized.FindProperty("deriveFromEntity").boolValue = chosen > 0;
        }

        static void DrawDerivedChannels(UnitChannels channels)
        {
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.EnumPopup("Plant / stone", channels.side);
                EditorGUILayout.EnumPopup("Head", channels.head);
                EditorGUILayout.EnumPopup("Count", channels.count);
                EditorGUILayout.EnumPopup("Stem / cadence", channels.stem);
                EditorGUILayout.EnumPopup("Mass", channels.mass);
                EditorGUILayout.EnumPopup("Reach", channels.reach);
                EditorGUILayout.EnumPopup("Accessory", channels.accessory);
                EditorGUILayout.EnumPopup("Accessory head", channels.accessoryHead);
                EditorGUILayout.EnumPopup("Accent", channels.accent);
            }
        }

        static void DrawOutput(StudioStyles styles, CreatureGrammarMode grammar)
        {
            styles.Section("OUTPUT & PROVENANCE");
            if (grammar.warnings.Length == 0)
            {
                UnitChannels channels = grammar.channels;
                string source = "Manual channel preset";
                if (grammar.selected.deriveFromEntity)
                {
                    source = "Derived from game entity";
                }

                GUILayout.Label(source + " → LookComposer", EditorStyles.boldLabel);
                GUILayout.Label(channels.side + " · " + channels.head + " · " + channels.count + "\n" + channels.stem
                    + " stem · " + channels.mass + " mass · " + channels.reach + " reach\n" + channels.accessory
                    + " · " + channels.accent + " accent", styles.small);
                GUILayout.Label(Generated(grammar.output), styles.small);
            }

            foreach (string warning in grammar.warnings)
            {
                EditorGUILayout.HelpBox(warning, MessageType.Warning);
            }

            foreach (string note in grammar.notes)
            {
                EditorGUILayout.HelpBox(note, MessageType.Info);
            }
        }

        static string Generated(CreatureRecipe output)
        {
            int parts = 0;
            int arms = 0;
            if (output != null && output.parts != null)
            {
                parts = output.parts.Length;
            }

            if (output != null && output.arms != null)
            {
                arms = output.arms.Length;
            }

            return "Generated output: " + parts + " parts, " + arms + " arms";
        }

        static void Field(SerializedObject serialized, string name, string label)
        {
            SerializedProperty property = serialized.FindProperty(name);
            if (property != null)
            {
                EditorGUILayout.PropertyField(property, new GUIContent(label), true);
            }
        }

        static void EndMutation()
        {
            GUIUtility.ExitGUI();
        }
    }
}
