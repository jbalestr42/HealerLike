using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using HealerLike.Render.Grammar;

namespace HealerLike.Render.Studio.Editor
{

public class SpellStudioWindowTests
{
    readonly StudioPrefsBackup _prefs = new StudioPrefsBackup();
    SpellStudioWindow _window;

    [SetUp]
    public void SetUp()
    {
        _prefs.Save();
        _window = ScriptableObject.CreateInstance<SpellStudioWindow>();
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

    // Closes the window, keeping its drafts, and opens a new one on them
    void Reopen()
    {
        Object.DestroyImmediate(_window);
        _window = ScriptableObject.CreateInstance<SpellStudioWindow>();
    }

    [Test]
    public void OnEnable_FirstOpen_SelectsTheFirstDraft()
    {
        Assert.AreSame(_window.drafts.items[0], _window.selected);
        Assert.NotNull(_window.serialized);
        Assert.AreEqual(0f, _window.timeline.time);
    }

    [Test]
    public void Duplicate_SelectedDraft_SelectsTheDetachedCopy()
    {
        SpellStudioPreset source = _window.selected;
        source.CaptureEntry();

        _window.Duplicate();

        Assert.AreNotSame(source, _window.selected);
        Assert.AreEqual(SpellStudioDrafts.Label(source) + " copy", _window.selected.displayName);
        Assert.IsFalse(AssetDatabase.Contains(_window.selected));
    }

    [Test]
    public void NewDraft_Reopened_SelectsTheSameDraft()
    {
        int count = _window.drafts.items.Count;
        _window.NewDraft();
        _window.selected.displayName = "Selected custom draft";

        Reopen();

        Assert.AreEqual("Selected custom draft", _window.selected.displayName);
        Assert.AreEqual(count + 1, _window.drafts.items.Count);
    }

    [Test]
    public void NewGrammarDraft_FromTheLibrary_SelectsAGrammarPreset()
    {
        _window.NewGrammarDraft();

        Assert.AreEqual(SpellStudioMode.GrammarChannels, _window.selected.mode);
        Assert.AreEqual("Defence boon", _window.selected.displayName);
    }

    [Test]
    public void Duration_ImpactThenStatus_FollowsTheSelectedPreset()
    {
        _window.selected.tempo = EffectTempo.ForDuration;
        _window.selected.durationSeconds = 5f;

        Assert.AreEqual(5f, _window.duration);
    }
}

}
