using System;
using HealerLike.Render.Creatures;
using HealerLike.Render.Grammar;
using HealerLike.Render.Spells.Editor;
using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Spells
{
    public class SpellPolishVocabularyTests
    {
        EffectVocabulary vocabulary;

        [SetUp]
        public void SetUp()
        {
            vocabulary = ScriptableObject.CreateInstance<EffectVocabulary>();
            SpellPolishVocabulary.Apply(vocabulary);
        }

        [TearDown]
        public void TearDown() { UnityEngine.Object.DestroyImmediate(vocabulary); }

        [Test]
        public void Apply_EveryFamilyHasReadableDurationAndBoundedProceduralComposition()
        {
            foreach (EffectKey element in Enum.GetValues(typeof(EffectKey)))
            {
                ElementEntry entry = vocabulary.GetEntry(element);
                Assert.That(entry.cycleSeconds, Is.InRange(1f, 3f), element.ToString());
                Assert.That(entry.presentation.enabled, Is.True, element.ToString());
                Assert.That(entry.parts.Length + entry.stackBeads.Length + entry.criticalRings.Length
                    + entry.sideRim.Length, Is.LessThanOrEqualTo(24), element.ToString());
                Assert.That(EffectComposer.Shapes(entry), Is.GreaterThanOrEqualTo(entry.minCount));
                var identifiers = new System.Collections.Generic.HashSet<string>();
                foreach (LookPart part in entry.parts)
                {
                    Assert.That(identifiers.Add(part.id), Is.True, element + ": " + part.id);
                    Assert.That(part.shape.isProcedural && part.shape.IsValid(), Is.True, part.id);
                    Assert.That(part.size.x > 0 && part.size.y > 0 && part.size.z > 0, Is.True, part.id);
                }
            }
        }

        [Test]
        public void Apply_RetainsArtistPaletteAndCustomGrammarMapping()
        {
            LookPalette palette = ScriptableObject.CreateInstance<LookPalette>();
            try
            {
                vocabulary.palette = palette;
                var cell = new EffectCell(EffectOperation.Heal, EffectAspect.Defence);
                var custom = new EffectCellEntry(EffectKey.Bud, EffectKey.Stalks, true);
                vocabulary.legacyTable[cell] = custom;
                SpellPolishVocabulary.Apply(vocabulary);
                Assert.That(vocabulary.palette, Is.SameAs(palette));
                Assert.That(vocabulary.legacyTable[cell].once, Is.EqualTo(EffectKey.Bud));
                Assert.That(vocabulary.legacyTable[cell].periodic, Is.EqualTo(EffectKey.Stalks));
            }
            finally { UnityEngine.Object.DestroyImmediate(palette); }
        }

        [Test]
        public void Apply_RepeatedAuthoringIsStableAndDoesNotAccumulateParts()
        {
            string before = JsonUtility.ToJson(vocabulary.GetEntry(EffectKey.Stalks));
            SpellPolishVocabulary.Apply(vocabulary);
            // One entry per element, the eleven kinds included, and one kind cell per kind plus the Growth of a Bane
            // defence
            Assert.That(vocabulary.entries.Count, Is.EqualTo(Enum.GetValues(typeof(EffectKey)).Length));
            Assert.That(vocabulary.kinds.Count, Is.EqualTo(12));
            Assert.That(JsonUtility.ToJson(vocabulary.GetEntry(EffectKey.Stalks)), Is.EqualTo(before));
        }

        [Test]
        public void Apply_PresentationRolesAreAuthoredIndependentlyFromElementNames()
        {
            Assert.That(vocabulary.GetEntry(EffectKey.Burst).presentation.billboard, Is.True);
            Assert.That(vocabulary.GetEntry(EffectKey.Plates).presentation.isShield, Is.True);
            Assert.That(vocabulary.GetEntry(EffectKey.Bud).presentation.closesOverHead, Is.True);
            Assert.That(vocabulary.GetEntry(EffectKey.ManaUp).presentation.colourRole, Is.EqualTo(ColourRole.Mana));
            Assert.That(vocabulary.GetEntry(EffectKey.ManaDown).presentation.colourRole, Is.EqualTo(ColourRole.Mana));
            Assert.That(vocabulary.GetEntry(EffectKey.Beam).presentation.linkBeadSeconds, Is.GreaterThanOrEqualTo(1f));
        }

        [Test]
        public void Apply_WardPreventionUsesEnclosingPetalsWhileArmorKeepsPlates()
        {
            Assert.That(vocabulary.TryGetElement(EffectOperation.Ward, EffectAspect.Prevention,
                out EffectKey prevention), Is.True);
            Assert.That(prevention, Is.EqualTo(EffectKey.Bud));
            Assert.That(vocabulary.GetEntry(prevention).presentation.closesOverHead, Is.True);
            Assert.That(vocabulary.GetEntry(EffectKey.Plates).presentation.isShield, Is.True);
        }

        [Test]
        public void Apply_SingleArmorChargeFacesTheGameplayCamera()
        {
            ElementEntry armor = vocabulary.GetEntry(EffectKey.Plates);
            Assert.That(EffectComposer.Count(armor, 1, 1, 0), Is.EqualTo(1));
            Assert.That(armor.parts[0].position.z, Is.LessThan(-1f));
            Assert.That(armor.parts[0].position.x, Is.EqualTo(0).Within(.0001f));
        }

        [Test]
        public void Apply_EverySpellMovesAndColoursTheGround()
        {
            foreach (EffectKey element in Enum.GetValues(typeof(EffectKey)))
            {
                ElementEntry entry = vocabulary.GetEntry(element);
                Assert.That(entry.ground, Is.Not.Null, element.ToString());
                Assert.That(entry.ground.hasState, Is.True, element.ToString());
                Assert.That(entry.ground.kick != 0 || entry.ground.hold != 0, Is.True, element.ToString());
                Assert.That(entry.groundRadius, Is.GreaterThan(0));
                Assert.That(entry.groundStrength, Is.InRange(.1f, 1f));
            }
        }
    }
}
