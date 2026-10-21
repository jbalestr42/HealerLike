using System;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace HealerLike.Render.Studio.Editor
{

public class SpellStudioMenuTests
{
    readonly StudioPrefsBackup _prefs = new StudioPrefsBackup();
    SpellStudioPreset _preset;
    string _path;

    [SetUp]
    public void SetUp()
    {
        _prefs.Save();
        _preset = ScriptableObject.CreateInstance<SpellStudioPreset>();
        _path = "Assets/__SpellStudioMenu_" + Guid.NewGuid().ToString("N") + ".asset";
    }

    [TearDown]
    public void TearDown()
    {
        foreach (SpellStudioWindow window in Resources.FindObjectsOfTypeAll<SpellStudioWindow>())
        {
            window.Close();
        }

        AssetDatabase.DeleteAsset(_path);
        if (_preset != null)
        {
            Object.DestroyImmediate(_preset);
        }
        _prefs.Restore();
    }

    [Test]
    public void OpenPreset_UnsavedPreset_SelectsIt()
    {
        SpellStudioWindow window = SpellStudioMenu.OpenPreset(_preset);

        Assert.AreSame(_preset, window.selected);
    }

    [Test]
    public void OpenPreset_SavedPreset_SelectsTheAsset()
    {
        AssetDatabase.CreateAsset(_preset, _path);

        SpellStudioWindow window = SpellStudioMenu.OpenPreset(_preset);

        Assert.AreSame(_preset, window.selected);
        Assert.IsTrue(AssetDatabase.Contains(window.selected));
    }
}

}
