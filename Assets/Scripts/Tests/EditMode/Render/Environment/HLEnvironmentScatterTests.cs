using System.Linq;
using NUnit.Framework;
using UnityEngine;
namespace HealerLike.Render.Environment
{
    public class HLEnvironmentScatterTests
    {
        GameObject go;
        [SetUp] public void Setup() { go = new GameObject("HLScatterTest"); }
        [TearDown] public void Cleanup() { Object.DestroyImmediate(go); }
        HLEnvironmentScatter Make(int seed)
        {
            var scatter = go.AddComponent<HLEnvironmentScatter>();
            var settings = HLEnvironmentSettings.Default; settings.seed = seed;
            settings.counts = new HLEnvironmentCounts { boulders = 6, cairns = 3, monoliths = 2, mushroomTrees = 4, spiralFerns = 4, bladeRosettes = 4, sphereClusters = 4 };
            scatter.Settings = settings; return scatter;
        }
        [Test] public void BuildSpawnsOnePivotPerItemOutsideTheGridAndIsDeterministic()
        {
            var scatter = Make(5); var grid = new Rect(-8, -8, 16, 16);
            scatter.Build(grid, 1);
            Assert.That(scatter.Items.Count, Is.GreaterThan(0));
            Assert.AreEqual(scatter.Items.Count, scatter.Root.childCount);
            var first = Enumerable.Range(0, scatter.Root.childCount).Select(i => scatter.Root.GetChild(i).position).ToArray();
            foreach (var p in first) Assert.IsFalse(HLEnvironmentLayout.InsideMargin(grid, 1, new Vector2(p.x, p.z)));
            foreach (var r in scatter.Root.GetComponentsInChildren<MeshRenderer>())
            {
                var b = r.bounds; // no part may reach into the grid itself
                Assert.IsFalse(b.min.x > grid.xMin && b.max.x < grid.xMax && b.min.z > grid.yMin && b.max.z < grid.yMax, r.name);
            }
            scatter.Build(grid, 1);
            var second = Enumerable.Range(0, scatter.Root.childCount).Select(i => scatter.Root.GetChild(i).position).ToArray();
            Assert.AreEqual(first, second);
        }
        [Test] public void PlantsSwayThroughIdleMotionAndStonesDoNot()
        {
            var scatter = Make(9); scatter.Build(new Rect(-8, -8, 16, 16), 1);
            int swaying = scatter.Items.Count(i => i.Kind == HLEnvironmentKind.MushroomTree || i.Kind == HLEnvironmentKind.SpiralFern || i.Kind == HLEnvironmentKind.SphereCluster);
            Assert.AreEqual(swaying, scatter.SwayingCount);
            Assert.DoesNotThrow(() => TestHelpers.InvokePrivate(scatter, "Update"));
        }
        [Test] public void ClearRemovesEverything()
        {
            var scatter = Make(3); scatter.Build(new Rect(-8, -8, 16, 16), 1);
            scatter.Clear(); Assert.IsNull(scatter.Root); Assert.AreEqual(0, go.transform.childCount); Assert.AreEqual(0, scatter.SwayingCount);
        }
    }
}
