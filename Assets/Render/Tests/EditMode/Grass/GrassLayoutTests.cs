using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace HealerLike.Render.Grass
{

public class GrassLayoutTests
{
    [Test]
    public void Generate_SameSeed_IsDeterministicAndLeavesUnityRandomAlone()
    {
        UnityEngine.Random.State saved = UnityEngine.Random.state;
        UnityEngine.Random.InitState(987);
        UnityEngine.Random.State before = UnityEngine.Random.state;

        BladeSeed[] first = GrassLayout.Generate(3, 7, 2f, new Vector3(4f, 9f, -5f), 3f, 101, 42);
        BladeSeed[] second = GrassLayout.Generate(3, 7, 2f, new Vector3(4f, 9f, -5f), 3f, 101, 42);
        BladeSeed[] reseeded = GrassLayout.Generate(3, 7, 2f, new Vector3(4f, 9f, -5f), 3f, 101, 43);
        UnityEngine.Random.State after = UnityEngine.Random.state;
        UnityEngine.Random.state = saved;

        CollectionAssert.AreEqual(first, second);
        Assert.AreEqual(before, after);
        Assert.AreNotEqual(first[0], reseeded[0]);
        foreach (BladeSeed blade in first)
        {
            Assert.That(blade.positionYaw.x, Is.InRange(1f, 7f));
            Assert.That(blade.positionYaw.z, Is.InRange(-12f, 2f));
        }
    }

    [Test]
    public void Generate_SeedOne_MatchesGoldenBlade()
    {
        BladeSeed blade = GrassLayout.Generate(1, 1, 1f, Vector3.zero, 0.5f, 1, 1)[0];

        Assert.That(blade.positionYaw.x, Is.EqualTo(0.3113201856613159f).Within(0.000001f));
        Assert.That(blade.positionYaw.z, Is.EqualTo(0.26609694957733154f).Within(0.000001f));
        Assert.That(blade.heightPhaseWidthRandom.x, Is.EqualTo(0.2594265639781952f).Within(0.000001f));
    }

    [Test]
    public void Generate_MainBoard_HasExactQuotasAndRanges()
    {
        BladeSeed[] seeds = GrassLayout.Generate(16, 16, 1f, Vector3.zero, 0.5f);
        int[] quotas = new int[256];

        Assert.AreEqual(65536, seeds.Length);
        foreach (BladeSeed blade in seeds)
        {
            Assert.That(blade.positionYaw.x, Is.GreaterThanOrEqualTo(-8f).And.LessThan(8f));
            Assert.That(blade.positionYaw.z, Is.GreaterThanOrEqualTo(-8f).And.LessThan(8f));
            Assert.AreEqual(0.505f, blade.positionYaw.y);
            Assert.That(blade.positionYaw.w, Is.InRange(0f, 2f * Mathf.PI));
            Assert.That(blade.heightPhaseWidthRandom.x, Is.InRange(0.23f, 0.27f));
            Assert.That(blade.heightPhaseWidthRandom.y, Is.InRange(0f, 2f * Mathf.PI));
            Assert.That(blade.heightPhaseWidthRandom.z, Is.InRange(0.0747f, 0.0914f));
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
        BladeSeed[] seeds = GrassLayout.Generate(16, 16, 1f, Vector3.zero, 0.5f);
        int[] rows = new int[8];
        int[] columns = new int[8];

        foreach (BladeSeed blade in seeds)
        {
            rows[Mathf.FloorToInt(Mathf.Repeat(blade.positionYaw.z + 8f, 1f) * 8f)]++;
            columns[Mathf.FloorToInt(Mathf.Repeat(blade.positionYaw.x + 8f, 1f) * 8f)]++;
        }

        // 65536 tufts over 8 bands is 8192 each; a sparse last row or gaps between rows fall far below
        for (int i = 0; i < 8; i++)
        {
            Assert.That(rows[i], Is.InRange(8192 * 0.8f, 8192 * 1.2f), "row band " + i);
            Assert.That(columns[i], Is.InRange(8192 * 0.8f, 8192 * 1.2f), "column band " + i);
        }
    }

    [TestCase(32768)]
    [TestCase(65536)]
    [TestCase(98304)]
    public void Generate_QualityBudget_ReturnsThatManyBlades(int count)
    {
        Assert.AreEqual(count, GrassLayout.Generate(16, 16, 1f, Vector3.zero, 0f, count).Length);
    }

    [Test]
    public void Generate_BudgetsAndDensity_CapAndSpreadRemainder()
    {
        BladeSeed[] small = GrassLayout.Generate(3, 1, 1f, Vector3.zero, 0f, 8);
        int[] quotas = new int[3];
        Rect rect = new Rect(2f, -3f, 4f, 7f);
        BladeSeed[] dense = GrassLayout.Generate(rect, 0f, 8f);

        Assert.AreEqual(98304, GrassLayout.Generate(32, 32, 1f, Vector3.zero, 0f, int.MaxValue).Length);
        foreach (BladeSeed blade in small)
        {
            quotas[Mathf.FloorToInt(blade.positionYaw.x + 1.5f)]++;
        }
        CollectionAssert.AreEqual(new[] { 3, 3, 2 }, quotas);
        Assert.AreEqual(224, dense.Length); // 4 * 7 * 8
        foreach (BladeSeed blade in dense)
        {
            Assert.IsTrue(rect.Contains(new Vector2(blade.positionYaw.x, blade.positionYaw.z)));
        }
        Assert.IsEmpty(GrassLayout.Generate(1, 1, 1f, Vector3.zero, 0f, 0));
    }

    [Test]
    public void Generate_PatchLane_VariesSoftlyBetweenNeighbourCells()
    {
        BladeSeed[] seeds = GrassLayout.Generate(16, 16, 1f, Vector3.zero, 0f);
        float[] means = new float[256];
        HashSet<Vector3> roots = new HashSet<Vector3>();

        foreach (BladeSeed blade in seeds)
        {
            roots.Add(blade.positionYaw);
            Assert.That(blade.heightPhaseWidthRandom.w, Is.InRange(0f, 1f));
            means[Mathf.FloorToInt(blade.positionYaw.x + 8f) + 16 * Mathf.FloorToInt(blade.positionYaw.z + 8f)] += blade.heightPhaseWidthRandom.w / 256f;
        }

        // Every tuft stands on its own root, no clumps
        Assert.AreEqual(seeds.Length, roots.Count);
        float largestStep = 0f;
        float lowest = 1f;
        float highest = 0f;
        for (int cell = 0; cell < 256; cell++)
        {
            lowest = Mathf.Min(lowest, means[cell]);
            highest = Mathf.Max(highest, means[cell]);
            if (cell % 16 < 15)
            {
                largestStep = Mathf.Max(largestStep, Mathf.Abs(means[cell + 1] - means[cell]));
            }
            if (cell < 240)
            {
                largestStep = Mathf.Max(largestStep, Mathf.Abs(means[cell + 16] - means[cell]));
            }
        }
        // A lattice point every 5 cells moves the lane by at most about a quarter of its range per cell
        Assert.Less(largestStep, 0.3f); // 0.25 measured for seed 1
        Assert.Greater(highest - lowest, 0.2f);
    }

    [Test]
    public void Generate_InvalidInputs_LogsAndReturnsNoSeeds()
    {
        Regex rejected = new Regex(@"^\[GrassLayout\] Rejected");
        for (int i = 0; i < 6; i++)
        {
            LogAssert.Expect(LogType.Error, rejected);
        }

        Assert.IsEmpty(GrassLayout.Generate(0, 1, 1f, Vector3.zero, 0f));
        Assert.IsEmpty(GrassLayout.Generate(1, 1, float.NaN, Vector3.zero, 0f));
        Assert.IsEmpty(GrassLayout.Generate(1, 1, 1f, Vector3.zero, 0f, -1));
        Assert.IsEmpty(GrassLayout.Generate(1, 1, 1f, Vector3.zero, float.PositiveInfinity));
        Assert.IsEmpty(GrassLayout.Generate(1, 1, 1f, new Vector3(float.NaN, 0f, 0f), 0f));
        Assert.IsEmpty(GrassLayout.Generate(new Rect(0f, 0f, 1f, 1f), 0f, -1f));
    }
}

}
