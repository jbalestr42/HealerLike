using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace HealerLike.Render.Studio.Editor
{
    // The ways into the spell studio: the menu, a preset asset opened in the project, a preset inspector
    public static class SpellStudioMenu
    {
        [MenuItem("Tools/Render/Spell Studio", false, 110)]
        public static void Open()
        {
            bool isOpen = EditorWindow.HasOpenInstances<SpellStudioWindow>();
            SpellStudioWindow window = EditorWindow.GetWindow<SpellStudioWindow>();
            if (!isOpen)
            {
                window.position = new Rect(80f, 80f, 1280f, 800f);
            }
            window.Show();
        }

        public static SpellStudioWindow OpenPreset(SpellStudioPreset preset)
        {
            Open();
            SpellStudioWindow window = EditorWindow.GetWindow<SpellStudioWindow>();
            if (preset != null)
            {
                window.Select(preset);
            }

            window.Focus();
            return window;
        }

        // Every open studio showing the preset reads it again
        public static void NotifyPresetChanged(SpellStudioPreset preset)
        {
            foreach (SpellStudioWindow window in Resources.FindObjectsOfTypeAll<SpellStudioWindow>())
            {
                if (window.selected == preset)
                {
                    window.ReadSelection();
                }
            }
        }

        [OnOpenAsset]
        static bool OnOpenAsset(EntityId entityId, int line)
        {
            SpellStudioPreset preset = EditorUtility.EntityIdToObject(entityId) as SpellStudioPreset;
            if (preset == null)
            {
                return false;
            }

            OpenPreset(preset);
            return true;
        }
    }
}
