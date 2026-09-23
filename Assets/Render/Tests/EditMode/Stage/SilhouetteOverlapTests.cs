using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Stage
{

public class SilhouetteOverlapTests
{
    // An 8-pixel strip whose pixels from first to last, excluded, are set
    static bool[] CreateMask(int first, int last)
    {
        bool[] mask = new bool[8];
        for (int i = first; i < last; i++)
        {
            mask[i] = true;
        }
        return mask;
    }

    [Test]
    public void IoU_IdenticalMasks_IsOne()
    {
        bool[] mask = CreateMask(first: 1, last: 5);

        float iou = SilhouetteOverlap.IoU(mask, CreateMask(first: 1, last: 5));

        Assert.AreEqual(1f, iou);
    }

    [Test]
    public void IoU_DisjointMasks_IsZero()
    {
        bool[] a = CreateMask(first: 0, last: 4);
        bool[] b = CreateMask(first: 4, last: 8);

        float iou = SilhouetteOverlap.IoU(a, b);

        Assert.AreEqual(0f, iou);
    }

    [Test]
    public void IoU_HalfOverlap_IsOneThird()
    {
        bool[] a = CreateMask(first: 0, last: 4);
        bool[] b = CreateMask(first: 2, last: 6);

        float iou = SilhouetteOverlap.IoU(a, b);

        Assert.AreEqual(1f / 3f, iou, 0.0001f); // 2 shared of 6 covered
    }

    [Test]
    public void IoU_TwoEmptyMasks_IsZero()
    {
        bool[] empty = CreateMask(first: 0, last: 0);

        float iou = SilhouetteOverlap.IoU(empty, CreateMask(first: 0, last: 0));

        Assert.AreEqual(0f, iou);
    }

    [Test]
    public void IoU_DifferentSizes_LogsAndIsZero()
    {
        bool[] a = CreateMask(first: 0, last: 4);
        bool[] b = new bool[4];

        float iou = 1f;
        TestHelpers.WithLoggingDisabled(() => iou = SilhouetteOverlap.IoU(a, b));

        Assert.AreEqual(0f, iou);
    }

    [Test]
    public void Mask_AgainstGroundColour_SetsTheDifferingPixels()
    {
        Color32 ground = new Color32(91, 144, 85, 255);
        Color32[] pixels = { ground, new Color32(95, 146, 86, 255), new Color32(200, 60, 60, 255) };

        bool[] mask = SilhouetteOverlap.Mask(pixels, ground, 24);

        Assert.AreEqual(new[] { false, false, true }, mask);
    }

    [Test]
    public void Mask_AgainstGroundRender_CancelsItsShading()
    {
        Color32[] ground = { new Color32(40, 70, 40, 255), new Color32(120, 180, 110, 255) };
        Color32[] pixels = { new Color32(40, 70, 40, 255), new Color32(40, 70, 40, 255) };

        bool[] mask = SilhouetteOverlap.Mask(pixels, ground, 24);

        Assert.AreEqual(new[] { false, true }, mask);
    }

    [Test]
    public void Collisions_ThreeMasks_ListsPairsAboveTheThresholdClosestFirst()
    {
        List<bool[]> masks = new List<bool[]>
        {
            CreateMask(first: 0, last: 8),
            CreateMask(first: 0, last: 7),
            CreateMask(first: 0, last: 8)
        };

        List<SilhouetteOverlap.Pair> pairs = SilhouetteOverlap.Collisions(masks, 0.85f);

        Assert.AreEqual(3, pairs.Count);
        Assert.AreEqual(0, pairs[0].first);
        Assert.AreEqual(2, pairs[0].second);
        Assert.AreEqual(1f, pairs[0].iou);
        Assert.AreEqual(0.875f, pairs[2].iou); // 7 shared of 8 covered
    }

    [Test]
    public void Collisions_DistinctMasks_ListsNothing()
    {
        List<bool[]> masks = new List<bool[]> { CreateMask(first: 0, last: 4), CreateMask(first: 2, last: 6) };

        List<SilhouetteOverlap.Pair> pairs = SilhouetteOverlap.Collisions(masks, 0.85f);

        Assert.AreEqual(0, pairs.Count);
    }
}

}
