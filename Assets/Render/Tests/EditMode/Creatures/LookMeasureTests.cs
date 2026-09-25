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
                        UnitChannels channels = RenderTestAssets.CreateChannels(side, HeadKind.Bud, stem: stem,
                            mass: mass, accessory: accessory);

                        float reach = LookMeasure.AccessoryReach(channels, _vocabulary);

                        if (_vocabulary.accessories[accessory].isCentered)
                        {
                            PartList layout = LookComposer.Layout(channels, _vocabulary);
                            float left = float.MaxValue;
                            float right = float.MinValue;
                            for (int i = layout.accessoryStart; i < layout.count; i++)
                            {
                                LookPart part = layout.Source(i);
                                left = UnityEngine.Mathf.Min(left, part.position.x - part.size.x * 0.5f);
                                right = UnityEngine.Mathf.Max(right, part.position.x + part.size.x * 0.5f);
                            }
                            Assert.Less(left, 0f, accessory.ToString());
                            Assert.Greater(right, 0f, accessory.ToString());
                            Assert.Greater((right - left) * _vocabulary.Unit(side), 0.35f,
                                "A centred collar or crown still needs a readable span: " + accessory);
                        }
                        else
                        {
                            // Side attachments must still clear the body and head in cells.
                            Assert.GreaterOrEqual(reach, needed, $"{side} {accessory} {mass} {stem}");
                        }
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
