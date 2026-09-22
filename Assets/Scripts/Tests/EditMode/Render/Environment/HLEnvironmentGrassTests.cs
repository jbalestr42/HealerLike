using System.Linq;
using NUnit.Framework;
using UnityEngine;
using HealerLike.Render.Grass;
namespace HealerLike.Render.Environment
{
    public class HLEnvironmentGrassTests
    {
        [Test] public void StripsTileTheRingWithoutTouchingTheGrid()
        {
            var grid = new Rect(-8, -8, 16, 16); var strips = HLEnvironmentGrass.Strips(grid, 16);
            Assert.AreEqual(4, strips.Length);
            float area = 0;
            for (int i = 0; i < strips.Length; i++)
            {
                area += strips[i].width * strips[i].height;
                Assert.IsFalse(strips[i].Overlaps(grid), "strip " + i);
                for (int j = i + 1; j < strips.Length; j++) Assert.IsFalse(strips[i].Overlaps(strips[j]), i + "/" + j);
            }
            Assert.AreEqual(48 * 48 - 16 * 16, area, 1e-3f);
            Assert.AreEqual(new Rect(-24, 8, 48, 16), strips[0]);
        }
        [Test] public void BudgetIsAreaTimesDensityCappedByTheGrassMaximum()
        {
            Assert.AreEqual(48 * 16 * 64, HLEnvironmentGrass.Budget(new Rect(0, 0, 48, 16), 64));
            Assert.AreEqual(HLGrassLayout.MaxBudget, HLEnvironmentGrass.Budget(new Rect(0, 0, 1000, 1000), 64));
            Assert.AreEqual(0, HLEnvironmentGrass.Budget(new Rect(0, 0, 4, 4), -1));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => HLEnvironmentGrass.Strips(new Rect(0, 0, 1, 1), 0));
        }
        [Test] public void BandsTileTheAnnulusInWholeCellsWithoutTouchingTheGrid()
        {
            var grid = new Rect(-8, -8, 16, 16); var strips = HLEnvironmentGrass.Bands(grid);
            float reach = HLEnvironmentGrass.DefaultWidths.Sum(), area = 0;
            Assert.AreEqual(24, reach);
            var outer = new Rect(grid.xMin - reach, grid.yMin - reach, grid.width + 2 * reach, grid.height + 2 * reach);
            for (int i = 0; i < strips.Length; i++)
            {
                var r = strips[i].Rect; area += r.width * r.height;
                Assert.IsFalse(r.Overlaps(grid), "strip " + i);
                Assert.IsTrue(r.xMin >= outer.xMin && r.yMin >= outer.yMin && r.xMax <= outer.xMax && r.yMax <= outer.yMax, "strip " + i);
                Assert.AreEqual(Mathf.Round(r.width), r.width); Assert.AreEqual(Mathf.Round(r.height), r.height);
                Assert.AreEqual(Mathf.Round(r.x), r.x); Assert.AreEqual(Mathf.Round(r.y), r.y);
                for (int j = i + 1; j < strips.Length; j++) Assert.IsFalse(r.Overlaps(strips[j].Rect), i + "/" + j);
            }
            Assert.AreEqual(outer.width * outer.height - grid.width * grid.height, area, 1e-3f);
            Assert.That(outer.yMin, Is.LessThan(-12.6f));
        }
        [Test] public void BandBudgetsStayUnderTheCapAndDensityFallsOutward()
        {
            var strips = HLEnvironmentGrass.Bands(new Rect(-8, -8, 16, 16));
            foreach (var s in strips)
            {
                Assert.That(s.Budget, Is.LessThanOrEqualTo(HLGrassLayout.MaxBudget));
                Assert.AreEqual(Mathf.RoundToInt(s.Rect.width * s.Rect.height * s.Density), s.Budget);
                Assert.AreEqual(HLEnvironmentGrass.BoardDensity * HLEnvironmentGrass.DefaultFractions[s.Band], s.Density, 1e-3f);
            }
            for (int band = 1; band < HLEnvironmentGrass.DefaultWidths.Length; band++)
                Assert.That(strips.Where(s => s.Band == band).Max(s => s.Density), Is.LessThan(strips.Where(s => s.Band == band - 1).Min(s => s.Density)));
            Assert.That(strips.Where(s => s.Band == 0).All(s => s.Rect.xMin >= -11 && s.Rect.xMax <= 11 && s.Rect.yMin >= -11 && s.Rect.yMax <= 11));
        }
        [Test] public void OverBudgetStripsAreSplitWithoutLosingDensity()
        {
            // One 16-wide band at full board density: top and bottom are 48 x 16 x 256 = two budgets each.
            var strips = HLEnvironmentGrass.Bands(new Rect(-8, -8, 16, 16), new float[] { 16 }, new[] { 1f }, 256);
            Assert.That(strips.Length, Is.GreaterThan(4));
            Assert.AreEqual((48 * 48 - 16 * 16) * 256, strips.Sum(s => s.Budget));
            foreach (var s in strips) Assert.That(s.Budget, Is.LessThanOrEqualTo(HLGrassLayout.MaxBudget));
            // An odd split still yields whole cells and no loss.
            var odd = HLEnvironmentGrass.Bands(new Rect(0, 0, 5, 3), new float[] { 7 }, new[] { 1f }, 3000);
            foreach (var s in odd) { Assert.AreEqual(Mathf.Round(s.Rect.width), s.Rect.width); Assert.That(s.Budget, Is.LessThanOrEqualTo(HLGrassLayout.MaxBudget)); }
            Assert.AreEqual((19 * 17 - 5 * 3) * 3000, odd.Sum(s => s.Budget));
        }
        [Test] public void BandsRejectInvalidInput()
        {
            var grid = new Rect(-8, -8, 16, 16);
            Assert.Catch<System.ArgumentException>(() => HLEnvironmentGrass.Bands(grid, null, new[] { 1f }, 256));
            Assert.Catch<System.ArgumentException>(() => HLEnvironmentGrass.Bands(grid, new float[] { 3, 5 }, new[] { 1f }, 256));
            Assert.Catch<System.ArgumentException>(() => HLEnvironmentGrass.Bands(grid, new float[0], new float[0], 256));
            Assert.Catch<System.ArgumentException>(() => HLEnvironmentGrass.Bands(grid, new[] { 2.5f }, new[] { 1f }, 256));
            Assert.Catch<System.ArgumentException>(() => HLEnvironmentGrass.Bands(grid, new float[] { 0 }, new[] { 1f }, 256));
            Assert.Catch<System.ArgumentException>(() => HLEnvironmentGrass.Bands(grid, new float[] { 3 }, new[] { 0f }, 256));
            Assert.Catch<System.ArgumentException>(() => HLEnvironmentGrass.Bands(grid, new float[] { 3 }, new[] { 1f }, float.NaN));
            Assert.Catch<System.ArgumentException>(() => HLEnvironmentGrass.Bands(new Rect(0, 0, 2.5f, 3), new float[] { 3 }, new[] { 1f }, 256));
        }
        [Test] public void EnsureCellsSizesTheProxyGridOnce()
        {
            var go = new GameObject("HLRingProxy");
            try
            {
                var proxy = go.AddComponent<GridManager>(); proxy.width = 48; proxy.height = 16; proxy.size = 1;
                HLEnvironmentGrass.EnsureCells(proxy); Assert.AreEqual(48 * 16, proxy.cells.Length);
                var cells = proxy.cells; HLEnvironmentGrass.EnsureCells(proxy); Assert.AreSame(cells, proxy.cells);
                Assert.AreEqual(new Vector2Int(47, 15), proxy.cells[proxy.cells.Length - 1].coord);
            }
            finally { Object.DestroyImmediate(go); }
        }
        [Test] public void LateUpdateWithoutRegistryOrFieldsIsSafe()
        {
            var go = new GameObject("HLRingGrass");
            try { var grass = go.AddComponent<HLEnvironmentGrass>(); grass.Configure(null, null, null);
                Assert.DoesNotThrow(() => { TestHelpers.InvokePrivate(grass, "Start"); TestHelpers.InvokePrivate(grass, "LateUpdate"); TestHelpers.InvokePrivate(grass, "OnDisable"); }); }
            finally { Object.DestroyImmediate(go); }
        }
    }
}
