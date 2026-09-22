using System.Linq;
using NUnit.Framework;
using UnityEngine;
namespace HealerLike.Render.Environment
{
    public class HLEnvironmentForegroundTests
    {
        static readonly Vector3 Position = new Vector3(0, 43.224f, -9.837f);
        static readonly Quaternion Rotation = Quaternion.Euler(73.70f, 0, 0);
        const float Fov = 40, Aspect = 9f / 16f, Ground = .5f;
        static readonly Rect Grid = new Rect(-8, -8, 16, 16);
        GameObject go;
        [SetUp] public void Setup() { go = new GameObject("HLForegroundTest"); }
        [TearDown] public void Cleanup() { Object.DestroyImmediate(go); }

        [Test] public void GroundHitMatchesTheMeasuredBottomEdge()
        {
            var left = HLEnvironmentForeground.GroundHit(Position, Rotation, Fov, Aspect, new Vector2(0, 0), Ground);
            var right = HLEnvironmentForeground.GroundHit(Position, Rotation, Fov, Aspect, new Vector2(1, 0), Ground);
            Assert.AreEqual(-12.6f, left.z, .1f); Assert.AreEqual(left.z, right.z, 1e-3f); Assert.AreEqual(-left.x, right.x, 1e-3f);
            Assert.That(right.x, Is.InRange(7.5f, 9f)); Assert.AreEqual(Ground, left.y, 1e-4f);
            var back = HLEnvironmentForeground.ToViewport(right, Position, Rotation, Fov, Aspect);
            Assert.AreEqual(1, back.x, 1e-3f); Assert.AreEqual(0, back.y, 1e-3f);
        }
        [Test] public void SameSeedGivesTheSameLayoutAndAnotherSeedDiffers()
        {
            var a = HLEnvironmentForeground.Layout(Position, Rotation, Fov, Aspect, Ground, 3);
            var b = HLEnvironmentForeground.Layout(Position, Rotation, Fov, Aspect, Ground, 3);
            var c = HLEnvironmentForeground.Layout(Position, Rotation, Fov, Aspect, Ground, 4);
            Assert.AreEqual(a.Count, b.Count);
            for (int i = 0; i < a.Count; i++)
            {
                Assert.AreEqual(a[i].Kind, b[i].Kind); Assert.AreEqual(a[i].Position, b[i].Position);
                Assert.AreEqual(a[i].Scale, b[i].Scale); Assert.AreEqual(a[i].Yaw, b[i].Yaw); Assert.AreEqual(a[i].Seed, b[i].Seed);
            }
            Assert.IsFalse(a.Select(i => i.Position).SequenceEqual(c.Select(i => i.Position)));
        }
        [Test] public void EachBottomCornerGetsLargeBouldersAndRosettesCroppedByTheFrame()
        {
            for (int seed = 0; seed < 24; seed++)
            {
                var items = HLEnvironmentForeground.Layout(Position, Rotation, Fov, Aspect, Ground, seed);
                foreach (float side in new[] { -1f, 1f })
                {
                    var corner = items.Where(i => Mathf.Sign(i.Position.x) == side).ToList();
                    Assert.That(corner.Count(i => i.Kind == HLForegroundKind.Boulder), Is.InRange(2, 3), "seed " + seed);
                    Assert.That(corner.Count(i => i.Kind == HLForegroundKind.Rosette), Is.InRange(1, 2), "seed " + seed);
                }
                foreach (var item in items)
                {
                    string label = $"seed {seed} {item.Kind} at {item.Position}";
                    Assert.That(item.Scale, item.Kind == HLForegroundKind.Boulder ? Is.InRange(1.4f, 2.45f) : Is.InRange(2.1f, 3.5f), label);
                    Assert.AreEqual(Ground, item.Position.y, label);
                    var vp = HLEnvironmentForeground.ToViewport(item.Position, Position, Rotation, Fov, Aspect);
                    Assert.That(vp.y, Is.LessThan(.2f), label); Assert.IsTrue(vp.x < .25f || vp.x > .75f, label);
                    // Partly outside: the footprint's outer-lower rim leaves the frame.
                    var rim = item.Position + new Vector3(Mathf.Sign(item.Position.x) * item.Radius, 0, -item.Radius) * .7071f;
                    var rv = HLEnvironmentForeground.ToViewport(rim, Position, Rotation, Fov, Aspect);
                    Assert.IsTrue(rv.x < 0 || rv.x > 1 || rv.y < 0, label);
                    // For the stage pose nothing reaches over the board.
                    Assert.That(item.Position.z + item.Radius, Is.LessThan(Grid.yMin), label);
                }
            }
        }
        [Test] public void InvalidInputThrows()
        {
            Assert.Catch<System.ArgumentException>(() => HLEnvironmentForeground.Layout(Position, Rotation, 0, Aspect, Ground, 1));
            Assert.Catch<System.ArgumentException>(() => HLEnvironmentForeground.Layout(Position, Rotation, Fov, 0, Ground, 1));
            Assert.Catch<System.ArgumentException>(() => HLEnvironmentForeground.Layout(Position, Quaternion.Euler(-30, 0, 0), Fov, Aspect, Ground, 1));
        }
        [Test] public void BuildMakesOneColliderFreeChildPerItemColouredThroughThePropertyBlock()
        {
            var foreground = go.AddComponent<HLEnvironmentForeground>();
            foreground.Configure(null, null, null, Ground, 5);
            Assert.DoesNotThrow(() => foreground.Build()); Assert.IsNull(foreground.Root);
            foreground.Build(Position, Rotation, Fov, Aspect);
            Assert.That(foreground.Items.Count, Is.InRange(6, 10));
            Assert.AreEqual(foreground.Items.Count, foreground.Root.childCount);
            for (int i = 0; i < foreground.Items.Count; i++)
            {
                var pivot = foreground.Root.GetChild(i);
                Assert.That(Vector3.Distance(foreground.Items[i].Position, pivot.position), Is.LessThan(1e-3f));
                Assert.That(pivot.childCount, Is.GreaterThanOrEqualTo(foreground.Items[i].Kind == HLForegroundKind.Boulder ? 1 : 7));
            }
            Assert.AreEqual(0, go.GetComponentsInChildren<Collider>().Length);
            var renderers = foreground.Root.GetComponentsInChildren<MeshRenderer>();
            Assert.That(renderers.Length, Is.GreaterThan(foreground.Items.Count));
            var block = new MaterialPropertyBlock();
            foreach (var r in renderers) { Assert.IsTrue(r.HasPropertyBlock(), r.name); r.GetPropertyBlock(block); Assert.That(block.GetColor("_BaseColor").a, Is.GreaterThan(0)); }
        }
        [Test] public void BuildFromACameraMatchesTheLayoutAndRebuildsIdentically()
        {
            var cameraObject = new GameObject("HLForegroundCamera");
            try
            {
                var camera = cameraObject.AddComponent<Camera>(); camera.fieldOfView = Fov; camera.aspect=Aspect;
                cameraObject.transform.SetPositionAndRotation(Position, Rotation);
                var foreground = go.AddComponent<HLEnvironmentForeground>();
                foreground.Configure(camera, null, null, Ground, 11);
                foreground.Build();
                var expected = HLEnvironmentForeground.Layout(Position, Rotation, Fov, Aspect, Ground, 11);
                Assert.AreEqual(expected.Count, foreground.Items.Count);
                // The camera transform round-trips the pose through its own storage; compare within float tolerance.
                for (int i = 0; i < expected.Count; i++) Assert.That(Vector3.Distance(expected[i].Position, foreground.Items[i].Position), Is.LessThan(1e-3f), i.ToString());
                int children = foreground.Root.childCount; foreground.Build();
                Assert.AreEqual(children, foreground.Root.childCount); Assert.AreEqual(1, go.transform.childCount);
            }
            finally { Object.DestroyImmediate(cameraObject); }
        }
        [Test] public void ClearRemovesEverything()
        {
            var foreground = go.AddComponent<HLEnvironmentForeground>(); foreground.Configure(null, null, null, Ground, 2);
            foreground.Build(Position, Rotation, Fov, Aspect);
            foreground.Clear(); Assert.IsNull(foreground.Root); Assert.AreEqual(0, go.transform.childCount);
        }
    }
}
