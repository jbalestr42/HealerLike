using UnityEditor;
using UnityEngine;
using HealerLike.Render.Spells.Studio;

namespace HealerLike.Render.Spells.Editor.Studio
{
    [CustomEditor(typeof(SpellStudioPreset))]
    public sealed class SpellStudioPresetEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var preset = (SpellStudioPreset)target;
            if (GUILayout.Button("Open in Spell Studio", GUILayout.Height(32))) SpellStudioWindow.OpenPreset(preset);
            serializedObject.Update();
            Field("displayName"); Field("description");
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("GRAMMAR & PRESETS", EditorStyles.boldLabel);
            Field("mode"); Field("vocabulary");
            var mode = (SpellStudioMode)serializedObject.FindProperty("mode").enumValueIndex;
            if (mode == SpellStudioMode.AuthoredElement)
            { Field("element"); Field("family"); Field("tempo"); Field("periodSeconds"); }
            else if (mode == SpellStudioMode.GrammarChannels)
            { Field("family"); Field("attributeGroup"); Field("tempo"); Field("periodSeconds"); }
            else { Field("sourceHandler"); Field("isSameSide"); }
            Field("spellLooks"); Field("useGameplayOverrides"); Field("sourceProjectile");
            if (preset.TryResolve(out var channels, out var element))
                EditorGUILayout.HelpBox((preset.UsesGameplayOverride ? "Native handler preset wins: " : "Resolved: ") +
                    channels.family + " / " + channels.group + " / " + channels.tempo + " → " + element, MessageType.Info);
            else EditorGUILayout.HelpBox("Choose a gameplay handler to derive its renderer grammar.", MessageType.Info);
            EditorGUILayout.BeginHorizontal();
            using (new EditorGUI.DisabledScope(preset.vocabulary == null))
                if (GUILayout.Button("Edit vocabulary")) RenderGrammarLibraryWindow.OpenAsset(preset.vocabulary);
            using (new EditorGUI.DisabledScope(preset.spellLooks == null))
                if (GUILayout.Button("Edit native presets")) RenderGrammarLibraryWindow.OpenAsset(preset.spellLooks);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("PREVIEW & AUTHORED OVERRIDES", EditorStyles.boldLabel);
            Field("durationSeconds"); Field("stacks"); Field("charges"); Field("amount"); Field("critical"); Field("side"); Field("scale");
            Field("overrideColour");
            if (serializedObject.FindProperty("overrideColour").boolValue) Field("colour");
            Field("overrideEntry");
            if (serializedObject.FindProperty("overrideEntry").boolValue) Field("entry", true);
            if (serializedObject.ApplyModifiedProperties()) SpellStudioWindow.NotifyPresetChanged(preset);
        }
        void Field(string name, bool children = false) => EditorGUILayout.PropertyField(serializedObject.FindProperty(name), children);
    }
}
