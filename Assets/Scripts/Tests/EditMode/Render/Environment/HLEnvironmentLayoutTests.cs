using System.Linq;
using NUnit.Framework;
using UnityEngine;
namespace HealerLike.Render.Environment
{
    public class HLEnvironmentLayoutTests
    {
        static readonly Rect Grid = new Rect(-8, -8, 16, 16);
        [Test] public void SameSeedGivesTheSameLayout()
        {
            var a = HLEnvironmentLayout.Generate(HLEnvironmentSettings.Default, Grid, 1, .5f);
            var b = HLEnvironmentLayout.Generate(HLEnvironmentSettings.Default, Grid, 1, .5f);
            Assert.That(a.Count, Is.GreaterThan(0)); Assert.AreEqual(a.Count, b.Count);
            for (int i = 0; i < a.Count; i++)
            {
                Assert.AreEqual(a[i].Kind, b[i].Kind); Assert.AreEqual(a[i].Position, b[i].Position);
                Assert.AreEqual(a[i].Scale, b[i].Scale); Assert.AreEqual(a[i].Seed, b[i].Seed); Assert.AreEqual(a[i].Yaw, b[i].Yaw);
            }
        }
        [Test] public void DifferentSeedGivesADifferentLayout()
        {
            var settings = HLEnvironmentSettings.Default; var a = HLEnvironmentLayout.Generate(settings, Grid, 1, .5f);
            settings.seed++; var b = HLEnvironmentLayout.Generate(settings, Grid, 1, .5f);
            Assert.IsFalse(a.Select(i => i.Position).SequenceEqual(b.Select(i => i.Position)));
        }
        [Test] public void NothingInsideTheGridOrItsOneCellMargin()
        {
            foreach (int seed in new[] { 1, 1707, 99 })
            {
                var settings = HLEnvironmentSettings.Default; settings.seed = seed;
                foreach (var item in HLEnvironmentLayout.Generate(settings, Grid, 1, .5f))
                {
                    var p = new Vector2(item.Position.x, item.Position.z);
                    Assert.IsFalse(HLEnvironmentLayout.InsideMargin(Grid, 1, p), $"{item.Kind} at {p}");
                    Assert.That(HLEnvironmentLayout.EdgeDistance(Grid, p), Is.LessThanOrEqualTo(1 + settings.ringDistance + 1e-3f));
                    Assert.AreEqual(.5f, item.Position.y);
                }
            }
        }
        [Test] public void CountsAreBoundedPerKindAndByTheCap()
        {
            var settings = HLEnvironmentSettings.Default;
            settings.counts = new HLEnvironmentCounts { boulders = 5, cairns = 0, monoliths = 2, mushroomTrees = 3, spiralFerns = 100000, bladeRosettes = 1, sphereClusters = 4 };
            var items = HLEnvironmentLayout.Generate(settings, Grid, 1, 0);
            foreach (HLEnvironmentKind kind in System.Enum.GetValues(typeof(HLEnvironmentKind)))
                Assert.That(items.Count(i => i.Kind == kind), Is.LessThanOrEqualTo(System.Math.Min(settings.counts[kind], HLEnvironmentLayout.MaxPerKind)), kind.ToString());
            Assert.That(items.Count(i => i.Kind == HLEnvironmentKind.SpiralFern), Is.LessThanOrEqualTo(HLEnvironmentLayout.MaxPerKind));
        }
        [Test] public void MonolithsAreFarSideAndTallKindsNeverOnTheCameraSide()
        {
            var settings = HLEnvironmentSettings.Default; settings.counts.monoliths = 12;
            var items = HLEnvironmentLayout.Generate(settings, Grid, 1, 0);
            Assert.That(items.Count(i => i.Kind == HLEnvironmentKind.Monolith), Is.GreaterThan(0));
            foreach (var m in items.Where(i => i.Kind == HLEnvironmentKind.Monolith)) { Assert.That(m.Position.z, Is.GreaterThan(Grid.yMax + 1)); Assert.That(m.Scale, Is.GreaterThanOrEqualTo(1.6f)); }
            Assert.IsFalse(items.Any(i => i.Kind == HLEnvironmentKind.MushroomTree && i.Position.z < Grid.yMin));
        }
        [Test] public void DensityFallsOffAndStonesGrowWithDistance()
        {
            var items = HLEnvironmentLayout.Generate(HLEnvironmentSettings.Default, Grid, 1, 0);
            int near = items.Count(i => i.Distance01 < .5f), far = items.Count(i => i.Distance01 >= .5f);
            Assert.That(near, Is.GreaterThan(far));
            var boulders = items.Where(i => i.Kind == HLEnvironmentKind.Boulder).ToList();
            float nearScale = boulders.Where(i => i.Distance01 < .3f).Average(i => i.Scale), farScale = boulders.Where(i => i.Distance01 >= .5f).DefaultIfEmpty(boulders[0]).Average(i => i.Scale);
            Assert.That(farScale, Is.GreaterThan(nearScale));
        }
        [Test] public void InvalidInputThrows()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(() => HLEnvironmentLayout.Generate(HLEnvironmentSettings.Default, Grid, 0, 0));
            var settings = HLEnvironmentSettings.Default; settings.ringDistance = 0;
            Assert.Throws<System.ArgumentOutOfRangeException>(() => HLEnvironmentLayout.Generate(settings, Grid, 1, 0));
        }
    }
}
