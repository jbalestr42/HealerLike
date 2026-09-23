using System;
using NUnit.Framework;
using UnityEditor;
using HealerLike.Render.Grammar;

namespace HealerLike.Render.Creatures
{

public class LookVocabularyTests
{
    public static LookVocabulary Vocabulary()
    {
        return AssetDatabase.LoadAssetAtPath<LookVocabulary>("Assets/Render/Creatures/Data/LookVocabulary.asset");
    }

    [Test]
    public void Asset_EveryChannelValue_HasAnEntryOnBothSides()
    {
        LookVocabulary vocabulary = Vocabulary();

        Assert.NotNull(vocabulary.palette);
        foreach (HeadKind head in Enum.GetValues(typeof(HeadKind)))
        {
            Assert.IsNotEmpty(vocabulary.heads[head].plant, head.ToString());
            Assert.IsNotEmpty(vocabulary.heads[head].stone, head.ToString());
        }

        foreach (AccessoryKind accessory in Enum.GetValues(typeof(AccessoryKind)))
        {
            if (accessory == AccessoryKind.None)
            {
                continue;
            }

            Assert.IsNotEmpty(vocabulary.accessories[accessory].plant, accessory.ToString());
            Assert.IsNotEmpty(vocabulary.accessories[accessory].stone, accessory.ToString());
        }

        foreach (MassBand mass in Enum.GetValues(typeof(MassBand)))
        {
            Assert.AreEqual(PartRole.Body, vocabulary.bodies[mass].plant[0].role, mass.ToString());
            Assert.AreEqual(PartRole.Body, vocabulary.bodies[mass].stone[0].role, mass.ToString());
        }

        foreach (StemBand stem in Enum.GetValues(typeof(StemBand)))
        {
            Assert.Greater(vocabulary.stems[stem].length, 0f, stem.ToString());
        }

        foreach (ReachBand reach in Enum.GetValues(typeof(ReachBand)))
        {
            Assert.Greater(vocabulary.roots[reach].reach, 0f, reach.ToString());
        }
    }

    [TestCase(ReachBand.Short)]
    [TestCase(ReachBand.Mid)]
    [TestCase(ReachBand.Long)]
    public void Reach_WhilePinned_IsOneValueForEveryBand(ReachBand band)
    {
        LookVocabulary vocabulary = Vocabulary();

        float reach = vocabulary.Reach(band);

        Assert.IsTrue(vocabulary.isReachPinned);
        Assert.AreEqual(vocabulary.pinnedReach, reach);
    }

    [Test]
    public void Stems_Bands_KeepTheSpecRatio()
    {
        LookVocabulary vocabulary = Vocabulary();

        float slow = vocabulary.stems[StemBand.Slow].length;

        Assert.AreEqual(1.6f, vocabulary.stems[StemBand.Steady].length / slow, 0.0001f);
        Assert.AreEqual(2.4f, vocabulary.stems[StemBand.Quick].length / slow, 0.0001f);
    }

    [Test]
    public void Heads_StoneParts_DrawSeededStonesRatherThanTheOneBoulder()
    {
        LookVocabulary vocabulary = Vocabulary();

        foreach (LookVocabulary.HeadEntry entry in vocabulary.heads.Values)
        {
            foreach (LookPart part in entry.stone)
            {
                Assert.AreNotEqual(Primitive.Boulder, part.primitive, part.id);
            }
        }
    }

    [Test]
    public void Heads_ArchAndCairn_ShowOneThreeOrFiveCopiesByCount()
    {
        LookVocabulary vocabulary = Vocabulary();
        LookVocabulary.HeadEntry arch = vocabulary.heads[HeadKind.Arch];

        int[] plant = TipsByBand(arch.plant);
        int[] stone = TipsByBand(arch.stone);

        Assert.IsTrue(arch.carriesCount);
        Assert.AreEqual(new int[] { 1, 3, 5 }, plant);
        Assert.AreEqual(new int[] { 1, 3, 5 }, stone);
    }

    static int[] TipsByBand(LookPart[] parts)
    {
        int[] tips = new int[3];
        foreach (LookPart part in parts)
        {
            for (int band = (int)part.minCount; band < 3 && part.role == PartRole.Tip; band++)
            {
                tips[band]++;
            }
        }
        return tips;
    }
}

}
