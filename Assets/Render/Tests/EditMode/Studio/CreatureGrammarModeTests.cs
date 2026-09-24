using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using HealerLike.Render.Creatures;
using HealerLike.Render.Grammar;
using Object = UnityEngine.Object;

namespace HealerLike.Render.Studio.Editor
{

public class CreatureGrammarModeTests
{
    readonly StudioPrefsBackup _prefs = new StudioPrefsBackup();
    readonly List<Object> _objects = new List<Object>();
    CreatureStudioWindow _window;
    string _path;

    [SetUp]
    public void SetUp()
    {
        _prefs.Save();
        _window = ScriptableObject.CreateInstance<CreatureStudioWindow>();
        _path = "Assets/__CreatureGrammarMode_" + Guid.NewGuid().ToString("N") + ".asset";
    }

    [TearDown]
    public void TearDown()
    {
        if (_window != null)
        {
            Object.DestroyImmediate(_window);
        }

        foreach (Object trackedObject in _objects)
        {
            if (trackedObject != null)
            {
                Object.DestroyImmediate(trackedObject);
            }
        }

        _objects.Clear();
        AssetDatabase.DeleteAsset(_path);
        _prefs.Restore();
    }

    T Track<T>(T instance) where T : Object
    {
        _objects.Add(instance);
        return instance;
    }

    void Reopen()
    {
        Object.DestroyImmediate(_window);
        _window = ScriptableObject.CreateInstance<CreatureStudioWindow>();
    }

    [Test]
    public void Regenerate_ChangedHead_ReplacesTheComposedRecipe()
    {
        CreatureGrammarMode grammar = _window.grammar;
        CreatureRecipe before = grammar.output;
        grammar.selected.head = HeadKind.Spear;

        grammar.Regenerate();

        Assert.AreNotSame(before, grammar.output);
        Assert.IsTrue(before == null); // the mode destroys the recipe it replaced
        Assert.IsEmpty(CreatureStudioAuthoring.Validate(grammar.output));
        Assert.AreSame(grammar.output, _window.selected);
    }

    [Test]
    public void Regenerate_PresetWithErrors_ShowsNoRecipe()
    {
        CreatureGrammarMode grammar = _window.grammar;
        grammar.selected.vocabulary = null;

        grammar.Regenerate();

        Assert.IsNull(grammar.output);
        Assert.IsNull(_window.selected);
        Assert.IsNotEmpty(grammar.warnings);
    }

    [Test]
    public void Dispose_EditedDraft_RestoresChannelsAndVocabularyOnReopening()
    {
        CreatureGrammarPreset preset = _window.grammar.selected;
        preset.displayName = "Persistent grammar";
        preset.head = HeadKind.Arch;
        preset.mass = MassBand.Heavy;
        LookVocabulary vocabulary = preset.vocabulary;

        Reopen();

        preset = _window.grammar.selected;
        Assert.AreEqual("Persistent grammar", preset.displayName);
        Assert.AreEqual(HeadKind.Arch, preset.head);
        Assert.AreEqual(MassBand.Heavy, preset.mass);
        Assert.AreSame(vocabulary, preset.vocabulary);
        Assert.NotNull(_window.grammar.output);
    }

    [Test]
    public void Dispose_OtherOverrideTable_RestoresItOnReopening()
    {
        CreatureLooks table = ScriptableObject.CreateInstance<CreatureLooks>();
        AssetDatabase.CreateAsset(table, _path);
        _window.grammar.creatureLooks = table;

        Reopen();

        Assert.AreSame(table, _window.grammar.creatureLooks);
    }

    [Test]
    public void Bake_SavedPreset_LeavesThePresetAsSaved()
    {
        CreatureGrammarPreset preset = Object.Instantiate(_window.grammar.selected);
        preset.hideFlags = HideFlags.None;
        preset.head = HeadKind.Pulse;
        AssetDatabase.CreateAsset(preset, _path);
        _window.SelectGrammar(preset);

        CreatureRecipe baked = Track(_window.grammar.Bake());

        Assert.AreSame(preset, _window.grammar.selected);
        Assert.AreEqual(HeadKind.Pulse, preset.head);
        Assert.AreEqual("Grammar draft baked", baked.name);
    }

    [Test]
    public void GetOverrideRecipe_AuthoredViewWithABuilder_ReturnsItsRecipe()
    {
        CreatureGrammarMode grammar = _window.grammar;
        EntityData entity = RenderTestAssets.LoadEntity("SoldierEntity");
        CreatureRecipe recipe = Track(RenderTestAssets.CreateRecipe());
        GameObject view = Track(new GameObject("Authored view"));
        RenderTestAssets.SetRecipe(view.AddComponent<CreatureBuilder>(), recipe, RenderTestAssets.LoadLookMaterial(),
            RenderTestAssets.LoadMeshes());
        CreatureLooks table = Track(ScriptableObject.CreateInstance<CreatureLooks>());
        table.entities[entity] = view;
        grammar.creatureLooks = table;
        grammar.selected.sourceEntity = entity;

        grammar.isOverrideShown = true;
        grammar.Regenerate();

        Assert.IsTrue(grammar.HasSourceOverride());
        Assert.AreSame(recipe, grammar.GetOverrideRecipe());
        Assert.AreSame(recipe, _window.selected); // the viewport auditions the game's view
        Assert.AreEqual(LookSide.Plant, grammar.previewSide);
    }

    [Test]
    public void Regenerate_OverrideShownWithoutAnAuthoredView_ShowsTheGrammarAgain()
    {
        CreatureGrammarMode grammar = _window.grammar;
        grammar.isOverrideShown = true;

        grammar.Regenerate();

        Assert.IsFalse(grammar.isOverrideShown);
        Assert.AreSame(grammar.output, _window.selected);
    }
}

}
