using System;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using HealerLike.Render.Creatures;
using HealerLike.Render.Spells;
using Object = UnityEngine.Object;

namespace HealerLike.Render.Studio.Editor
{

public class SpellStudioPublishingTests
{
    SpellStudioPreset _preset;
    EffectVocabulary _vocabulary;
    string _copyPath;
    string _otherPath;

    // An authored Burst shape with its own colour, so it publishes without a palette
    [SetUp]
    public void SetUp()
    {
        _vocabulary = ScriptableObject.CreateInstance<EffectVocabulary>();
        _preset = ScriptableObject.CreateInstance<SpellStudioPreset>();
        _preset.vocabulary = _vocabulary;
        _preset.element = EffectElement.Burst;
        _preset.overrideEntry = true;
        _preset.overrideColour = true;
        _preset.entry = new ElementEntry { cycleSeconds = 1.2f, motion = EffectMotionKind.Rise,
            parts = new LookPart[] { new LookPart { id = "Authored", size = Vector3.one } } };
        string suffix = Guid.NewGuid().ToString("N");
        _copyPath = "Assets/__SpellStudioSavedCopy_" + suffix + ".asset";
        _otherPath = "Assets/__SpellStudioUnrelated_" + suffix + ".asset";
    }

    [TearDown]
    public void TearDown()
    {
        Undo.ClearUndo(_vocabulary);
        AssetDatabase.DeleteAsset(_copyPath);
        AssetDatabase.DeleteAsset(_otherPath);
        Object.DestroyImmediate(_preset);
        Object.DestroyImmediate(_vocabulary);
    }

    [Test]
    public void PublishEntry_AuthoredShape_CopiesItIntoTheVocabulary()
    {
        Assert.IsTrue(SpellStudioPublishing.PublishEntry(_preset));
        ElementEntry published = _vocabulary.elements[EffectElement.Burst];

        _preset.entry.parts[0].size = Vector3.zero;

        Assert.AreEqual(EffectMotionKind.Rise, published.motion);
        Assert.AreEqual(1.2f, published.cycleSeconds);
        Assert.AreEqual(Vector3.one, published.parts[0].size);
    }

    [Test]
    public void PublishEntry_OtherElementPresent_LeavesItAlone()
    {
        ElementEntry other = new ElementEntry { cycleSeconds = 7f };
        _vocabulary.elements[EffectElement.Orbit] = other;

        SpellStudioPublishing.PublishEntry(_preset);

        Assert.AreSame(other, _vocabulary.elements[EffectElement.Orbit]);
    }

    [Test]
    public void PublishEntry_NoDestination_ReturnsFalse()
    {
        _preset.vocabulary = null;

        Assert.IsFalse(SpellStudioPublishing.PublishEntry(_preset));
        Assert.IsFalse(SpellStudioPublishing.PublishEntry(null));
    }

    [Test]
    public void PublishEntry_Undone_PutsBackTheSharedEntry()
    {
        _vocabulary.elements[EffectElement.Burst] = new ElementEntry { cycleSeconds = 8f };
        // Odin keeps the dictionary in its serialized data, which has to be current before the snapshot
        ((ISerializationCallbackReceiver)_vocabulary).OnBeforeSerialize();
        Undo.IncrementCurrentGroup();

        SpellStudioPublishing.PublishEntry(_preset);
        Undo.FlushUndoRecordObjects();
        Undo.PerformUndo();

        Assert.AreEqual(8f, _vocabulary.elements[EffectElement.Burst].cycleSeconds);
    }

    [Test]
    public void SaveCopy_OtherAssetDirty_SavesOnlyTheCopy()
    {
        SpellStudioPreset other = ScriptableObject.CreateInstance<SpellStudioPreset>();
        other.displayName = "Original disk value";
        AssetDatabase.CreateAsset(other, _otherPath);
        AssetDatabase.SaveAssetIfDirty(other);
        other.displayName = "Unrelated unsaved edit";
        EditorUtility.SetDirty(other);
        _preset.displayName = "Saved copy";

        SpellStudioPreset copy = SpellStudioPublishing.SaveCopy(_preset, _copyPath);

        Assert.IsTrue(AssetDatabase.Contains(copy));
        Assert.IsFalse(EditorUtility.IsDirty(copy));
        Assert.IsTrue(EditorUtility.IsDirty(other));
        Assert.AreEqual("Saved copy", copy.displayName);
        StringAssert.Contains("Original disk value", File.ReadAllText(_otherPath));
    }
}

}
