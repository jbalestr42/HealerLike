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
