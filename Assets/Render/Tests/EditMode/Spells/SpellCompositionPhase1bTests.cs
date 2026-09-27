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
            vocabulary.table = new System.Collections.Generic.Dictionary<EffectCell, EffectCellEntry>();
            foreach (var pair in EffectVocabulary.LegacyCells())
            {
                vocabulary.table[pair.Key] = new EffectCellEntry(pair.Value, pair.Value, false);
            }
            return vocabulary;
        }

        [Test]
        public void CellTable_MatchesPreMigrationSwitchForEveryReachableCell()
        {
            EffectVocabulary vocabulary = Vocabulary();
            foreach (var pair in EffectVocabulary.LegacyCells())
            {
                EffectElement actual;
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

        [Test]
        public void PeriodicOffenceCellsUseDripsAndStalks()
        {
            EffectVocabulary vocabulary = Vocabulary();
            vocabulary.table[new EffectCell(EffectOperation.Damage, EffectAspect.Offence)] =
                new EffectCellEntry(EffectElement.Burst, EffectElement.Drips, true);
            vocabulary.table[new EffectCell(EffectOperation.Heal, EffectAspect.Offence)] =
                new EffectCellEntry(EffectElement.Rise, EffectElement.Stalks, true);
            EffectElement damage;
            EffectElement heal;
            Assert.IsTrue(vocabulary.TryGetElement(EffectOperation.Damage, EffectAspect.Offence,
                EffectTempo.PerPeriod, out damage));
            Assert.IsTrue(vocabulary.TryGetElement(EffectOperation.Heal, EffectAspect.Offence,
                EffectTempo.PerPeriod, out heal));
            Assert.AreEqual(EffectElement.Drips, damage);
            Assert.AreEqual(EffectElement.Stalks, heal);
        }
    }
}
