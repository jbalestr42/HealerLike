using UnityEditor;
using UnityEngine;
using HealerLike.Render.Grammar;
using HealerLike.Render.Spells;

namespace HealerLike.Render.Studio.Editor
{
    // The inspector's grammar section: where the element comes from, the fields that mode reads, the native rows,
    // and what the preset resolves to
    public static class SpellGrammarFields
    {
        public static void Draw(SerializedObject serialized, SpellStudioPreset selected, StudioStyles styles)
        {
            Field(serialized, "mode", "Recipe source");
            SpellStudioMode mode = (SpellStudioMode)serialized.FindProperty("mode").enumValueIndex;
            Field(serialized, "vocabulary", "Vocabulary");
            using (new EditorGUI.DisabledScope(selected.vocabulary == null))
            {
                if (GUILayout.Button("Edit effect vocabulary…"))
                {
                    RenderGrammarLibraryWindow.OpenAsset(selected.vocabulary);
                }
            }

            if (mode == SpellStudioMode.AuthoredElement)
            {
                Field(serialized, "element", "Element");
                Field(serialized, "family", "Family");
                DrawTiming(serialized);
                GUILayout.Label("Authored elements keep full manual control. Grammar mode derives the element "
                    + "from family and attribute group.", styles.small);
            }
            else if (mode == SpellStudioMode.GrammarChannels)
            {
                Field(serialized, "family", "Family");
                Field(serialized, "attributeGroup", "Attribute group");
                DrawTiming(serialized);
                GUILayout.Label("Uses EffectComposer: boon + defence → plates; boon + prevention → bud; "
                    + "bane + offence → press.", styles.small);
            }
            else
            {
                DrawHandler(serialized, selected, styles);
            }

            Field(serialized, "spellLooks", "Native presets");
            Field(serialized, "useGameplayOverrides", "Use native rows");
            using (new EditorGUI.DisabledScope(selected.spellLooks == null))
            {
                if (GUILayout.Button("Edit native spell / projectile presets…"))
                {
                    RenderGrammarLibraryWindow.OpenAsset(selected.spellLooks);
                }
            }

            DrawResolution(selected, mode);
            Field(serialized, "sourceProjectile", "Projectile lookup");
            if (selected.sourceProjectile != null)
            {
                GUILayout.Label(SpellInspectorPane.ProjectileReadout(selected), styles.small);
            }
        }

        static void DrawTiming(SerializedObject serialized)
        {
            Field(serialized, "tempo", "Tempo");
            Field(serialized, "periodSeconds", "Period (s)");
        }

        static void DrawHandler(SerializedObject serialized, SpellStudioPreset selected, StudioStyles styles)
        {
            Field(serialized, "sourceHandler", "Buff handler");
            Field(serialized, "isSameSide", "Same side");
            using (new EditorGUI.DisabledScope(selected.sourceHandler == null))
            {
                if (GUILayout.Button("Inspect gameplay handler"))
                {
                    Selection.activeObject = selected.sourceHandler;
                    EditorGUIUtility.PingObject(selected.sourceHandler);
                }
            }

            GUILayout.Label("Reads consumer sign, modifier polarity, attribute group, duration and period from the "
                + "actual gameplay asset.", styles.small);
        }

        static void DrawResolution(SpellStudioPreset selected, SpellStudioMode mode)
        {
            EffectChannels channels;
            EffectElement resolved;
            if (!selected.TryResolve(out channels, out resolved))
            {
                EditorGUILayout.HelpBox("Choose a gameplay handler to resolve this preset.", MessageType.Info);
                return;
            }

            string priority = "Derived by renderer grammar";
            if (selected.usesGameplayOverride)
            {
                priority = "Native handler preset wins";
            }
            else if (mode == SpellStudioMode.AuthoredElement)
            {
                priority = "Manual element";
            }

            string text = priority + "\n" + channels.family + " · " + channels.group + " · " + channels.tempo
                + " → " + resolved;
            if (channels.tempo == EffectTempo.PerPeriod)
            {
                text += "\nPeriod: " + channels.periodSeconds.ToString("0.###") + " s; zero uses entry cycle.";
            }
            EditorGUILayout.HelpBox(text, MessageType.Info);
        }

        static void Field(SerializedObject serialized, string name, string label)
        {
            SerializedProperty property = serialized.FindProperty(name);
            if (property != null)
            {
                EditorGUILayout.PropertyField(property, new GUIContent(label), false);
            }
        }
    }
}
