using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using HealerLike.Render.Creatures;
using HealerLike.Render.Grammar;

namespace HealerLike.Render.Studio.Editor
{

public class CreatureStudioWindowTests
{
    readonly StudioPrefsBackup _prefs = new StudioPrefsBackup();
    CreatureStudioWindow _window;

    [SetUp]
    public void SetUp()
    {
        _prefs.Save();
        _window = ScriptableObject.CreateInstance<CreatureStudioWindow>();
    }

    [TearDown]
    public void TearDown()
    {
        if (_window != null)
        {
            Object.DestroyImmediate(_window);
        }
        _prefs.Restore();
    }

    // Closes the window, keeping its drafts, and opens a new one on them in parts mode
    void ReopenInParts()
    {
        Object.DestroyImmediate(_window);
        _window = ScriptableObject.CreateInstance<CreatureStudioWindow>();
        _window.SwitchToParts(_window.partsSelection);
    }

    [Test]
    public void OnEnable_FirstOpen_OpensInGrammarModeOnTheComposedRecipe()
    {
        Assert.IsTrue(_window.isGrammarMode);
        Assert.AreSame(_window.grammar.output, _window.selected);
        Assert.AreSame(_window.drafts.items[0], _window.partsSelection);
    }

    [Test]
    public void SwitchToParts_FirstDraft_SelectsItAndItsSurface()
    {
        _window.SwitchToParts(null);

        Assert.IsFalse(_window.isGrammarMode);
        Assert.AreSame(_window.drafts.items[0], _window.selected);
        Assert.AreEqual(LookSide.Plant, _window.manualSurface);
    }

    [Test]
    public void SwitchToGrammar_FromParts_RemembersTheRecipeToReturnTo()
    {
        _window.SwitchToParts(_window.drafts.items[2]);

        _window.SwitchToGrammar();

        Assert.IsTrue(_window.isGrammarMode);
        Assert.AreSame(_window.drafts.items[2], _window.partsSelection);
        Assert.AreSame(_window.grammar.output, _window.selected);
    }

    [Test]
    public void SwitchToParts_RememberedStoneSurface_SurvivesReopening()
    {
        CreatureRecipe healer = _window.drafts.items[0];
        _window.drafts.RememberSurface(healer, LookSide.Stone);
        _window.SwitchToParts(_window.drafts.items[1]);
        Assert.AreEqual(LookSide.Plant, _window.manualSurface);

        _window.SwitchToParts(healer);
        Assert.AreEqual(LookSide.Stone, _window.manualSurface);
        ReopenInParts();

        Assert.AreEqual(LookSide.Stone, _window.manualSurface);
    }

    [Test]
    public void BakeGrammar_ComposedRecipe_OpensAnIndependentPartsDraft()
    {
        CreatureRecipe output = _window.grammar.output;
        string id = output.parts[0].id;

        _window.BakeGrammar();
        _window.selected.parts[0].id = "Independent authored edit";

        Assert.IsFalse(_window.isGrammarMode);
        Assert.AreNotSame(output, _window.selected);
        Assert.AreEqual(id, output.parts[0].id);
        Assert.Contains(_window.selected, _window.drafts.items);
        Assert.IsFalse(AssetDatabase.Contains(_window.selected));
    }

    [Test]
    public void BakeGrammar_Reopened_KeepsTheBakedDraftSelected()
    {
        _window.BakeGrammar();
        string name = _window.selected.name;
        _window.selected.parts[0].id = "Kept root";

        ReopenInParts();

        Assert.AreEqual(name, _window.selected.name);
        Assert.AreEqual("Kept root", _window.selected.parts[0].id);
    }
}

}
