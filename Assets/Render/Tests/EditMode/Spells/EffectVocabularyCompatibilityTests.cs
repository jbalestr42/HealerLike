using NUnit.Framework;
using HealerLike.Render.Grammar;

namespace HealerLike.Render.Spells
{
    public class EffectVocabularyCompatibilityTests
    {
        [TestCase(EffectFamily.Rot, EffectKey.Drips)]
        [TestCase(EffectFamily.Renew, EffectKey.Stalks)]
        public void LegacyPeriodicFamily_EveryAspectAndClock_MatchesShippedVocabulary(
            EffectFamily family, EffectKey expected)
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

        [TestCase(EffectOperation.Damage, EffectKey.Burst, EffectKey.Burst, EffectKey.Burst)]
        [TestCase(EffectOperation.Heal, EffectKey.Rise, EffectKey.Rise, EffectKey.Rise)]
        [TestCase(EffectOperation.Boon, EffectKey.Orbit, EffectKey.Plates, EffectKey.Bud)]
        [TestCase(EffectOperation.Bane, EffectKey.Press, EffectKey.Crack, EffectKey.Crack)]
        [TestCase(EffectOperation.Ward, EffectKey.Plates, EffectKey.Plates, EffectKey.Bud)]
        [TestCase(EffectOperation.Mana, EffectKey.ManaUp, EffectKey.ManaUp, EffectKey.ManaUp)]
        [TestCase(EffectOperation.ManaDrain, EffectKey.ManaDown, EffectKey.ManaDown, EffectKey.ManaDown)]
        public void ShippedTable_EveryOperation_HasThePinnedAspectEntries(EffectOperation operation,
            EffectKey offence, EffectKey defence, EffectKey prevention)
        {
            EffectVocabulary vocabulary = RenderTestAssets.LoadEffectVocabulary();
            Assert.AreEqual(21, vocabulary.legacyTable.Count);
            EffectKey[] expected = { offence, defence, prevention };
            foreach (EffectAspect aspect in System.Enum.GetValues(typeof(EffectAspect)))
            {
                EffectCellEntry cell = vocabulary.legacyTable[new EffectCell(operation, aspect)];
                Assert.AreEqual(expected[(int)aspect], cell.once, aspect.ToString());
                bool periodic = operation == EffectOperation.Damage || operation == EffectOperation.Heal;
                Assert.AreEqual(periodic, cell.hasPeriodic);
                if (periodic)
                    Assert.AreEqual(operation == EffectOperation.Damage ? EffectKey.Drips : EffectKey.Stalks,
                        cell.periodic);
            }
        }
    }
}
