using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using HealerLike.Render.Zones;

namespace HealerLike.Render.Grass
{

public class HLGrassLayoutTests
{
    [Test]
    public void DeterministicIndependentOfUnityRandomAndSeedChangesLayout()
    {
        UnityEngine.Random.State saved = UnityEngine.Random.state;
        UnityEngine.Random.InitState(987);
        UnityEngine.Random.State before = UnityEngine.Random.state;

        HLBladeSeed[] first = HLGrassLayout.Generate(3, 7, 2f, new Vector3(4f, 9f, -5f), 3f, 101, 42);
        HLBladeSeed[] second = HLGrassLayout.Generate(3, 7, 2f, new Vector3(4f, 9f, -5f), 3f, 101, 42);
        HLBladeSeed[] reseeded = HLGrassLayout.Generate(3, 7, 2f, new Vector3(4f, 9f, -5f), 3f, 101, 43);
        UnityEngine.Random.State after = UnityEngine.Random.state;
        UnityEngine.Random.state = saved;

        CollectionAssert.AreEqual(first, second);
        Assert.AreEqual(before, after);
        Assert.AreNotEqual(first[0], reseeded[0]);
        foreach (HLBladeSeed blade in first)
        {
            Assert.That(blade.positionYaw.x, Is.InRange(1f, 7f));
            Assert.That(blade.positionYaw.z, Is.InRange(-12f, 2f));
        }
    }

    [Test]
    public void GoldenFixture()
    {
        HLBladeSeed blade = HLGrassLayout.Generate(1, 1, 1f, Vector3.zero, 0.5f, 1, 1)[0];

        Assert.That(blade.positionYaw.x, Is.EqualTo(0.3113201856613159f).Within(0.000001f));
        Assert.That(blade.positionYaw.z, Is.EqualTo(0.26609694957733154f).Within(0.000001f));
        Assert.That(blade.heightPhaseWidthRandom.x, Is.EqualTo(0.3229929780960083f).Within(0.000001f));
    }

    [Test]
    public void MainHasExactCountsAndRanges()
    {
        HLBladeSeed[] seeds = HLGrassLayout.Generate(16, 16, 1f, Vector3.zero, 0.5f);
        int[] quotas = new int[256];

        Assert.AreEqual(65536, seeds.Length);
        foreach (HLBladeSeed blade in seeds)
        {
            Assert.That(blade.positionYaw.x, Is.GreaterThanOrEqualTo(-8f).And.LessThan(8f));
            Assert.That(blade.positionYaw.z, Is.GreaterThanOrEqualTo(-8f).And.LessThan(8f));
            Assert.AreEqual(0.505f, blade.positionYaw.y);
            Assert.That(blade.positionYaw.w, Is.InRange(0f, 2f * Mathf.PI));
            Assert.That(blade.heightPhaseWidthRandom.x, Is.InRange(0.17f, 0.42f));
            Assert.That(blade.heightPhaseWidthRandom.y, Is.InRange(0f, 2f * Mathf.PI));
            Assert.That(blade.heightPhaseWidthRandom.z, Is.InRange(0.095f, 0.15f));
            Assert.That(blade.heightPhaseWidthRandom.w, Is.InRange(0f, 1f));
            quotas[Mathf.FloorToInt(blade.positionYaw.x + 8f) + 16 * Mathf.FloorToInt(blade.positionYaw.z + 8f)]++;
        }
        foreach (int quota in quotas)
        {
            Assert.AreEqual(256, quota);
        }
    }

    [Test]
    public void Generate_OrdinaryCells_SpreadsBladesEvenlyAcrossEachCell()
    {
        HLBladeSeed[] seeds = HLGrassLayout.Generate(16, 16, 1f, Vector3.zero, 0.5f);
        int[] rows = new int[8];
        int[] columns = new int[8];

        foreach (HLBladeSeed blade in seeds)
        {
            rows[Mathf.FloorToInt(Mathf.Repeat(blade.positionYaw.z + 8f, 1f) * 8f)]++;
            columns[Mathf.FloorToInt(Mathf.Repeat(blade.positionYaw.x + 8f, 1f) * 8f)]++;
        }

        // 65536 blades over 8 bands is 8192 each; a sparse last row or gaps between rows fall far below
        for (int i = 0; i < 8; i++)
        {
            Assert.That(rows[i], Is.InRange(8192 * 0.8f, 8192 * 1.2f), "row band " + i);
            Assert.That(columns[i], Is.InRange(8192 * 0.8f, 8192 * 1.2f), "column band " + i);
        }
    }

    [TestCase(32768)]
    [TestCase(65536)]
    [TestCase(98304)]
    public void StandardQualityBudgets(int count)
    {
        Assert.AreEqual(count, HLGrassLayout.Generate(16, 16, 1f, Vector3.zero, 0f, count).Length);
    }

    [Test]
    public void CapAndStableQuotientRemainderAndDensity()
    {
        HLBladeSeed[] small = HLGrassLayout.Generate(3, 1, 1f, Vector3.zero, 0f, 8);
        int[] quotas = new int[3];
        Rect rect = new Rect(2f, -3f, 4f, 7f);
        HLBladeSeed[] dense = HLGrassLayout.Generate(rect, 0f, 8f);

        Assert.AreEqual(98304, HLGrassLayout.Generate(32, 32, 1f, Vector3.zero, 0f, int.MaxValue).Length);
        foreach (HLBladeSeed blade in small)
        {
            quotas[Mathf.FloorToInt(blade.positionYaw.x + 1.5f)]++;
        }
        CollectionAssert.AreEqual(new[] { 3, 3, 2 }, quotas);
        Assert.AreEqual(224, dense.Length); // 4 * 7 * 8
        foreach (HLBladeSeed blade in dense)
        {
            Assert.IsTrue(rect.Contains(new Vector2(blade.positionYaw.x, blade.positionYaw.z)));
        }
        Assert.IsEmpty(HLGrassLayout.Generate(1, 1, 1f, Vector3.zero, 0f, 0));
    }

    [Test]
    public void ClumpsShareRootsPhaseAndPatchHueWithThreeToSevenBlades()
    {
        HLBladeSeed[] seeds = HLGrassLayout.Generate(8, 8, 1f, Vector3.zero, 0f);
        HashSet<int> sizes = new HashSet<int>();
        float shortest = 1f;
        float tallest = 0f;
        int i = 0;

        while (i < seeds.Length)
        {
            HLBladeSeed first = seeds[i];
            int end = i + 1;
            while (end < seeds.Length && (Vector3)seeds[end].positionYaw == (Vector3)first.positionYaw)
            {
                end++;
            }
            Assert.That(end - i, Is.InRange(3, 7));
            sizes.Add(end - i);
            for (int j = i; j < end; j++)
            {
                Assert.AreEqual(first.heightPhaseWidthRandom.y, seeds[j].heightPhaseWidthRandom.y);
                Assert.AreEqual(first.heightPhaseWidthRandom.w, seeds[j].heightPhaseWidthRandom.w);
                shortest = Mathf.Min(shortest, seeds[j].heightPhaseWidthRandom.x);
                tallest = Mathf.Max(tallest, seeds[j].heightPhaseWidthRandom.x);
            }
            i = end;
        }

        Assert.AreEqual(5, sizes.Count);
        Assert.Less(shortest, 0.19f);
        Assert.Greater(tallest, 0.40f);
        // The first 2x2 cells always belong to one patch, whatever the seeded 2..4 patch scale
        Assert.AreEqual(seeds[0].heightPhaseWidthRandom.w, seeds[1024].heightPhaseWidthRandom.w);
        Assert.AreEqual(seeds[0].heightPhaseWidthRandom.w, seeds[8192].heightPhaseWidthRandom.w);
    }

    [Test]
    public void Generate_InvalidInputs_LogsAndReturnsNoSeeds()
    {
        Regex rejected = new Regex(@"^\[HLGrassLayout\] Rejected");
        for (int i = 0; i < 6; i++)
        {
            LogAssert.Expect(LogType.Error, rejected);
        }

        Assert.IsEmpty(HLGrassLayout.Generate(0, 1, 1f, Vector3.zero, 0f));
        Assert.IsEmpty(HLGrassLayout.Generate(1, 1, float.NaN, Vector3.zero, 0f));
        Assert.IsEmpty(HLGrassLayout.Generate(1, 1, 1f, Vector3.zero, 0f, -1));
        Assert.IsEmpty(HLGrassLayout.Generate(1, 1, 1f, Vector3.zero, float.PositiveInfinity));
        Assert.IsEmpty(HLGrassLayout.Generate(1, 1, 1f, new Vector3(float.NaN, 0f, 0f), 0f));
        Assert.IsEmpty(HLGrassLayout.Generate(new Rect(0f, 0f, 1f, 1f), 0f, -1f));
    }

    [Test]
    public void BufferStridesAndZoneOffsetsMatchFrozenAbi()
    {
        string[] names = { "position", "radius", "kind", "strength", "age", "reserved" };
        int[] offsets = { 0, 12, 16, 20, 24, 28 };

        Assert.AreEqual(HLZone.Stride, Marshal.SizeOf<HLZone>());
        Assert.AreEqual(32, HLZone.Stride);
        Assert.AreEqual(HLBladeSeed.Stride, Marshal.SizeOf<HLBladeSeed>());
        Assert.AreEqual(HLBladeState.Stride, Marshal.SizeOf<HLBladeState>());
        for (int i = 0; i < names.Length; i++)
        {
            Assert.AreEqual(offsets[i], Marshal.OffsetOf<HLZone>(names[i]).ToInt32());
        }
    }
}
}
