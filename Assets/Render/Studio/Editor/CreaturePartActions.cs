using UnityEditor;
using UnityEngine;
using HealerLike.Render.Creatures;

namespace HealerLike.Render.Studio.Editor
{
    // The parts inspector's edits on the selected recipe, each one undoable and announced to the other studios:
    // unsaved drafts included, since the spell studio may be previewing one
    public class CreaturePartActions
    {
        CreatureStudioWindow _window;
        CreaturePartsInspector _inspector;

        public void Init(CreatureStudioWindow window, CreaturePartsInspector inspector)
        {
            _window = window;
            _inspector = inspector;
        }

        // Field edits made through the serialized recipe
        public void ApplyEdits()
        {
            _window.serialized.ApplyModifiedProperties();
            _window.RefreshPreview();
            RenderGrammarLibraryWindow.OnAssetChanged.Invoke(_window.selected);
        }

        // A child of the selected part, or the root of an empty recipe
        public void AddPart()
        {
            CreatureRecipe recipe = Begin("Add creature part");
            int parent = -1;
            if (recipe.parts != null && recipe.parts.Length > 0)
            {
                parent = _inspector.selectedPart;
            }

            int index = CreatureRecipeEdits.AddPart(recipe, parent, _inspector.newPrimitive);
            if (index >= 0)
            {
                _inspector.selectedPart = index;
            }
            else
            {
                _window.ShowNotification(new GUIContent("Repair the recipe checks before adding parts"));
            }
            Finish();
        }

        public void DuplicatePart()
        {
            CreatureRecipe recipe = Begin("Duplicate creature part");
            int index = CreatureRecipeEdits.DuplicatePart(recipe, _inspector.selectedPart);
            if (index >= 0)
            {
                _inspector.selectedPart = index;
            }
            Finish();
        }

        public void RemovePart()
        {
            CreatureRecipe recipe = Begin("Remove creature subtree");
            if (CreatureRecipeEdits.RemovePart(recipe, _inspector.selectedPart))
            {
                _inspector.selectedPart = 0;
            }
            Finish();
        }

        public void AddArm()
        {
            CreatureRecipe recipe = Begin("Add creature arm");
            int index = CreatureRecipeEdits.AddArm(recipe, _inspector.selectedPart);
            if (index >= 0)
            {
                _inspector.selectedArm = index;
            }
            else
            {
                _window.ShowNotification(new GUIContent("Select a valid body part before adding an arm"));
            }
            Finish();
        }

        public void RemoveArm()
        {
            CreatureRecipe recipe = Begin("Remove creature arm");
            CreatureRecipeEdits.RemoveArm(recipe, _inspector.selectedArm);
            _inspector.selectedArm = 0;
            Finish();
        }

        public void RebuildArm()
        {
            CreatureRecipe recipe = Begin("Rebuild arm rest pose");
            if (!CreatureRecipeEdits.RebuildArmRestPose(recipe, _inspector.selectedArm))
            {
                _window.ShowNotification(new GUIContent("Unable to rebuild this arm"));
            }
            Finish();
        }

        // A detached copy as a new draft, previewed on the same surface
        public void DuplicateRecipe()
        {
            CreatureRecipe selected = _window.selected;
            if (selected == null)
            {
                return;
            }

            CreatureRecipe draft = CreatureStudioAuthoring.Clone(selected);
            draft.name = selected.name + " copy";
            _window.drafts.Add(draft);
            _window.drafts.RememberSurface(draft, _window.manualSurface);
            _window.SwitchToParts(draft);
        }

        CreatureRecipe Begin(string undoName)
        {
            _window.serialized.ApplyModifiedProperties();
            Undo.RecordObject(_window.selected, undoName);
            return _window.selected;
        }

        void Finish()
        {
            EditorUtility.SetDirty(_window.selected);
            _window.serialized.Update();
            _window.RefreshPreview();
            _window.Repaint();
            RenderGrammarLibraryWindow.OnAssetChanged.Invoke(_window.selected);
        }
    }
}
