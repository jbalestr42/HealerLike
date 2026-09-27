using System;
using NUnit.Framework;
using HealerLike.Render.Creatures;
using HealerLike.Render.Grammar;

namespace HealerLike.Render.Spells
{
    public class SpellCompositionPhase1bTests
    {
        static EffectVocabulary Vocabulary()
        {
            EffectVocabulary vocabulary = UnityEngine.ScriptableObject.CreateInstance<EffectVocabulary>();
            vocabulary.legacyTable = new System.Collections.Generic.Dictionary<EffectCell, EffectCellEntry>();
            foreach (var pair in ExpectedCells())
            {
                vocabulary.legacyTable[pair.Key] = new EffectCellEntry(pair.Value, pair.Value, false);
            }
            return vocabulary;
        }

        [Test]
        public void CellTable_MatchesPreMigrationSwitchForEveryReachableCell()
        {
            EffectVocabulary vocabulary = Vocabulary();
            foreach (var pair in ExpectedCells())
            {
                EffectKey actual;
                Assert.IsTrue(vocabulary.TryGetElement(pair.Key.operation, pair.Key.aspect, out actual));
                Assert.AreEqual(pair.Value, actual, pair.Key.operation + "/" + pair.Key.aspect);
            }
        }

        [Test]
        public void MissingCell_IsReportedByCompositionValidator()
        {
            EffectVocabulary vocabulary = UnityEngine.ScriptableObject.CreateInstance<EffectVocabulary>();
            EffectChannels channels = new EffectChannels {
                operation = EffectOperation.Damage, aspect = EffectAspect.Offence, tempo = EffectTempo.Once
            };
            string error;
            Assert.IsFalse(EffectCompositionValidator.TryValidate(channels, vocabulary, out error));
            StringAssert.Contains("missing", error);
        }

        [Test]
        public void RecipeOverBudget_IsRejected()
        {
            ElementEntry entry = new ElementEntry { parts = new LookPart[257] };
            EffectRecipe recipe = new EffectRecipe { entry = entry, count = 1 };
            string error;
            Assert.IsFalse(EffectValidator.TryValidate(recipe, out error));
            StringAssert.Contains("1..256", error);
        }

        [Test]
        public void BadPart_IsRejected()
        {
            ElementEntry entry = new ElementEntry { parts = new[] { new LookPart() } };
            EffectRecipe recipe = new EffectRecipe { entry = entry, count = 1 };
            string error;
            Assert.IsFalse(EffectValidator.TryValidate(recipe, out error));
            StringAssert.Contains("valid spell part", error);
        }

        static System.Collections.Generic.Dictionary<EffectCell, EffectKey> ExpectedCells()
        {
            var result = new System.Collections.Generic.Dictionary<EffectCell, EffectKey>();
            foreach (EffectOperation operation in Enum.GetValues(typeof(EffectOperation)))
            foreach (EffectAspect aspect in Enum.GetValues(typeof(EffectAspect)))
            {
                EffectKey key = operation == EffectOperation.Damage ? EffectKey.Burst
                    : operation == EffectOperation.Heal ? EffectKey.Rise
                    : operation == EffectOperation.Boon ? (aspect == EffectAspect.Defence ? EffectKey.Plates
                        : aspect == EffectAspect.Prevention ? EffectKey.Bud : EffectKey.Orbit)
                    : operation == EffectOperation.Ward ? (aspect == EffectAspect.Prevention ? EffectKey.Bud : EffectKey.Plates)
                    : operation == EffectOperation.Mana ? EffectKey.ManaUp
                    : aspect == EffectAspect.Offence ? EffectKey.Press : EffectKey.Crack;
                result[new EffectCell(operation, aspect)] = key;
            }
            return result;
        }

        [Test]
        public void PeriodicOffenceCellsUseDripsAndStalks()
        {
            EffectVocabulary vocabulary = Vocabulary();
            vocabulary.legacyTable[new EffectCell(EffectOperation.Damage, EffectAspect.Offence)] =
                new EffectCellEntry(EffectKey.Burst, EffectKey.Drips, true);
            vocabulary.legacyTable[new EffectCell(EffectOperation.Heal, EffectAspect.Offence)] =
                new EffectCellEntry(EffectKey.Rise, EffectKey.Stalks, true);
            EffectKey damage;
            EffectKey heal;
            Assert.IsTrue(vocabulary.TryGetElement(EffectOperation.Damage, EffectAspect.Offence,
                EffectTempo.PerPeriod, out damage));
            Assert.IsTrue(vocabulary.TryGetElement(EffectOperation.Heal, EffectAspect.Offence,
                EffectTempo.PerPeriod, out heal));
            Assert.AreEqual(EffectKey.Drips, damage);
            Assert.AreEqual(EffectKey.Stalks, heal);
        }
    }
}
