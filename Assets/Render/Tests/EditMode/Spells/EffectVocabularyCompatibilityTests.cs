using NUnit.Framework;
using HealerLike.Render.Grammar;

namespace HealerLike.Render.Spells
{
    public class EffectVocabularyCompatibilityTests
    {
        [TestCase(EffectFamily.Rot, EffectElement.Drips)]
        [TestCase(EffectFamily.Renew, EffectElement.Stalks)]
        public void LegacyPeriodicFamily_EveryAspectAndClock_MatchesShippedVocabulary(
            EffectFamily family, EffectElement expected)
        {
            EffectVocabulary vocabulary = RenderTestAssets.LoadEffectVocabulary();
            foreach (AttributeGroup group in System.Enum.GetValues(typeof(AttributeGroup)))
            foreach (EffectTempo tempo in System.Enum.GetValues(typeof(EffectTempo)))
            {
                EffectChannels channels = new EffectChannels { family = family, group = group, tempo = tempo };
                Assert.AreEqual(expected, EffectComposer.Element(channels), $"Legacy {group}/{tempo}");
                Assert.AreEqual(expected, EffectComposer.Element(vocabulary, channels), $"Asset {group}/{tempo}");
            }
        }

        [TestCase(EffectOperation.Damage, EffectElement.Burst, EffectElement.Burst, EffectElement.Burst)]
        [TestCase(EffectOperation.Heal, EffectElement.Rise, EffectElement.Rise, EffectElement.Rise)]
        [TestCase(EffectOperation.Boon, EffectElement.Orbit, EffectElement.Plates, EffectElement.Bud)]
        [TestCase(EffectOperation.Bane, EffectElement.Press, EffectElement.Crack, EffectElement.Crack)]
        [TestCase(EffectOperation.Ward, EffectElement.Plates, EffectElement.Plates, EffectElement.Plates)]
        [TestCase(EffectOperation.Mana, EffectElement.ManaUp, EffectElement.ManaUp, EffectElement.ManaUp)]
        public void ShippedTable_EveryOperation_HasThePinnedAspectEntries(EffectOperation operation,
            EffectElement offence, EffectElement defence, EffectElement prevention)
        {
            EffectVocabulary vocabulary = RenderTestAssets.LoadEffectVocabulary();
            Assert.AreEqual(18, vocabulary.table.Count);
            EffectElement[] expected = { offence, defence, prevention };
            foreach (EffectAspect aspect in System.Enum.GetValues(typeof(EffectAspect)))
            {
                EffectCellEntry cell = vocabulary.table[new EffectCell(operation, aspect)];
                Assert.AreEqual(expected[(int)aspect], cell.once, aspect.ToString());
                bool periodic = operation == EffectOperation.Damage || operation == EffectOperation.Heal;
                Assert.AreEqual(periodic, cell.hasPeriodic);
                if (periodic)
                    Assert.AreEqual(operation == EffectOperation.Damage ? EffectElement.Drips : EffectElement.Stalks,
                        cell.periodic);
            }
        }
    }
}
