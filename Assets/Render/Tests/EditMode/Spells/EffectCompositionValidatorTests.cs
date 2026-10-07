using System;
using NUnit.Framework;
using UnityEngine;
using HealerLike.Render.Creatures;
using HealerLike.Render.Grammar;

namespace HealerLike.Render.Spells
{
    public class EffectCompositionValidatorTests
    {
        [Test]
        public void MissingElementAndNullDictionaries_ReturnErrorsWithoutLoggedLookup()
        {
            EffectVocabulary vocabulary = ScriptableObject.CreateInstance<EffectVocabulary>();
            try
            {
                vocabulary.legacyTable[new EffectCell(EffectOperation.Damage, EffectAspect.Offence)] =
                    new EffectCellEntry(EffectKey.Burst, EffectKey.Drips, true);
                Assert.IsFalse(EffectCompositionValidator.TryValidate(default, vocabulary, out string error));
                StringAssert.Contains("missing", error);
                vocabulary.entries = null;
                Assert.IsFalse(EffectCompositionValidator.TryValidate(default, vocabulary, out error));
                vocabulary.legacyTable = null;
                Assert.IsFalse(EffectCompositionValidator.TryValidate(default, vocabulary, out error));
                Assert.IsFalse(EffectCompositionValidator.TryValidate(default, null, out error));
            }
            finally { UnityEngine.Object.DestroyImmediate(vocabulary); }
        }

        [Test]
        public void SelectedEntry_IsValidatedBeforeComposition()
        {
            EffectVocabulary vocabulary = ScriptableObject.CreateInstance<EffectVocabulary>();
            try
            {
                vocabulary.legacyTable[new EffectCell(EffectOperation.Damage, EffectAspect.Offence)] =
                    new EffectCellEntry(EffectKey.Burst, EffectKey.Burst, false);
                vocabulary.entries[EffectKey.Burst] = new ElementEntry();
                Assert.IsFalse(EffectCompositionValidator.TryValidate(default, vocabulary, out _));
                vocabulary.entries[EffectKey.Burst].parts = new[]
                    { new LookPart { id = "Shape", primitive = Primitive.Sphere, size = Vector3.one } };
                Assert.IsTrue(EffectCompositionValidator.TryValidate(default, vocabulary, out string error), error);
            }
            finally { UnityEngine.Object.DestroyImmediate(vocabulary); }
        }

        [Test]
        public void Channels_AllEnumsAreCheckedAndRawPeriodsKeepFallbackSemantics()
        {
            EffectChannels[] cases = new[]
            {
                new EffectChannels { operation = (EffectOperation)999 },
                new EffectChannels { aspect = (EffectAspect)999 },
                new EffectChannels { magnitude = (EffectMagnitude)999 },
                new EffectChannels { reach = (EffectReach)999 },
                new EffectChannels { delivery = (EffectDelivery)999 },
                new EffectChannels { trigger = (EffectTrigger)999 },
                new EffectChannels { side = (EffectSide)999 },
                new EffectChannels { origin = (EffectOrigin)999 },
                new EffectChannels { family = (EffectFamily)999 },
                new EffectChannels { group = (AttributeGroup)999 },
                new EffectChannels { tempo = (EffectTempo)999 }
            };
            foreach (EffectChannels channels in cases)
            {
                Assert.IsFalse(EffectCompositionValidator.TryValidateChannels(channels, out string error));
                Assert.IsNotEmpty(error);
            }
            Assert.IsTrue(EffectCompositionValidator.TryValidateChannels(default, out _));
            foreach (float period in new[] { 0f, -1f, float.NaN, float.PositiveInfinity })
            {
                Assert.IsTrue(EffectCompositionValidator.TryValidateChannels(
                    new EffectChannels { periodSeconds = period }, out _));
            }
        }
    }
}
