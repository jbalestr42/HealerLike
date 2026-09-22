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
        [Test] public void AllPlantKindsSwayAndStonesDoNot()
        {
            var scatter = Make(9); scatter.Build(new Rect(-8, -8, 16, 16), 1);
            int swaying = scatter.Items.Count(i => i.Kind == HLEnvironmentKind.MushroomTree || i.Kind == HLEnvironmentKind.SpiralFern || i.Kind == HLEnvironmentKind.SphereCluster || i.Kind == HLEnvironmentKind.BladeRosette);
            Assert.AreEqual(swaying, scatter.SwayingCount);
            Assert.DoesNotThrow(() => TestHelpers.InvokePrivate(scatter, "Update"));
        }
        [Test] public void MotionFreezesBeyondFogAndResumesWithoutAccumulating()
        {
            var scatter = Make(5); scatter.Build(new Rect(-8, -8, 16, 16), 1);
            var plant = Enumerable.Range(0, scatter.Root.childCount).Select(i => scatter.Root.GetChild(i)).First(t => t.name == "BladeRosette");
            var rest = plant.localRotation;
            scatter.Animate(10, Vector3.one * 1000, 1); Assert.AreEqual(rest, plant.localRotation);
            scatter.Animate(10, plant.position, 100); var pose = plant.localRotation;
            Assert.That(Quaternion.Angle(rest, pose), Is.GreaterThan(.001f));
            scatter.Animate(11, Vector3.one * 1000, 1); Assert.AreEqual(pose, plant.localRotation);
            scatter.Animate(10, plant.position, 100); Assert.AreEqual(pose, plant.localRotation);
        }
        [Test] public void GustOpensFernJointsAndCapsHaveSeparateNod()
        {
            var scatter = Make(5); scatter.Build(new Rect(-8, -8, 16, 16), 1);
            var gust = go.AddComponent<HLEnvironmentGust>(); scatter.ConfigureMotion(null, gust, 100);
            var fern = Enumerable.Range(0, scatter.Root.childCount).Select(i => scatter.Root.GetChild(i)).First(t => t.name == "SpiralFern");
            var joint = fern.GetChild(0).GetChild(0).GetChild(1);
            scatter.Animate(10, Vector3.zero, 100); float resting = joint.localEulerAngles.z;
            gust.GustAt(Vector3.forward, 1, 2, 9); scatter.Animate(10, Vector3.zero, 100);
            Assert.That(Mathf.DeltaAngle(resting, joint.localEulerAngles.z), Is.GreaterThan(2));
            var mushroom = Enumerable.Range(0, scatter.Root.childCount).Select(i => scatter.Root.GetChild(i)).First(t => t.name == "MushroomTree");
            var cap = mushroom.Find("NoddingCap"); Assert.IsNotNull(cap); Assert.AreEqual(2, cap.childCount);
            var before = cap.localRotation; scatter.Animate(11, Vector3.zero, 100);
            Assert.That(Quaternion.Angle(before, cap.localRotation), Is.GreaterThan(.001f));
        }
        [Test] public void SpawnSettleDecaysAndAbsoluteSamplingIsStable()
        {
            var scatter = Make(5); scatter.Build(new Rect(-8, -8, 16, 16), 1);
            var plant = Enumerable.Range(0, scatter.Root.childCount).Select(i => scatter.Root.GetChild(i)).First(t => t.name == "BladeRosette");
            TestHelpers.SetPrivateField(scatter, "builtAt", 10d);
            scatter.Animate(10.1, Vector3.zero, 100); var settling = plant.localRotation;
            TestHelpers.SetPrivateField(scatter, "builtAt", 0d);
            scatter.Animate(10.1, Vector3.zero, 100);
            Assert.That(Quaternion.Angle(settling, plant.localRotation), Is.GreaterThan(1));
            var settled = plant.localRotation; scatter.Animate(10.1, Vector3.zero, 100); Assert.AreEqual(settled, plant.localRotation);
        }
        [Test] public void ColourVariationIsRepeatableAndBounded()
        {
            Color basis = new Color(.4f, .7f, .3f);
            var a = HLEnvironmentScatter.VaryColor(basis, 5);
            Assert.AreEqual(a, HLEnvironmentScatter.VaryColor(basis, 5));
            Assert.AreNotEqual(a, HLEnvironmentScatter.VaryColor(basis, 99));
            Color.RGBToHSV(basis, out float h, out _, out float v);
            Color.RGBToHSV(a, out float h2, out _, out float v2);
            Assert.That(Mathf.Abs(Mathf.DeltaAngle(h * 360, h2 * 360)), Is.LessThanOrEqualTo(6.001f));
            Assert.That(v2 / v, Is.InRange(.9199f, 1.0801f));
        }
        [Test] public void AnimateAllocatesNothingAfterWarmup()
        {
            var scatter = Make(5); scatter.Build(new Rect(-8, -8, 16, 16), 1);
            for (int i = 0; i < 10; i++) scatter.Animate(i, Vector3.zero, 100);
            long before = System.GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 100; i++) scatter.Animate(i, Vector3.zero, 100);
            long bytes = System.GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.AreEqual(0, bytes);
        }
        [Test] public void ClearRemovesEverything()
        {
            var scatter = Make(3); scatter.Build(new Rect(-8, -8, 16, 16), 1);
            scatter.Clear(); Assert.IsNull(scatter.Root); Assert.AreEqual(0, go.transform.childCount); Assert.AreEqual(0, scatter.SwayingCount);
        }
    }
}
