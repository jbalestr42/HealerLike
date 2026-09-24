using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
using HealerLike.Render.Creatures;

namespace HealerLike.Render.Studio.Editor
{
    // The ways into the creature studio: the menu, a recipe or grammar preset opened in the project or from its
    // inspector, and the notices those inspectors send when they change what a studio shows
    public static class CreatureStudioMenu
    {
        [MenuItem("Tools/Render/Creature Studio", false, 111)]
        [MenuItem("Tools/Render/Render Studio", false, 109)]
        public static void Open()
        {
            bool isOpen = EditorWindow.HasOpenInstances<CreatureStudioWindow>();
            CreatureStudioWindow window = EditorWindow.GetWindow<CreatureStudioWindow>();
            if (!isOpen)
            {
                window.position = new Rect(80f, 80f, 1360f, 850f);
            }
            window.Show();
        }

        public static CreatureStudioWindow OpenRecipe(CreatureRecipe recipe)
        {
            Open();
            CreatureStudioWindow window = EditorWindow.GetWindow<CreatureStudioWindow>();
            if (recipe != null)
            {
                window.SwitchToParts(recipe);
            }

            window.Focus();
            return window;
        }

        public static CreatureStudioWindow OpenGrammar(CreatureGrammarPreset preset)
        {
            Open();
            CreatureStudioWindow window = EditorWindow.GetWindow<CreatureStudioWindow>();
            if (preset != null)
            {
                window.SelectGrammar(preset);
            }
            else
            {
                window.SwitchToGrammar();
            }

            window.Focus();
            return window;
        }

        // Every open studio showing the recipe reads it again, and the other studios hear of it
        public static void NotifyRecipeChanged(CreatureRecipe recipe)
        {
            foreach (CreatureStudioWindow window in Resources.FindObjectsOfTypeAll<CreatureStudioWindow>())
            {
                if (window.selected == recipe)
                {
                    window.ReadSelection();
                }
            }
            RenderGrammarLibraryWindow.OnAssetChanged.Invoke(recipe);
        }

        public static void NotifyGrammarChanged(CreatureGrammarPreset preset)
        {
            foreach (CreatureStudioWindow window in Resources.FindObjectsOfTypeAll<CreatureStudioWindow>())
            {
                if (window.grammar.selected != preset)
                {
                    continue;
                }

                window.grammar.serialized.Update();
                if (window.isGrammarMode)
                {
                    window.grammar.Regenerate();
                }
            }
        }

        [OnOpenAsset]
        static bool OnOpenAsset(EntityId entityId, int line)
        {
            Object asset = EditorUtility.EntityIdToObject(entityId);
            if (asset is CreatureRecipe recipe)
            {
                OpenRecipe(recipe);
                return true;
            }

            if (asset is CreatureGrammarPreset preset)
            {
                OpenGrammar(preset);
                return true;
            }
            return false;
        }
    }
}
