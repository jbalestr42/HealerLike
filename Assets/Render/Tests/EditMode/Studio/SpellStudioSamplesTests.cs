using System;
using HealerLike.Render.Grammar;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using HealerLike.Render.Creatures;
using HealerLike.Render.Spells;

using HealerLike.Render.Studio.Editor;

namespace HealerLike.Render.Studio
{
    public class SpellStudioSamplesTests
    {
        [TestCase(0, EffectElement.Rise, EffectFamily.Heal, EffectTempo.Once)]
        [TestCase(1, EffectElement.Drips, EffectFamily.Rot, EffectTempo.PerPeriod)]
        [TestCase(2, EffectElement.Plates, EffectFamily.Boon, EffectTempo.ForDuration)]
        [TestCase(3, EffectElement.Beam, EffectFamily.Damage, EffectTempo.ForDuration)]
        [TestCase(4, EffectElement.ManaUp, EffectFamily.Heal, EffectTempo.Once)]
        [TestCase(5, EffectElement.Burst, EffectFamily.Damage, EffectTempo.Once)]
        public void Build_ProductionVocabularyProducesNamedConsistentEditableSample(int index, EffectElement element,
            EffectFamily family, EffectTempo tempo)
        {
            EffectVocabulary vocabulary = AssetDatabase.LoadAssetAtPath<EffectVocabulary>(SpellStudioSamples.VocabularyPath);
            Assert.NotNull(vocabulary);
            SpellStudioPreset preset = SpellStudioSamples.Build(vocabulary, index);
            try
            {
                Assert.NotNull(preset);
                Assert.AreEqual(SpellStudioSamples.NameAt(index), preset.displayName);
                Assert.IsNotEmpty(preset.description);
                Assert.AreEqual(element, preset.element);
                Assert.AreEqual(family, preset.family);
                Assert.AreEqual(tempo, preset.tempo);
                Assert.IsTrue(preset.overrideEntry);
                Assert.IsTrue(preset.overrideColour);
                EffectRecipe recipe = preset.Compose();
                Assert.NotNull(recipe);
                Assert.Greater(recipe.count, 0);
                Assert.AreEqual(element, recipe.element);
                Assert.AreEqual(family, recipe.family);
                Assert.AreEqual(tempo, recipe.tempo);
                Assert.AreEqual(preset.colour, recipe.colour);
                Assert.AreEqual(tempo == EffectTempo.PerPeriod ? preset.periodSeconds : preset.entry.cycleSeconds, recipe.cycleSeconds);
                Assert.AreEqual(EffectComposer.Count(preset.entry, preset.stacks, preset.charges, preset.amount), recipe.count);
                Assert.IsEmpty(preset.Validate());
            }
            finally { if (preset != null) UnityEngine.Object.DestroyImmediate(preset); }
        }

        [Test]
        public void Build_RepeatedSamplesDoNotShareEntryOrArraysWithVocabulary()
        {
            EffectVocabulary vocabulary = AssetDatabase.LoadAssetAtPath<EffectVocabulary>(SpellStudioSamples.VocabularyPath);
            SpellStudioPreset a = SpellStudioSamples.Build(vocabulary, 0);
            SpellStudioPreset b = SpellStudioSamples.Build(vocabulary, 0);
            try
            {
                ElementEntry source = vocabulary.elements[EffectElement.Rise];
                Vector3 sourcePosition = source.parts[0].position;
                Assert.AreNotSame(source, a.entry);
                Assert.AreNotSame(a.entry, b.entry);
                Assert.AreNotSame(source.parts, a.entry.parts);
                Assert.AreNotSame(source.criticalRings, a.entry.criticalRings);
                Assert.AreNotSame(source.stackBeads, a.entry.stackBeads);
                Assert.AreNotSame(source.sideRim, a.entry.sideRim);
                a.entry.parts[0].position = new Vector3(12f, 13f, 14f);
                Assert.AreEqual(sourcePosition, source.parts[0].position);
                Assert.AreEqual(sourcePosition, b.entry.parts[0].position);
            }
            finally
            {
                if (a != null) UnityEngine.Object.DestroyImmediate(a);
                if (b != null) UnityEngine.Object.DestroyImmediate(b);
            }
        }

        [Test]
        public void Build_MissingInputReturnsNullAndInvalidIndexIsExplicit()
        {
            Assert.IsNull(SpellStudioSamples.Build(null, 0));
            var empty = ScriptableObject.CreateInstance<EffectVocabulary>();
            try { Assert.IsNull(SpellStudioSamples.Build(empty, 0)); }
            finally { UnityEngine.Object.DestroyImmediate(empty); }
            Assert.Throws<ArgumentOutOfRangeException>(() => SpellStudioSamples.NameAt(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => SpellStudioSamples.Build(null, SpellStudioSamples.Count));
        }
    }
}
