using System.Linq;
using HealerLike.Render.Grass;
using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Environment
{
    public class HLEnvironmentGrassTests
    {
        [Test]
        public void StripsTileTheRingWithoutTouchingTheGrid()
        {
            Rect grid = new Rect(-8f, -8f, 16f, 16f);

            Rect[] strips = HLEnvironmentGrass.Strips(grid, 16f);

            Assert.AreEqual(4, strips.Length);
            float area = 0f;
            for (int i = 0; i < strips.Length; i++)
            {
                area += strips[i].width * strips[i].height;
                Assert.IsFalse(strips[i].Overlaps(grid), "strip " + i);
                for (int j = i + 1; j < strips.Length; j++)
                {
                    Assert.IsFalse(strips[i].Overlaps(strips[j]), i + "/" + j);
                }
            }
            Assert.AreEqual(48 * 48 - 16 * 16, area, 0.001f);
            Assert.AreEqual(new Rect(-24f, 8f, 48f, 16f), strips[0]);
        }

        [Test]
        public void BudgetIsAreaTimesDensityCappedByTheGrassMaximum()
        {
            Assert.AreEqual(48 * 16 * 64, HLEnvironmentGrass.Budget(new Rect(0f, 0f, 48f, 16f), 64f));
            Assert.AreEqual(HLGrassLayout.MaxBudget, HLEnvironmentGrass.Budget(new Rect(0f, 0f, 1000f, 1000f), 64f));
            Assert.AreEqual(0, HLEnvironmentGrass.Budget(new Rect(0f, 0f, 4f, 4f), -1f));
            Assert.Throws<System.ArgumentOutOfRangeException>(() =>
                HLEnvironmentGrass.Strips(new Rect(0f, 0f, 1f, 1f), 0f));
        }

        [Test]
        public void BandsTileTheAnnulusInWholeCellsWithoutTouchingTheGrid()
        {
            Rect grid = new Rect(-8f, -8f, 16f, 16f);

            HLRingStrip[] strips = HLEnvironmentGrass.Bands(grid);

            float reach = HLEnvironmentGrass.DefaultWidths.Sum();
            float area = 0f;
            Assert.AreEqual(24f, reach);
            Rect outer = new Rect(grid.xMin - reach, grid.yMin - reach,
                grid.width + 2f * reach, grid.height + 2f * reach);
            for (int i = 0; i < strips.Length; i++)
            {
                Rect rect = strips[i].rect;
                area += rect.width * rect.height;
                Assert.IsFalse(rect.Overlaps(grid), "strip " + i);
                bool isInside = rect.xMin >= outer.xMin && rect.yMin >= outer.yMin && rect.xMax <= outer.xMax
                    && rect.yMax <= outer.yMax;
                Assert.IsTrue(isInside, "strip " + i);
                Assert.AreEqual(Mathf.Round(rect.width), rect.width);
                Assert.AreEqual(Mathf.Round(rect.height), rect.height);
                Assert.AreEqual(Mathf.Round(rect.x), rect.x);
                Assert.AreEqual(Mathf.Round(rect.y), rect.y);
                for (int j = i + 1; j < strips.Length; j++)
                {
                    Assert.IsFalse(rect.Overlaps(strips[j].rect), i + "/" + j);
                }
            }
            Assert.AreEqual(outer.width * outer.height - grid.width * grid.height, area, 0.001f);
            Assert.That(outer.yMin, Is.LessThan(-12.6f));
        }

        [Test]
        public void BandBudgetsStayUnderTheCapAndDensityFallsOutward()
        {
            HLRingStrip[] strips = HLEnvironmentGrass.Bands(new Rect(-8f, -8f, 16f, 16f));

            foreach (HLRingStrip strip in strips)
            {
                Assert.That(strip.budget, Is.LessThanOrEqualTo(HLGrassLayout.MaxBudget));
                Assert.AreEqual(Mathf.RoundToInt(strip.rect.width * strip.rect.height * strip.density), strip.budget);
                Assert.AreEqual(HLEnvironmentGrass.BoardDensity * HLEnvironmentGrass.DefaultFractions[strip.band],
                    strip.density, 0.001f);
            }
            for (int band = 1; band < HLEnvironmentGrass.DefaultWidths.Length; band++)
            {
                float outerMax = strips.Where(s => s.band == band).Max(s => s.density);
                float innerMin = strips.Where(s => s.band == band - 1).Min(s => s.density);
                Assert.That(outerMax, Is.LessThan(innerMin));
            }
            bool isNearBoard = strips.Where(s => s.band == 0)
                .All(s => s.rect.xMin >= -11f && s.rect.xMax <= 11f && s.rect.yMin >= -11f && s.rect.yMax <= 11f);
            Assert.That(isNearBoard);
        }

        [Test]
        public void OverBudgetStripsAreSplitWithoutLosingDensity()
        {
            // One 16-wide band at full board density: top and bottom are 48 x 16 x 256 = two budgets each
            HLRingStrip[] strips = HLEnvironmentGrass.Bands(new Rect(-8f, -8f, 16f, 16f), new float[] { 16f },
                new float[] { 1f }, 256f);

            Assert.That(strips.Length, Is.GreaterThan(4));
            Assert.AreEqual((48 * 48 - 16 * 16) * 256, strips.Sum(s => s.budget));
            foreach (HLRingStrip strip in strips)
            {
                Assert.That(strip.budget, Is.LessThanOrEqualTo(HLGrassLayout.MaxBudget));
            }

            // An odd split still yields whole cells and no loss
            HLRingStrip[] odd = HLEnvironmentGrass.Bands(new Rect(0f, 0f, 5f, 3f), new float[] { 7f },
                new float[] { 1f }, 3000f);

            foreach (HLRingStrip strip in odd)
            {
                Assert.AreEqual(Mathf.Round(strip.rect.width), strip.rect.width);
                Assert.That(strip.budget, Is.LessThanOrEqualTo(HLGrassLayout.MaxBudget));
            }
            Assert.AreEqual((19 * 17 - 5 * 3) * 3000, odd.Sum(s => s.budget));
        }

        [Test]
        public void BandsRejectInvalidInput()
        {
            Rect grid = new Rect(-8f, -8f, 16f, 16f);
            Assert.Catch<System.ArgumentException>(() =>
                HLEnvironmentGrass.Bands(grid, null, new float[] { 1f }, 256f));
            Assert.Catch<System.ArgumentException>(() =>
                HLEnvironmentGrass.Bands(grid, new float[] { 3f, 5f }, new float[] { 1f }, 256f));
            Assert.Catch<System.ArgumentException>(() =>
                HLEnvironmentGrass.Bands(grid, new float[0], new float[0], 256f));
            Assert.Catch<System.ArgumentException>(() =>
                HLEnvironmentGrass.Bands(grid, new float[] { 2.5f }, new float[] { 1f }, 256f));
            Assert.Catch<System.ArgumentException>(() =>
                HLEnvironmentGrass.Bands(grid, new float[] { 0f }, new float[] { 1f }, 256f));
            Assert.Catch<System.ArgumentException>(() =>
                HLEnvironmentGrass.Bands(grid, new float[] { 3f }, new float[] { 0f }, 256f));
            Assert.Catch<System.ArgumentException>(() =>
                HLEnvironmentGrass.Bands(grid, new float[] { 3f }, new float[] { 1f }, float.NaN));
            Rect halfCellGrid = new Rect(0f, 0f, 2.5f, 3f);
            Assert.Catch<System.ArgumentException>(() =>
                HLEnvironmentGrass.Bands(halfCellGrid, new float[] { 3f }, new float[] { 1f }, 256f));
        }

        [Test]
        public void EnsureCellsSizesTheProxyGridOnce()
        {
            GameObject go = new GameObject("HLRingProxy");
            try
            {
                GridManager proxy = go.AddComponent<GridManager>();
                proxy.width = 48;
                proxy.height = 16;
                proxy.size = 1f;

                HLEnvironmentGrass.EnsureCells(proxy);

                Assert.AreEqual(48 * 16, proxy.cells.Length);
                GridCell[] cells = proxy.cells;

                HLEnvironmentGrass.EnsureCells(proxy);

                Assert.AreSame(cells, proxy.cells);
                Assert.AreEqual(new Vector2Int(47, 15), proxy.cells[proxy.cells.Length - 1].coord);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void LateUpdateWithoutRegistryOrFieldsIsSafe()
        {
            GameObject go = new GameObject("HLRingGrass");
            try
            {
                HLEnvironmentGrass grass = go.AddComponent<HLEnvironmentGrass>();
                grass.Configure(null, null, null);

                Assert.DoesNotThrow(() =>
                {
                    TestHelpers.InvokePrivate(grass, "Start");
                    TestHelpers.InvokePrivate(grass, "LateUpdate");
                    TestHelpers.InvokePrivate(grass, "OnDisable");
                });
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}
