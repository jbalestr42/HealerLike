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
            EditorGUILayout.Space(6);
            if (GUILayout.Button("Open in Spell Studio", GUILayout.Height(32)))
                SpellStudioWindow.OpenPreset(preset);
            EditorGUILayout.HelpBox("Edit this recipe in Spell Studio to see its motion, shape and cast context in a live preview.", MessageType.Info);
            EditorGUILayout.Space(4);
            if (DrawDefaultInspector()) SpellStudioWindow.NotifyPresetChanged(preset);
        }
    }
}
