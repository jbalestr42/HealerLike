using System;
using NUnit.Framework;
using HealerLike.Render.Grammar;

namespace HealerLike.Render.Creatures
{

public class LookMeasureTests
{
    LookVocabulary _vocabulary;

    [SetUp]
    public void SetUp()
    {
        _vocabulary = RenderTestAssets.LoadLookVocabulary();
    }

    [Test]
    public void AccessoryReach_EveryAccessoryMassStemAndSide_BreaksTheOutline()
    {
        foreach (LookSide side in Enum.GetValues(typeof(LookSide)))
        {
            float needed = side == LookSide.Plant ? LookComposer.PlantAccessoryReach : LookComposer.StoneAccessoryReach;
            foreach (AccessoryKind accessory in Enum.GetValues(typeof(AccessoryKind)))
            {
                if (accessory == AccessoryKind.None)
                {
                    continue;
                }

                foreach (MassBand mass in Enum.GetValues(typeof(MassBand)))
                {
                    foreach (StemBand stem in Enum.GetValues(typeof(StemBand)))
                    {
                        UnitChannels channels = RenderTestAssets.CreateChannels(side, HeadKind.Bud, stem: stem, mass: mass, accessory: accessory);

                        float reach = LookMeasure.AccessoryReach(channels, _vocabulary);

                        Assert.GreaterOrEqual(reach, needed, $"{side} {accessory} {mass} {stem}"); // in cells past body and head
                    }
                }
            }
        }
    }

    [Test]
    public void HeadSpan_EveryPlantHeadAtLightMass_CoversAThirdOfACell()
    {
        foreach (HeadKind head in Enum.GetValues(typeof(HeadKind)))
        {
            UnitChannels channels = RenderTestAssets.CreateChannels(LookSide.Plant, head, mass: MassBand.Light);

            float span = LookMeasure.HeadSpan(channels, _vocabulary);

            Assert.GreaterOrEqual(span, 0.35f, head.ToString()); // the smaller side of its screen box, in cells
        }
    }

    [TestCase(CountBand.Few)]
    [TestCase(CountBand.Many)]
    public void HeadGap_EveryFannedPlantHead_KeepsNeighboursApart(CountBand count)
    {
        foreach (HeadKind head in Enum.GetValues(typeof(HeadKind)))
        {
            if (_vocabulary.heads[head].carriesCount)
            {
                continue;
            }

            UnitChannels channels = RenderTestAssets.CreateChannels(LookSide.Plant, head, count, mass: MassBand.Light);

            float gap = LookMeasure.HeadGap(channels, _vocabulary);

            Assert.GreaterOrEqual(gap, 0f, $"{head} {count}"); // in cells between two neighbouring copies
        }
    }
}

}
