using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Environment
{
    public class HLEnvironmentScatterTests
    {
        GameObject _go;

        [SetUp]
        public void Setup()
        {
            _go = new GameObject("HLScatterTest");
        }

        [TearDown]
        public void Cleanup()
        {
            Object.DestroyImmediate(_go);
        }

        HLEnvironmentScatter Make(int seed)
        {
            HLEnvironmentScatter scatter = _go.AddComponent<HLEnvironmentScatter>();
            HLEnvironmentSettings settings = HLEnvironmentSettings.Default;
            settings.seed = seed;
            settings.counts = new HLEnvironmentCounts
            {
                boulders = 6,
                cairns = 3,
                monoliths = 2,
                mushroomTrees = 4,
                spiralFerns = 4,
                bladeRosettes = 4,
                sphereClusters = 4
            };
            scatter.settings = settings;
            return scatter;
        }

        Transform FirstPivot(HLEnvironmentScatter scatter, string name)
        {
            return Enumerable.Range(0, scatter.root.childCount)
                .Select(i => scatter.root.GetChild(i))
                .First(t => t.name == name);
        }

        Vector3[] PivotPositions(HLEnvironmentScatter scatter)
        {
            return Enumerable.Range(0, scatter.root.childCount)
                .Select(i => scatter.root.GetChild(i).position)
                .ToArray();
        }

        [Test]
        public void BuildSpawnsOnePivotPerItemOutsideTheGridAndIsDeterministic()
        {
            HLEnvironmentScatter scatter = Make(5);
            Rect grid = new Rect(-8f, -8f, 16f, 16f);

            scatter.Build(grid, 1f);

            Assert.That(scatter.items.Count, Is.GreaterThan(0));
            Assert.AreEqual(scatter.items.Count, scatter.root.childCount);
            Vector3[] first = PivotPositions(scatter);
            foreach (Vector3 point in first)
            {
                Assert.IsFalse(HLEnvironmentLayout.InsideMargin(grid, 1f, new Vector2(point.x, point.z)));
            }
            foreach (MeshRenderer meshRenderer in scatter.root.GetComponentsInChildren<MeshRenderer>())
            {
                // No part may reach into the grid itself
                Bounds b = meshRenderer.bounds;
                Assert.IsFalse(b.min.x > grid.xMin && b.max.x < grid.xMax && b.min.z > grid.yMin
                    && b.max.z < grid.yMax, meshRenderer.name);
            }

            scatter.Build(grid, 1f);

            Vector3[] second = PivotPositions(scatter);
            Assert.AreEqual(first, second);
        }

        [Test]
        public void AllPlantKindsSwayAndStonesDoNot()
        {
            HLEnvironmentScatter scatter = Make(9);

            scatter.Build(new Rect(-8f, -8f, 16f, 16f), 1f);

            int swaying = scatter.items.Count(i => i.kind == HLEnvironmentKind.MushroomTree
                || i.kind == HLEnvironmentKind.SpiralFern
                || i.kind == HLEnvironmentKind.SphereCluster || i.kind == HLEnvironmentKind.BladeRosette);
            Assert.AreEqual(swaying, scatter.swayingCount);
            Assert.DoesNotThrow(() => TestHelpers.InvokePrivate(scatter, "Update"));
        }

        [Test]
        public void MotionFreezesBeyondFogAndResumesWithoutAccumulating()
        {
            HLEnvironmentScatter scatter = Make(5);
            scatter.Build(new Rect(-8f, -8f, 16f, 16f), 1f);
            Transform plant = FirstPivot(scatter, "BladeRosette");
            Quaternion rest = plant.localRotation;

            scatter.Animate(10, Vector3.one * 1000f, 1f);
            Assert.AreEqual(rest, plant.localRotation);

            scatter.Animate(10, plant.position, 100f);
            Quaternion pose = plant.localRotation;
            Assert.That(Quaternion.Angle(rest, pose), Is.GreaterThan(0.001f));

            scatter.Animate(11, Vector3.one * 1000f, 1f);
            Assert.AreEqual(pose, plant.localRotation);

            scatter.Animate(10, plant.position, 100f);
            Assert.AreEqual(pose, plant.localRotation);
        }

        [Test]
        public void GustOpensFernJointsAndCapsHaveSeparateNod()
        {
            HLEnvironmentScatter scatter = Make(5);
            scatter.Build(new Rect(-8f, -8f, 16f, 16f), 1f);
            HLEnvironmentGust gust = _go.AddComponent<HLEnvironmentGust>();
            scatter.ConfigureMotion(null, gust, 100f);
            Transform fern = FirstPivot(scatter, "SpiralFern");
            Transform joint = fern.GetChild(0).GetChild(0).GetChild(1);
            scatter.Animate(10, Vector3.zero, 100f);
            float resting = joint.localEulerAngles.z;

            gust.GustAt(Vector3.forward, 1f, 2f, 9);
            scatter.Animate(10, Vector3.zero, 100f);

            Assert.That(Mathf.DeltaAngle(resting, joint.localEulerAngles.z), Is.GreaterThan(2f));
            Transform mushroom = FirstPivot(scatter, "MushroomTree");
            Transform cap = mushroom.Find("NoddingCap");
            Assert.IsNotNull(cap);
            Assert.AreEqual(2, cap.childCount);

            Quaternion before = cap.localRotation;
            scatter.Animate(11, Vector3.zero, 100f);

            Assert.That(Quaternion.Angle(before, cap.localRotation), Is.GreaterThan(0.001f));
        }

        [Test]
        public void SpawnSettleDecaysAndAbsoluteSamplingIsStable()
        {
            HLEnvironmentScatter scatter = Make(5);
            scatter.Build(new Rect(-8f, -8f, 16f, 16f), 1f);
            Transform plant = FirstPivot(scatter, "BladeRosette");

            TestHelpers.SetPrivateField(scatter, "_builtAt", 10d);
            scatter.Animate(10.1, Vector3.zero, 100f);
            Quaternion settling = plant.localRotation;
            TestHelpers.SetPrivateField(scatter, "_builtAt", 0d);
            scatter.Animate(10.1, Vector3.zero, 100f);

            Assert.That(Quaternion.Angle(settling, plant.localRotation), Is.GreaterThan(1f));

            Quaternion settled = plant.localRotation;
            scatter.Animate(10.1, Vector3.zero, 100f);

            Assert.AreEqual(settled, plant.localRotation);
        }

        [Test]
        public void ColourVariationIsRepeatableAndBounded()
        {
            Color basis = new Color(0.4f, 0.7f, 0.3f);

            Color a = HLEnvironmentScatter.VaryColor(basis, 5);

            Assert.AreEqual(a, HLEnvironmentScatter.VaryColor(basis, 5));
            Assert.AreNotEqual(a, HLEnvironmentScatter.VaryColor(basis, 99));
            Color.RGBToHSV(basis, out float h, out _, out float v);
            Color.RGBToHSV(a, out float h2, out _, out float v2);
            Assert.That(Mathf.Abs(Mathf.DeltaAngle(h * 360f, h2 * 360f)), Is.LessThanOrEqualTo(6.001f));
            Assert.That(v2 / v, Is.InRange(0.9199f, 1.0801f));
        }

        [Test]
        public void AnimateAllocatesNothingAfterWarmup()
        {
            HLEnvironmentScatter scatter = Make(5);
            scatter.Build(new Rect(-8f, -8f, 16f, 16f), 1f);
            for (int i = 0; i < 10; i++)
            {
                scatter.Animate(i, Vector3.zero, 100f);
            }

            long before = System.GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 100; i++)
            {
                scatter.Animate(i, Vector3.zero, 100f);
            }
            long bytes = System.GC.GetAllocatedBytesForCurrentThread() - before;

            Assert.AreEqual(0, bytes);
        }

        [Test]
        public void ClearRemovesEverything()
        {
            HLEnvironmentScatter scatter = Make(3);
            scatter.Build(new Rect(-8f, -8f, 16f, 16f), 1f);

            scatter.Clear();

            Assert.IsNull(scatter.root);
            Assert.AreEqual(0, _go.transform.childCount);
            Assert.AreEqual(0, scatter.swayingCount);
        }
    }
}
