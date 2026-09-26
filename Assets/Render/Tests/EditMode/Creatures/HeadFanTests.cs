using System.Collections.Generic;
using System;
using NUnit.Framework;
using UnityEngine.TestTools;
using UnityEngine;
using HealerLike.Render.Grammar;
using HealerLike.Render.Studio;
using Object = UnityEngine.Object;

namespace HealerLike.Render.Creatures
{

public class HeadFanTests : CreatureCompositionFixture
{
    [TestCase(CountBand.Few)]
    [TestCase(CountBand.Many)]
    public void Layout_StoneFan_KeepsBroadHeadCopiesApart(CountBand count)
    {
        _vocabulary.heads[HeadKind.Bud].stone[0].size = new Vector3(2.3f, 1f, 0.5f);
        UnitChannels channels = RenderTestAssets.CreateChannels(LookSide.Stone, HeadKind.Bud, count);
        Assert.Greater(LookMeasure.HeadGap(channels, _vocabulary), 0f);
    }

    [TestCase(CountBand.Few)]
    [TestCase(CountBand.Many)]
    public void Layout_PlantFan_KeepsItsOuterNeighboursApartForWideHeads(CountBand count)
    {
        _vocabulary.heads[HeadKind.Bud].plant[0].size = new Vector3(2.3f, 1f, 0.5f);
        UnitChannels channels = RenderTestAssets.CreateChannels(LookSide.Plant, HeadKind.Bud, count);
        Assert.Greater(LookMeasure.HeadGap(channels, _vocabulary), 0f);
    }

    [TestCase(CountBand.Few, 2)]
    [TestCase(CountBand.Many, 4)]
    public void Layout_StoneFan_ConnectsOuterCopiesWithMineralSlabs(CountBand count, int supports)
    {
        UnitChannels channels = RenderTestAssets.CreateChannels(LookSide.Stone, HeadKind.Bud, count);
        PartList parts = LookComposer.Layout(channels, _vocabulary);
        Vector3 neck = UnitSockets.Place(channels, _vocabulary).neck;
        LookPart body = parts.Source(0);
        Bounds bodyBounds = new Bounds(body.position, body.size);
        int found = 0;
        foreach (int start in parts.headStarts)
        {
            LookPart support = parts.Source(start);
            if (support.id != HeadFan.BranchId)
            {
                Assert.IsTrue(
                    bodyBounds.Intersects(new Bounds(support.position, support.size)),
                    "The central head rests on the body."
                );
                continue;
            }

            found++;
            LookPart head = parts.Source(start + 1);
            Assert.AreEqual(ShapeKind.Block, support.shape.kind);
            Assert.AreEqual(Primitive.Stone, support.primitive);
            Assert.AreEqual(neck.y, support.position.y, 0.00001f);
            // Supports lie across X after a quarter-turn about Z.
            Bounds slab = new Bounds(support.position, new Vector3(support.size.y, support.size.x, support.size.z));
            Assert.IsTrue(bodyBounds.Intersects(slab), "The slab attaches to the body.");
            Assert.IsTrue(
                slab.Intersects(new Bounds(head.position, head.size)),
                "Each outer head rests on a connected slab."
            );
        }

        Assert.AreEqual(supports, found);
    }
}
}
