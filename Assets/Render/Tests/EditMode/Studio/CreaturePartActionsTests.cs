using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using HealerLike.Render.Creatures;
using HealerLike.Render.Grammar;

namespace HealerLike.Render.Studio.Editor
{

public class CreaturePartActionsTests
{
    readonly StudioPrefsBackup _prefs = new StudioPrefsBackup();
    CreatureStudioWindow _window;
    CreaturePartActions _actions;
    Object _announced;

    [SetUp]
    public void SetUp()
    {
        _prefs.Save();
        _window = ScriptableObject.CreateInstance<CreatureStudioWindow>();
        _window.SwitchToParts(_window.partsSelection);
        _actions = _window.parts.actions;
        RenderGrammarLibraryWindow.OnAssetChanged.AddListener(OnAssetChanged);
    }

    [TearDown]
    public void TearDown()
    {
        RenderGrammarLibraryWindow.OnAssetChanged.RemoveListener(OnAssetChanged);
        Object.DestroyImmediate(_window);
        _prefs.Restore();
    }

    void OnAssetChanged(Object asset)
    {
        _announced = asset;
    }

    [Test]
    public void AddPart_SelectedRoot_AddsAChildAndSelectsIt()
    {
        CreatureRecipe recipe = _window.selected;
        int count = recipe.parts.Length;

        _actions.AddPart();

        Assert.AreEqual(count + 1, recipe.parts.Length);
        Assert.AreEqual(count, _window.parts.selectedPart);
        Assert.AreEqual(0, recipe.parts[count].parent);
    }

    [Test]
    public void RemovePart_AfterAdding_LeavesAValidRecipe()
    {
        CreatureRecipe recipe = _window.selected;
        int count = recipe.parts.Length;
        _actions.AddPart();

        _actions.RemovePart();

        Assert.AreEqual(count, recipe.parts.Length);
        Assert.AreEqual(0, _window.parts.selectedPart);
        Assert.IsEmpty(CreatureStudioAuthoring.Validate(recipe));
    }

    [Test]
    public void AddPart_UnsavedDraft_AnnouncesItToTheOtherStudios()
    {
        CreatureRecipe recipe = _window.selected;

        _actions.AddPart();

        Assert.AreSame(recipe, _announced);
        Assert.IsFalse(AssetDatabase.Contains(recipe));
    }

    [Test]
    public void DuplicateRecipe_StoneSurface_OpensTheCopyOnTheSameSurface()
    {
        CreatureRecipe source = _window.selected;
        _window.manualSurface = LookSide.Stone;

        _actions.DuplicateRecipe();

        Assert.AreNotSame(source, _window.selected);
        Assert.AreEqual(source.name + " copy", _window.selected.name);
        Assert.AreEqual(LookSide.Stone, _window.manualSurface);
    }

    [Test]
    public void AddArm_SelectedRoot_AddsAnArmAndSelectsIt()
    {
        CreatureRecipe recipe = _window.selected;
        int count = recipe.arms.Length;

        _actions.AddArm();

        Assert.AreEqual(count + 1, recipe.arms.Length);
        Assert.AreEqual(count, _window.parts.selectedArm);
    }
}

}
