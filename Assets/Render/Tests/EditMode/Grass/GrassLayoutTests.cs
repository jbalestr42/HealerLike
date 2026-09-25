using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace HealerLike.Render.Grass
{

public class GrassLayoutTests
{
    // The 16 by 16 board at full density: 53 roots a side, each step 16 / 53
    static readonly int boardSide = 53;

    static TuftSeed[] CreateBoard(uint seed = 1)
    {
        return GrassLayout.Generate(16, 16, 1f, Vector3.zero, 0.5f, GrassLayout.MaxBudget, seed);
    }

    [Test]
    public void Generate_SameSeed_IsDeterministicAndLeavesUnityRandomAlone()
    {
        UnityEngine.Random.State saved = UnityEngine.Random.state;
        UnityEngine.Random.InitState(987);
        UnityEngine.Random.State before = UnityEngine.Random.state;

        TuftSeed[] first = GrassLayout.Generate(3, 7, 2f, new Vector3(4f, 9f, -5f), 3f, 101, 42);
        TuftSeed[] second = GrassLayout.Generate(3, 7, 2f, new Vector3(4f, 9f, -5f), 3f, 101, 42);
        TuftSeed[] reseeded = GrassLayout.Generate(3, 7, 2f, new Vector3(4f, 9f, -5f), 3f, 101, 43);
        UnityEngine.Random.State after = UnityEngine.Random.state;
        UnityEngine.Random.state = saved;

        CollectionAssert.AreEqual(first, second);
        Assert.AreEqual(before, after);
        Assert.AreNotEqual(first[0], reseeded[0]);
        foreach (TuftSeed tuft in first)
        {
            Assert.That(tuft.positionYaw.x, Is.InRange(1f, 7f));
            Assert.That(tuft.positionYaw.z, Is.InRange(-12f, 2f));
        }
    }

    [Test]
    public void Generate_MainBoard_FillsTheGridAtTheSpacing()
    {
        TuftSeed[] seeds = CreateBoard();

        Assert.AreEqual(boardSide * boardSide, seeds.Length); // 2,809 large tufts with room for the ground to remain visible
        Assert.That(GrassLayout.Spacing, Is.EqualTo(0.3f).Within(0.00001f));
        Assert.That(GrassLayout.Density, Is.EqualTo(1f / 0.09f).Within(0.001f));
        Assert.That(16f / boardSide, Is.InRange(GrassLayout.Spacing, GrassLayout.Spacing * 1.01f));
    }

    [Test]
    public void Generate_MainBoard_WarpsRowsAndJittersWithoutLeavingTheBoard()
    {
        TuftSeed[] seeds = CreateBoard();
        float step = 16f / boardSide;
        float largest = 0f;

        for (int i = 0; i < seeds.Length; i++)
        {
            float centreX = -8f + (i % boardSide + 0.5f) * step;
            float centreZ = -8f + (i / boardSide + 0.5f) * step;
            float offsetX = Mathf.Abs(seeds[i].positionYaw.x - centreX);
            float offsetZ = Mathf.Abs(seeds[i].positionYaw.z - centreZ);
            Assert.That(offsetX, Is.LessThanOrEqualTo((GrassLayout.Jitter + GrassLayout.Clump) * step + 0.00001f));
            Assert.That(offsetZ, Is.LessThanOrEqualTo((GrassLayout.Jitter + GrassLayout.Clump) * step + 0.00001f));
            Assert.That(seeds[i].positionYaw.x, Is.InRange(-8f, 8f));
            Assert.That(seeds[i].positionYaw.z, Is.InRange(-8f, 8f));
            Assert.AreEqual(0.505f, seeds[i].positionYaw.y);
            largest = Mathf.Max(largest, Mathf.Max(offsetX, offsetZ));
        }

        Assert.Greater(largest, 0.8f * step); // roots leave the straight row through coherent bend plus jitter
    }

    [Test]
    public void Generate_MainBoard_ScalesEachTuftUniformlyBetween08And12()
    {
        TuftSeed[] seeds = CreateBoard();
        float smallest = float.MaxValue;
        float largest = 0f;

        foreach (TuftSeed tuft in seeds)
        {
            float scale = tuft.heightWidthLean.x / GrassLayout.TuftHeight;
            Assert.That(scale, Is.InRange(GrassLayout.MinScale - 0.00001f, GrassLayout.MaxScale + 0.00001f));
            Assert.That(tuft.heightWidthLean.y / tuft.heightWidthLean.x, Is.EqualTo(0.29f / 0.884f).Within(0.0001f));
            Assert.That(tuft.positionYaw.w, Is.InRange(0f, 2f * Mathf.PI));
            smallest = Mathf.Min(smallest, scale);
            largest = Mathf.Max(largest, scale);
        }

        Assert.Less(smallest, 0.81f);
        Assert.Greater(largest, 1.19f);
        Assert.That(GrassLayout.TuftHeight, Is.EqualTo(3f * 0.45f * 0.55f).Within(0.00001f));
    }

    [Test]
    public void Generate_MainBoard_VariesLeanWithinThePrevailingDirection()
    {
        TuftSeed[] seeds = CreateBoard();
        float largest = 0f;
        float sum = 0f;
        float mostLeft = 0f;
        float mostRight = 0f;

        foreach (TuftSeed tuft in seeds)
        {
            Vector2 lean = new Vector2(tuft.heightWidthLean.z, tuft.heightWidthLean.w);
            Assert.That(lean.magnitude, Is.InRange(0f, GrassLayout.MaxLean + 0.00001f));
            Assert.That(Vector2.Dot(lean.normalized, GrassLayout.LeanHeading),
                Is.GreaterThanOrEqualTo(Mathf.Cos(GrassLayout.LeanSpread) - 0.00001f));
            mostLeft = Mathf.Min(mostLeft, lean.x);
            mostRight = Mathf.Max(mostRight, lean.x);
            largest = Mathf.Max(largest, lean.magnitude);
            sum += lean.magnitude;
        }

        Assert.Greater(largest, 0.49f);
        Assert.Less(mostLeft, -0.15f);
        Assert.Greater(mostRight, 0.15f);
        Assert.That(sum / seeds.Length, Is.EqualTo(0.25f).Within(0.01f)); // uniform in 0..0.5
    }

    [Test]
    public void Generate_SmallBudget_WidensTheGridAndStaysUnderTheBudget()
    {
        TuftSeed[] seeds = GrassLayout.Generate(4, 2, 1.5f, new Vector3(2f, 0f, -1f), 0f, 50, 3);
        HashSet<Vector3> roots = new HashSet<Vector3>();

        Assert.AreEqual(50, seeds.Length); // 10 by 5, the budget widens the .3-cell spacing
        foreach (TuftSeed tuft in seeds)
        {
            roots.Add(tuft.positionYaw);
            Assert.That(tuft.positionYaw.x, Is.InRange(-1f, 5f));
            Assert.That(tuft.positionYaw.z, Is.InRange(-2.5f, 0.5f));
            Assert.That(tuft.heightWidthLean.x / (GrassLayout.TuftHeight * 1.5f), Is.InRange(0.79999f, 1.20001f));
        }
        Assert.AreEqual(seeds.Length, roots.Count);
    }

    [Test]
    public void Generate_LargeGrid_IsCappedByTheMaximumBudget()
    {
        TuftSeed[] seeds = GrassLayout.Generate(128, 128, 1f, Vector3.zero, 0f, int.MaxValue, 1);

        Assert.That(seeds.Length, Is.InRange(GrassLayout.MaxBudget * 0.99f, GrassLayout.MaxBudget));
        Assert.AreEqual(GrassLayout.MaxBudget, GrassLayout.CountFor(128, 128, int.MaxValue));
        Assert.IsEmpty(GrassLayout.Generate(1, 1, 1f, Vector3.zero, 0f, 0, 1));
    }

    [Test]
    public void Generate_InvalidInputs_LogsAndReturnsNoSeeds()
    {
        Regex rejected = new Regex(@"^\[GrassLayout\] Rejected");
        for (int i = 0; i < 5; i++)
        {
            LogAssert.Expect(LogType.Error, rejected);
        }

        Assert.IsEmpty(GrassLayout.Generate(0, 1, 1f, Vector3.zero, 0f, 10, 1));
        Assert.IsEmpty(GrassLayout.Generate(1, 1, float.NaN, Vector3.zero, 0f, 10, 1));
        Assert.IsEmpty(GrassLayout.Generate(1, 1, 1f, Vector3.zero, 0f, -1, 1));
        Assert.IsEmpty(GrassLayout.Generate(1, 1, 1f, Vector3.zero, float.PositiveInfinity, 10, 1));
        Assert.IsEmpty(GrassLayout.Generate(1, 1, 1f, new Vector3(float.NaN, 0f, 0f), 0f, 10, 1));
    }

    static float Define(string source, string name)
    {
        Match match = Regex.Match(source, @"#define\s+" + name + @"\s+([0-9.]+)");
        Assert.That(match.Success, Is.True, name);
        return float.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
    }

    [Test]
    public void HealLift_ShaderDefines_MatchTheComputeAndTheInstancing()
    {
        string compute = File.ReadAllText(Path.Combine(Application.dataPath, "Render/Shaders/Grass.compute"));
        string instancing = File.ReadAllText(Path.Combine(Application.dataPath, "Render/Shaders/GrassInstancing.hlsl"));

        float spike = Define(compute, "HL_SPIKE_MIN_HEIGHT") + Define(compute, "HL_SPIKE_HEIGHT_RANGE");

        Assert.AreEqual(GrassLayout.HealLift, 1f + Define(compute, "HL_HEAL_LIFT"), 0.00001f);
        Assert.AreEqual(GrassLayout.SpikeHeight, spike, 0.00001f);
        Assert.AreEqual(GrassLayout.SpikeHalfWidth, Define(instancing, "HL_SPIKE_HALF_WIDTH"), 0.00001f);
    }
}

}
