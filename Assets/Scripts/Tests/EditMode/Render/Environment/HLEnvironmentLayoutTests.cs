using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Environment
{
    public class HLEnvironmentLayoutTests
    {
        static readonly Rect grid = new Rect(-8f, -8f, 16f, 16f);

        [Test]
        public void SameSeedGivesTheSameLayout()
        {
            List<HLEnvironmentItem> a = HLEnvironmentLayout.Generate(HLEnvironmentSettings.Default, grid, 1f, 0.5f);
            List<HLEnvironmentItem> b = HLEnvironmentLayout.Generate(HLEnvironmentSettings.Default, grid, 1f, 0.5f);

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

        [Test]
        public void DifferentSeedGivesADifferentLayout()
        {
            HLEnvironmentSettings settings = HLEnvironmentSettings.Default;
            List<HLEnvironmentItem> a = HLEnvironmentLayout.Generate(settings, grid, 1f, 0.5f);
            settings.seed++;

            List<HLEnvironmentItem> b = HLEnvironmentLayout.Generate(settings, grid, 1f, 0.5f);

            Assert.IsFalse(a.Select(i => i.position).SequenceEqual(b.Select(i => i.position)));
        }

        [Test]
        public void NothingInsideTheGridOrItsOneCellMargin()
        {
            foreach (int seed in new int[] { 1, 1707, 99 })
            {
                HLEnvironmentSettings settings = HLEnvironmentSettings.Default;
                settings.seed = seed;
                foreach (HLEnvironmentItem item in HLEnvironmentLayout.Generate(settings, grid, 1f, 0.5f))
                {
                    Vector2 point = new Vector2(item.position.x, item.position.z);
                    Assert.IsFalse(HLEnvironmentLayout.InsideMargin(grid, 1f, point), $"{item.kind} at {point}");
                    Assert.That(HLEnvironmentLayout.EdgeDistance(grid, point),
                        Is.LessThanOrEqualTo(1f + settings.ringDistance + 0.001f));
                    Assert.AreEqual(0.5f, item.position.y);
                }
            }
        }

        [Test]
        public void CountsAreBoundedPerKindAndByTheCap()
        {
            HLEnvironmentSettings settings = HLEnvironmentSettings.Default;
            settings.counts = new HLEnvironmentCounts
            {
                boulders = 5,
                cairns = 0,
                monoliths = 2,
                mushroomTrees = 3,
                spiralFerns = 100000,
                bladeRosettes = 1,
                sphereClusters = 4
            };

            List<HLEnvironmentItem> items = HLEnvironmentLayout.Generate(settings, grid, 1f, 0f);

            foreach (HLEnvironmentKind kind in System.Enum.GetValues(typeof(HLEnvironmentKind)))
            {
                int cap = System.Math.Min(settings.counts[kind], HLEnvironmentLayout.MaxPerKind);
                Assert.That(items.Count(i => i.kind == kind), Is.LessThanOrEqualTo(cap), kind.ToString());
            }
            Assert.That(items.Count(i => i.kind == HLEnvironmentKind.SpiralFern),
                Is.LessThanOrEqualTo(HLEnvironmentLayout.MaxPerKind));
        }

        [Test]
        public void MonolithsAreFarSideAndTallKindsNeverOnTheCameraSide()
        {
            HLEnvironmentSettings settings = HLEnvironmentSettings.Default;
            settings.counts.monoliths = 12;

            List<HLEnvironmentItem> items = HLEnvironmentLayout.Generate(settings, grid, 1f, 0f);

            Assert.That(items.Count(i => i.kind == HLEnvironmentKind.Monolith), Is.GreaterThan(0));
            foreach (HLEnvironmentItem monolith in items.Where(i => i.kind == HLEnvironmentKind.Monolith))
            {
                Assert.That(monolith.position.z, Is.GreaterThan(grid.yMax + 1f));
                Assert.That(monolith.scale, Is.GreaterThanOrEqualTo(1.6f));
            }
            Assert.IsFalse(items.Any(i => i.kind == HLEnvironmentKind.MushroomTree && i.position.z < grid.yMin));
        }

        [Test]
        public void DensityFallsOffAndStonesGrowWithDistance()
        {
            List<HLEnvironmentItem> items = HLEnvironmentLayout.Generate(HLEnvironmentSettings.Default, grid, 1f, 0f);

            int near = items.Count(i => i.distance01 < 0.5f);
            int far = items.Count(i => i.distance01 >= 0.5f);
            Assert.That(near, Is.GreaterThan(far));
            List<HLEnvironmentItem> boulders = items.Where(i => i.kind == HLEnvironmentKind.Boulder).ToList();
            float nearScale = boulders.Where(i => i.distance01 < 0.3f).Average(i => i.scale);
            float farScale = boulders.Where(i => i.distance01 >= 0.5f)
                .DefaultIfEmpty(boulders[0])
                .Average(i => i.scale);
            Assert.That(farScale, Is.GreaterThan(nearScale));
        }

        [Test]
        public void InvalidInputThrows()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(() =>
                HLEnvironmentLayout.Generate(HLEnvironmentSettings.Default, grid, 0f, 0f));
            HLEnvironmentSettings settings = HLEnvironmentSettings.Default;
            settings.ringDistance = 0f;
            Assert.Throws<System.ArgumentOutOfRangeException>(() =>
                HLEnvironmentLayout.Generate(settings, grid, 1f, 0f));
        }
    }
}
