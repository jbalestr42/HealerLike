using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using HealerLike.Render.Spells;

namespace HealerLike.Render.Studio.Editor
{

public class SpellStudioDraftsTests
{
    readonly StudioPrefsBackup _prefs = new StudioPrefsBackup();
    readonly List<SpellStudioDrafts> _drafts = new List<SpellStudioDrafts>();

    [SetUp]
    public void SetUp()
    {
        _prefs.Save();
    }

    [TearDown]
    public void TearDown()
    {
        foreach (SpellStudioDrafts drafts in _drafts)
        {
            drafts.Dispose();
        }
        _drafts.Clear();
        _prefs.Restore();
    }

    SpellStudioDrafts CreateDrafts()
    {
        SpellStudioDrafts drafts = new SpellStudioDrafts();
        drafts.Init();
        _drafts.Add(drafts);
        return drafts;
    }

    static int Count(List<SpellStudioPreset> presets, SpellStudioMode mode)
    {
        int count = 0;
        foreach (SpellStudioPreset preset in presets)
        {
            if (preset.mode == mode)
            {
                count++;
            }
        }
        return count;
    }

    static int CountDrafts()
    {
        int count = 0;
        foreach (SpellStudioPreset draft in Resources.FindObjectsOfTypeAll<SpellStudioPreset>())
        {
            if (draft.hideFlags == HideFlags.HideAndDontSave)
            {
                count++;
            }
        }
        return count;
    }

    [Test]
    public void Init_FirstOpen_DraftsEveryElementAndTheLinkedSamples()
    {
        SpellStudioDrafts drafts = CreateDrafts();

        int elementCount = Enum.GetValues(typeof(EffectElement)).Length;
        Assert.AreEqual(elementCount + 3, drafts.items.Count);
        Assert.AreEqual(elementCount, Count(drafts.items, SpellStudioMode.AuthoredElement));
        Assert.AreEqual(2, Count(drafts.items, SpellStudioMode.GrammarChannels));
        Assert.AreEqual(1, Count(drafts.items, SpellStudioMode.GameplayHandler));
        foreach (SpellStudioPreset draft in drafts.items)
        {
            Assert.NotNull(draft.Compose(), draft.displayName);
            Assert.IsFalse(AssetDatabase.Contains(draft));
        }
    }

    [Test]
    public void Duplicate_CapturedEntry_CopiesTheShapeAndSharesTheVocabulary()
    {
        SpellStudioDrafts drafts = CreateDrafts();
        SpellStudioPreset source = drafts.items[0];
        source.CaptureEntry();
        string id = source.entry.parts[0].id;

        SpellStudioPreset copy = drafts.Duplicate(source);
        copy.entry.parts[0].id = "Changed copy";

        Assert.AreEqual(id, source.entry.parts[0].id);
        Assert.AreSame(source.vocabulary, copy.vocabulary);
        Assert.AreEqual(SpellStudioDrafts.Label(source) + " copy", copy.displayName);
        Assert.Contains(copy, drafts.items);
    }

    [Test]
    public void Persist_EditedSelectedDraft_RestoresItWithItsPartsAndVocabulary()
    {
        SpellStudioDrafts drafts = CreateDrafts();
        SpellStudioPreset draft = drafts.NewDraft(null);
        draft.displayName = "Persistence test";
        draft.CaptureEntry();
        draft.entry.parts[0].id = "Persisted part";

        drafts.Persist(draft);
        SpellStudioDrafts reopened = CreateDrafts();

        SpellStudioPreset restored = reopened.restoredSelection;
        Assert.AreEqual("Persistence test", restored.displayName);
        Assert.AreEqual("Persisted part", restored.entry.parts[0].id);
        Assert.AreSame(draft.vocabulary, restored.vocabulary);
        Assert.AreEqual(drafts.items.Count, reopened.items.Count);
    }

    [Test]
    public void NewGrammarDraft_NoSelection_IsAHeldDefenceBoonOnTheShippedVocabulary()
    {
        SpellStudioDrafts drafts = CreateDrafts();

        SpellStudioPreset draft = drafts.NewGrammarDraft(null);

        Assert.AreEqual(SpellStudioMode.GrammarChannels, draft.mode);
        Assert.AreEqual(EffectElement.Plates, draft.resolvedElement);
        Assert.AreEqual(RenderTestAssets.LoadEffectVocabulary(), draft.vocabulary);
        Assert.NotNull(draft.spellLooks);
    }

    [Test]
    public void NewHandlerDraft_ShippedHandler_IsNamedByItsFolder()
    {
        SpellStudioDrafts drafts = CreateDrafts();
        ABuffHandlerFactory handler = AssetDatabase.LoadAssetAtPath<ABuffHandlerFactory>(
            StudioTestAssets.OpposingHandlerPath);

        SpellStudioPreset draft = drafts.NewHandlerDraft(null, handler);

        Assert.AreEqual(SpellStudioMode.GameplayHandler, draft.mode);
        Assert.AreEqual("Multi Target Reduce Damage / BuffHandlerFactory", draft.displayName);
        Assert.IsTrue(draft.isSameSide);
    }

    [Test]
    public void Init_CorruptedPrefs_LogsAndStartsFromTheDefaults()
    {
        string key = "HealerLike.SpellStudio.Drafts." + Application.dataPath;
        EditorPrefs.SetString(key, "{\"items\":[{\"json\":\"");
        LogAssert.Expect(LogType.Error, "[StudioPrefs] Dropped the unreadable drafts under " + key);

        SpellStudioDrafts drafts = CreateDrafts();

        Assert.Greater(drafts.items.Count, 0);
    }
    [TestCase("{\"items\":[null]}")]
    [TestCase("{\"items\":[{\"json\":\"{}\"},{\"json\":\"{broken}\"}]}")]
    public void Init_CorruptRecord_DiscardsPartialRestoreAndCreatesDefaults(string json)
    {
        string key = "HealerLike.SpellStudio.Drafts." + Application.dataPath;
        EditorPrefs.SetString(key, json);
        int before = CountDrafts();
        LogAssert.Expect(LogType.Error, "[StudioPrefs] Dropped the unreadable drafts under " + key);

        SpellStudioDrafts drafts = CreateDrafts();

        Assert.IsFalse(EditorPrefs.HasKey(key));
        Assert.Greater(drafts.items.Count, 0);
        Assert.AreEqual(before + drafts.items.Count, CountDrafts());
    }

}

}
