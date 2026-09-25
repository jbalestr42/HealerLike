using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace HealerLike.Render.Environment
{

public class EnvironmentLayoutTests
{
    static readonly Rect grid = new Rect(-8f, -8f, 16f, 16f);

    [Test]
    public void Generate_PortraitHeading_RotatesTheNearExclusionAndKeepsTheWalkableBoardEmpty()
    {
        Rect board = new Rect(-8f, -6f, 16f, 12f);
        Rect localBoard = new Rect(-6f, -8f, 12f, 16f);
        Quaternion turn = Quaternion.Euler(0f, 90f, 0f);
        var original = EnvironmentLayout.Generate(EnvironmentSettings.Default, localBoard, 1f, 0.5f);
        var portrait = EnvironmentLayout.Generate(EnvironmentSettings.Default, board, 1f, 0.5f, 90f);
        Assert.AreEqual(original.Count, portrait.Count);
        for (int i = 0; i < original.Count; i++)
        {
            Assert.That(Vector3.Distance(turn * original[i].position, portrait[i].position), Is.LessThan(0.001f));
            Assert.IsFalse(EnvironmentLayout.InsideMargin(board, 1f,
                new Vector2(portrait[i].position.x, portrait[i].position.z)));
            if (portrait[i].kind == EnvironmentKind.Monolith || portrait[i].kind == EnvironmentKind.MushroomTree)
                Assert.That(portrait[i].position.x, Is.GreaterThanOrEqualTo(board.xMin - 0.001f));
        }
    }

    [Test]
    public void Generate_SameSeed_GivesSameLayout()
    {
        List<EnvironmentItem> a = EnvironmentLayout.Generate(EnvironmentSettings.Default, grid, 1f, 0.5f);
        List<EnvironmentItem> b = EnvironmentLayout.Generate(EnvironmentSettings.Default, grid, 1f, 0.5f);

        Assert.That(a.Count, Is.GreaterThan(0));
        Assert.AreEqual(a.Count, b.Count);
        for (int i = 0; i < a.Count; i++)
        {
            Assert.AreEqual(a[i].kind, b[i].kind);
            Assert.AreEqual(a[i].position, b[i].position);
            Assert.AreEqual(a[i].scale, b[i].scale);
            Assert.AreEqual(a[i].seed, b[i].seed);
            Assert.AreEqual(a[i].yaw, b[i].yaw);
        }
    }

    [TestCase(1707)]
    [TestCase(99)]
    public void Generate_FarShoulders_KeepLandmarksNearTheBoardAndClearOfLargeFoliage(int seed)
    {
        EnvironmentSettings settings = EnvironmentSettings.Default;
        settings.seed = seed;
        var items = EnvironmentLayout.Generate(settings, grid, 1f, 0.5f);
        EnvironmentItem stone = items.First(i => i.kind == EnvironmentKind.Monolith);
        EnvironmentItem mushroom = items.First(i => i.kind == EnvironmentKind.MushroomTree);
        Assert.That(stone.position.x, Is.LessThan(grid.center.x));
        Assert.That(mushroom.position.x, Is.GreaterThan(grid.center.x));
        foreach (EnvironmentItem landmark in new[] { stone, mushroom })
        {
            Vector2 anchor = new Vector2(landmark.position.x, landmark.position.z);
            Assert.IsFalse(EnvironmentLayout.InsideMargin(grid, 1f, anchor));
            Assert.That(anchor.y, Is.InRange(grid.yMax + 1f, grid.yMax + 3f));
            foreach (EnvironmentItem plant in items.Where(i => i.kind == EnvironmentKind.BladeRosette
                                                               || i.kind == EnvironmentKind.SpiralFern))
                Assert.That(Vector2.Distance(anchor, new Vector2(plant.position.x, plant.position.z)),
                    Is.GreaterThanOrEqualTo(2f));
        }
    }

    [Test]
    public void Generate_DifferentSeed_GivesDifferentLayout()
    {
        EnvironmentSettings settings = EnvironmentSettings.Default;
        List<EnvironmentItem> a = EnvironmentLayout.Generate(settings, grid, 1f, 0.5f);
        settings.seed++;

        List<EnvironmentItem> b = EnvironmentLayout.Generate(settings, grid, 1f, 0.5f);

        Assert.IsFalse(a.Select(i => i.position).SequenceEqual(b.Select(i => i.position)));
    }

    [Test]
    public void Generate_AnySeed_PlacesNothingInsideGridMargin()
    {
        foreach (int seed in new int[] { 1, 1707, 99 })
        {
            EnvironmentSettings settings = EnvironmentSettings.Default;
            settings.seed = seed;
            foreach (EnvironmentItem item in EnvironmentLayout.Generate(settings, grid, 1f, 0.5f))
            {
                Vector2 point = new Vector2(item.position.x, item.position.z);
                Assert.IsFalse(EnvironmentLayout.InsideMargin(grid, 1f, point), $"{item.kind} at {point}");
                Assert.That(EnvironmentLayout.EdgeDistance(grid, point),
                    Is.LessThanOrEqualTo(1f + settings.ringDistance + 0.001f));
                Assert.AreEqual(0.5f, item.position.y);
            }
        }
    }

    [Test]
    public void Generate_HugeCounts_AreCappedPerKind()
    {
        EnvironmentSettings settings = EnvironmentSettings.Default;
        settings.counts = new EnvironmentCounts
        {
            boulders = 5,
            cairns = 0,
            monoliths = 2,
            mushroomTrees = 3,
            spiralFerns = 100000,
            bladeRosettes = 1,
            sphereClusters = 4
        };

        List<EnvironmentItem> items = EnvironmentLayout.Generate(settings, grid, 1f, 0f);

        foreach (EnvironmentKind kind in System.Enum.GetValues(typeof(EnvironmentKind)))
        {
            int cap = System.Math.Min(settings.counts[kind], EnvironmentLayout.MaxPerKind);
            Assert.That(items.Count(i => i.kind == kind), Is.LessThanOrEqualTo(cap), kind.ToString());
        }
        Assert.That(items.Count(i => i.kind == EnvironmentKind.SpiralFern),
            Is.LessThanOrEqualTo(EnvironmentLayout.MaxPerKind));
    }

    [Test]
    public void Generate_Monoliths_StayFarSideAndTallKindsOffCameraSide()
    {
        EnvironmentSettings settings = EnvironmentSettings.Default;
        settings.counts.monoliths = 12;

        List<EnvironmentItem> items = EnvironmentLayout.Generate(settings, grid, 1f, 0f);

        Assert.That(items.Count(i => i.kind == EnvironmentKind.Monolith), Is.GreaterThan(0));
        foreach (EnvironmentItem monolith in items.Where(i => i.kind == EnvironmentKind.Monolith))
        {
            Assert.That(monolith.position.z, Is.GreaterThan(grid.yMax + 1f));
            Assert.That(monolith.scale, Is.GreaterThanOrEqualTo(1.6f));
        }
        Assert.IsFalse(items.Any(i => i.kind == EnvironmentKind.MushroomTree && i.position.z < grid.yMin));
    }

    [Test]
    public void Generate_Default_ThinsOutAndGrowsStonesWithDistance()
    {
        List<EnvironmentItem> items = EnvironmentLayout.Generate(EnvironmentSettings.Default, grid, 1f, 0f);

        int near = items.Count(i => i.distance01 < 0.5f);
        int far = items.Count(i => i.distance01 >= 0.5f);
        Assert.That(near, Is.GreaterThan(far));
        List<EnvironmentItem> boulders = items.Where(i => i.kind == EnvironmentKind.Boulder).ToList();
        float nearScale = boulders.Where(i => i.distance01 < 0.3f).Average(i => i.scale);
        float farScale = boulders.Where(i => i.distance01 >= 0.5f)
            .DefaultIfEmpty(boulders[0])
            .Average(i => i.scale);
        Assert.That(farScale, Is.GreaterThan(nearScale));
    }

    [Test]
    public void Generate_InvalidInput_LogsAndReturnsEmpty()
    {
        EnvironmentSettings settings = EnvironmentSettings.Default;
        settings.ringDistance = 0f;
        LogAssert.Expect(LogType.Error, new Regex(@"^\[EnvironmentLayout\] Rejected grid"));
        LogAssert.Expect(LogType.Error, new Regex(@"^\[EnvironmentLayout\] Rejected grid"));

        Assert.IsEmpty(EnvironmentLayout.Generate(EnvironmentSettings.Default, grid, 0f, 0f));
        Assert.IsEmpty(EnvironmentLayout.Generate(settings, grid, 1f, 0f));
    }
}

}
