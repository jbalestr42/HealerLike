using UnityEditor;
using UnityEngine;
using HealerLike.Render.Grammar;
using HealerLike.Render.Spells;

namespace HealerLike.Render.Studio.Editor
{
    // A spell preset's inspector: the fields its mode reads, what it resolves to, and a way into the studio
    [CustomEditor(typeof(SpellStudioPreset))]
    public class SpellStudioPresetEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            SpellStudioPreset preset = (SpellStudioPreset)target;
            if (GUILayout.Button("Open in Spell Studio", GUILayout.Height(32f)))
            {
                SpellStudioMenu.OpenPreset(preset);
            }

            serializedObject.Update();
            Field("displayName");
            Field("description");
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("GRAMMAR & PRESETS", EditorStyles.boldLabel);
            Field("mode");
            Field("vocabulary");
            DrawModeFields((SpellStudioMode)serializedObject.FindProperty("mode").enumValueIndex);
            Field("spellLooks");
            Field("useGameplayOverrides");
            Field("sourceProjectile");
            DrawResolution(preset);
            DrawAssetButtons(preset);
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("PREVIEW & AUTHORED OVERRIDES", EditorStyles.boldLabel);
            Field("durationSeconds");
            Field("stacks");
            Field("charges");
            Field("amount");
            Field("critical");
            Field("side");
            Field("scale");
            Field("overrideColour");
            if (serializedObject.FindProperty("overrideColour").boolValue)
            {
                Field("colour");
            }

            Field("overrideEntry");
            if (serializedObject.FindProperty("overrideEntry").boolValue)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("entry"), true);
            }

            if (serializedObject.ApplyModifiedProperties())
            {
                SpellStudioMenu.NotifyPresetChanged(preset);
            }
        }

        void DrawModeFields(SpellStudioMode mode)
        {
            if (mode == SpellStudioMode.GameplayHandler)
            {
                Field("sourceHandler");
                Field("isSameSide");
                return;
            }

            if (mode == SpellStudioMode.AuthoredElement)
            {
                Field("element");
                Field("family");
            }
            else
            {
                Field("family");
                Field("attributeGroup");
            }

            Field("tempo");
            Field("periodSeconds");
        }

        static void DrawResolution(SpellStudioPreset preset)
        {
            EffectChannels channels;
            EffectElement element;
            if (!preset.TryResolve(out channels, out element))
            {
                EditorGUILayout.HelpBox("Choose a gameplay handler to derive its renderer grammar.", MessageType.Info);
                return;
            }

            string prefix = "Resolved: ";
            if (preset.usesGameplayOverride)
            {
                prefix = "Native handler preset wins: ";
            }

            string channelText = channels.family + " / " + channels.group + " / " + channels.tempo;
            string text = prefix + channelText + " → " + element;
            EditorGUILayout.HelpBox(text, MessageType.Info);
        }

        static void DrawAssetButtons(SpellStudioPreset preset)
        {
            EditorGUILayout.BeginHorizontal();
            using (new EditorGUI.DisabledScope(preset.vocabulary == null))
            {
                if (GUILayout.Button("Edit vocabulary"))
                {
                    RenderGrammarLibraryWindow.OpenAsset(preset.vocabulary);
                }
            }

            using (new EditorGUI.DisabledScope(preset.spellLooks == null))
            {
                if (GUILayout.Button("Edit native presets"))
                {
                    RenderGrammarLibraryWindow.OpenAsset(preset.spellLooks);
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        void Field(string name)
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty(name), false);
        }
    }
}
