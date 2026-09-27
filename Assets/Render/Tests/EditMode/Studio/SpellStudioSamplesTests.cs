using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using HealerLike.Render.Grammar;
using HealerLike.Render.Spells;

namespace HealerLike.Render.Studio.Editor
{

public class SpellStudioSamplesTests
{
    readonly List<Object> _objects = new List<Object>();
    EffectVocabulary _vocabulary;

    [SetUp]
    public void SetUp()
    {
        _vocabulary = RenderTestAssets.LoadEffectVocabulary();
    }

    [TearDown]
    public void TearDown()
    {
        foreach (Object trackedObject in _objects)
        {
            if (trackedObject != null)
            {
                Object.DestroyImmediate(trackedObject);
            }
        }
        _objects.Clear();
    }

    SpellStudioPreset Build(int index)
    {
        SpellStudioPreset preset = SpellStudioSamples.Build(_vocabulary, index);
        _objects.Add(preset);
        return preset;
    }

    [TestCase(0, EffectKey.Rise, EffectFamily.Heal, EffectTempo.Once)]
    [TestCase(1, EffectKey.Drips, EffectFamily.Rot, EffectTempo.PerPeriod)]
    [TestCase(2, EffectKey.Plates, EffectFamily.Boon, EffectTempo.ForDuration)]
    [TestCase(3, EffectKey.Beam, EffectFamily.Damage, EffectTempo.ForDuration)]
    [TestCase(4, EffectKey.ManaUp, EffectFamily.Heal, EffectTempo.Once)]
    [TestCase(5, EffectKey.Burst, EffectFamily.Damage, EffectTempo.Once)]
    public void Build_AuthoredSample_OwnsItsEntryAndComposesAsAuthored(int index, EffectKey element,
        EffectFamily family, EffectTempo tempo)
    {
        SpellStudioPreset preset = Build(index);

        EffectRecipe recipe = preset.Compose();

        Assert.AreEqual(SpellStudioSamples.NameAt(index), preset.displayName);
        Assert.IsNotEmpty(preset.description);
        Assert.IsTrue(preset.overrideEntry);
        Assert.AreEqual(element, recipe.element);
        Assert.AreEqual(family, recipe.family);
        Assert.AreEqual(tempo, recipe.tempo);
        Assert.AreEqual(preset.colour, recipe.colour);
        Assert.AreEqual(EffectComposer.Count(preset.entry, preset.stacks, preset.charges, preset.amount), recipe.count);
        Assert.IsEmpty(SpellPresetValidator.Validate(preset));
    }

    [TestCase(6, SpellStudioMode.GrammarChannels, EffectKey.Stalks)]
    [TestCase(7, SpellStudioMode.GrammarChannels, EffectKey.Plates)]
    [TestCase(8, SpellStudioMode.GameplayHandler, EffectKey.Press)]
    public void Build_LinkedSample_KeepsTheVocabularysEntry(int index, SpellStudioMode mode, EffectKey element)
    {
        SpellStudioPreset preset = Build(index);

        Assert.AreEqual(mode, preset.mode);
        Assert.IsFalse(preset.overrideEntry);
        Assert.IsFalse(preset.overrideColour);
        Assert.AreEqual(element, preset.Compose().element);
    }

    [Test]
    public void Build_SameSampleTwice_SharesNoArrayWithTheVocabulary()
    {
        SpellStudioPreset first = Build(0);
        SpellStudioPreset second = Build(0);
        ElementEntry source = _vocabulary.entries[EffectKey.Rise];
        Vector3 position = source.parts[0].position;

        first.entry.parts[0].position = new Vector3(12f, 13f, 14f);

        Assert.AreNotSame(source.parts, first.entry.parts);
        Assert.AreNotSame(source.criticalRings, first.entry.criticalRings);
        Assert.AreEqual(position, source.parts[0].position);
        Assert.AreEqual(position, second.entry.parts[0].position);
    }

    [Test]
    public void Build_NoVocabularyOrAnEmptyOne_ReturnsNull()
    {
        EffectVocabulary empty = ScriptableObject.CreateInstance<EffectVocabulary>();
        _objects.Add(empty);

        Assert.IsNull(SpellStudioSamples.Build(null, 0));
        Assert.IsNull(SpellStudioSamples.Build(empty, 0));
    }

    [Test]
    public void NameAt_OutsideTheSamples_LogsAndReturnsNull()
    {
        LogAssert.Expect(LogType.Error, "[SpellStudioSamples] No sample at 9");

        Assert.IsNull(SpellStudioSamples.NameAt(SpellStudioSamples.Count));
    }

    [TestCase(EffectKey.Rise, EffectFamily.Heal)]
    [TestCase(EffectKey.Press, EffectFamily.Bane)]
    [TestCase(EffectKey.Bud, EffectFamily.Boon)]
    [TestCase(EffectKey.Beam, EffectFamily.Damage)]
    public void Family_ShownAlone_ReadsInItsOwnFamily(EffectKey element, EffectFamily family)
    {
        Assert.AreEqual(family, SpellStudioSamples.Family(element));
    }
}

}
