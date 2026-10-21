using System;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using HealerLike.Render.Creatures;
using HealerLike.Render.Grammar;
using Object = UnityEngine.Object;

namespace HealerLike.Render.Studio.Editor
{

public class CreatureStudioMenuTests
{
    readonly StudioPrefsBackup _prefs = new StudioPrefsBackup();
    CreatureRecipe _recipe;
    string _path;

    [SetUp]
    public void SetUp()
    {
        _prefs.Save();
        _recipe = CreatureStudioAuthoring.BuildSample(1);
        _path = "Assets/__CreatureStudioMenu_" + Guid.NewGuid().ToString("N") + ".asset";
    }

    [TearDown]
    public void TearDown()
    {
        foreach (CreatureStudioWindow window in Resources.FindObjectsOfTypeAll<CreatureStudioWindow>())
        {
            window.Close();
        }

        AssetDatabase.DeleteAsset(_path);
        Object.DestroyImmediate(_recipe);
        _prefs.Restore();
    }

    [Test]
    public void OpenRecipe_OutsideRecipe_SelectsItInPartsMode()
    {
        CreatureStudioWindow window = CreatureStudioMenu.OpenRecipe(_recipe);

        Assert.AreSame(_recipe, window.selected);
        Assert.IsFalse(window.isGrammarMode);
    }

    [Test]
    public void OpenGrammar_SavedPreset_SelectsItInGrammarMode()
    {
        CreatureGrammarPreset preset = CreatureGrammarSamples.Build(3);
        AssetDatabase.CreateAsset(preset, _path);

        CreatureStudioWindow window = CreatureStudioMenu.OpenGrammar(preset);

        Assert.AreSame(preset, window.grammar.selected);
        Assert.IsTrue(window.isGrammarMode);
        Assert.AreEqual(LookSide.Stone, window.previewSide);
    }
}

}
