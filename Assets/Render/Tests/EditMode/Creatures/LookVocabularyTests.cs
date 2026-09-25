using System;
using NUnit.Framework;
using UnityEditor;
using HealerLike.Render.Grammar;

namespace HealerLike.Render.Creatures
{

public class LookVocabularyTests
{
    [Test]
    public void Palette_ShippedAsset_IsSet()
    {
        Assert.NotNull(RenderTestAssets.LoadLookVocabulary().palette);
    }

    [Test]
    public void Heads_EveryHeadKind_HasPartsOnBothSides()
    {
        LookVocabulary vocabulary = RenderTestAssets.LoadLookVocabulary();

        foreach (HeadKind head in Enum.GetValues(typeof(HeadKind)))
        {
            Assert.IsNotEmpty(vocabulary.heads[head].plant, head.ToString());
            Assert.IsNotEmpty(vocabulary.heads[head].stone, head.ToString());
        }
    }

    [Test]
    public void Accessories_EveryAccessoryKind_HasPartsOnBothSides()
    {
        LookVocabulary vocabulary = RenderTestAssets.LoadLookVocabulary();

        foreach (AccessoryKind accessory in Enum.GetValues(typeof(AccessoryKind)))
        {
            if (accessory == AccessoryKind.None)
            {
                continue;
            }

            Assert.IsNotEmpty(vocabulary.accessories[accessory].plant, accessory.ToString());
            Assert.IsNotEmpty(vocabulary.accessories[accessory].stone, accessory.ToString());
        }
    }

    [Test]
    public void Bodies_EveryMassBand_StartsWithTheBodyOnBothSides()
    {
        LookVocabulary vocabulary = RenderTestAssets.LoadLookVocabulary();

        foreach (MassBand mass in Enum.GetValues(typeof(MassBand)))
        {
            Assert.AreEqual(PartRole.Body, vocabulary.bodies[mass].plant[0].role, mass.ToString());
            Assert.AreEqual(PartRole.Body, vocabulary.bodies[mass].stone[0].role, mass.ToString());
        }
    }

    [Test]
    public void Stems_EveryStemBand_HasALength()
    {
        LookVocabulary vocabulary = RenderTestAssets.LoadLookVocabulary();

        foreach (StemBand stem in Enum.GetValues(typeof(StemBand)))
        {
            Assert.Greater(vocabulary.stems[stem].length, 0f, stem.ToString());
        }
    }

    [Test]
    public void Roots_EveryReachBand_HasAReach()
    {
        LookVocabulary vocabulary = RenderTestAssets.LoadLookVocabulary();

        foreach (ReachBand reach in Enum.GetValues(typeof(ReachBand)))
        {
            Assert.Greater(vocabulary.roots[reach].reach, 0f, reach.ToString());
        }
    }

    [TestCase(ReachBand.Short)]
    [TestCase(ReachBand.Mid)]
    [TestCase(ReachBand.Long)]
    public void Reach_ShippedAsset_UsesEachBandsAuthoredSpread(ReachBand band)
    {
        LookVocabulary vocabulary = RenderTestAssets.LoadLookVocabulary();

        float reach = vocabulary.Reach(band);

        Assert.IsFalse(vocabulary.isReachPinned);
        Assert.AreEqual(vocabulary.roots[band].reach, reach);
    }

    [Test]
    public void Reach_ExplicitPin_OverridesTheBand()
    {
        LookVocabulary vocabulary = UnityEngine.ScriptableObject.CreateInstance<LookVocabulary>();
        try
        {
            vocabulary.isReachPinned = true;
            vocabulary.pinnedReach = 1.7f;
            foreach (ReachBand band in Enum.GetValues(typeof(ReachBand)))
            {
                Assert.AreEqual(1.7f, vocabulary.Reach(band));
            }
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(vocabulary);
        }
    }

    // Cadence order remains stable while the vocabulary can exaggerate each band's proportions.
    [Test]
    public void Stems_Bands_KeepQuickTallAndSlowShort()
    {
        LookVocabulary vocabulary = RenderTestAssets.LoadLookVocabulary();

        float slow = vocabulary.stems[StemBand.Slow].length;

        Assert.Greater(vocabulary.stems[StemBand.Steady].length, slow);
        Assert.Greater(vocabulary.stems[StemBand.Quick].length, vocabulary.stems[StemBand.Steady].length);
        Assert.Less(vocabulary.stems[StemBand.Quick].thickness, vocabulary.stems[StemBand.Slow].thickness);
    }

    [Test]
    public void Heads_StoneParts_DrawSeededStonesRatherThanTheOneBoulder()
    {
        LookVocabulary vocabulary = RenderTestAssets.LoadLookVocabulary();

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
        LookVocabulary vocabulary = RenderTestAssets.LoadLookVocabulary();
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
