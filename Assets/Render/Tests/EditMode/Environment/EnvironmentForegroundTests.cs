using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using HealerLike.Render.Creatures;
using HealerLike.Render.Stage;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace HealerLike.Render.Environment
{
    public class EnvironmentForegroundTests
    {
        static readonly Vector3 position = new Vector3(0f, 43.224f, -9.837f);
        static readonly Quaternion rotation = Quaternion.Euler(73.70f, 0f, 0f);
        static readonly float fov = 40f;
        static readonly float aspect = 9f / 16f;
        static readonly float ground = 0.5f;
        static readonly Rect grid = new Rect(-8f, -8f, 16f, 16f);

        GameObject _go;

        [SetUp]
        public void Setup()
        {
            _go = new GameObject("HLForegroundTest");
        }

        [TearDown]
        public void Cleanup()
        {
            // Stones are generated per seed; the baked primitive meshes are assets and stay
            foreach (MeshFilter filter in _go.GetComponentsInChildren<MeshFilter>(true))
            {
                if (filter.sharedMesh != null && !EditorUtility.IsPersistent(filter.sharedMesh))
                {
                    Object.DestroyImmediate(filter.sharedMesh);
                }
            }

            Object.DestroyImmediate(_go);
        }

        static PrimitiveMeshes LoadMeshes()
        {
            return AssetDatabase.LoadAssetAtPath<PrimitiveMeshes>("Assets/Render/Creatures/Data/PrimitiveMeshes.asset");
        }

        [Test]
        public void GroundHitMatchesTheMeasuredBottomEdge()
        {
            Assert.IsTrue(EnvironmentForeground.GroundHit(position, rotation, fov, aspect, new Vector2(0f, 0f), ground,
                out Vector3 left));
            Assert.IsTrue(EnvironmentForeground.GroundHit(position, rotation, fov, aspect, new Vector2(1f, 0f), ground,
                out Vector3 right));

            Assert.AreEqual(-12.6f, left.z, 0.1f);
            Assert.AreEqual(left.z, right.z, 0.001f);
            Assert.AreEqual(-left.x, right.x, 0.001f);
            Assert.That(right.x, Is.InRange(7.5f, 9f));
            Assert.AreEqual(ground, left.y, 0.0001f);

            Vector2 back = EnvironmentForeground.ToViewport(right, position, rotation, fov, aspect);
            Assert.AreEqual(1f, back.x, 0.001f);
            Assert.AreEqual(0f, back.y, 0.001f);
        }

        [Test]
        public void SameSeedGivesTheSameLayoutAndAnotherSeedDiffers()
        {
            List<ForegroundItem> a = EnvironmentForeground.Layout(position, rotation, fov, aspect, ground, 3);
            List<ForegroundItem> b = EnvironmentForeground.Layout(position, rotation, fov, aspect, ground, 3);
            List<ForegroundItem> c = EnvironmentForeground.Layout(position, rotation, fov, aspect, ground, 4);

            Assert.AreEqual(a.Count, b.Count);
            for (int i = 0; i < a.Count; i++)
            {
                Assert.AreEqual(a[i].kind, b[i].kind);
                Assert.AreEqual(a[i].position, b[i].position);
                Assert.AreEqual(a[i].scale, b[i].scale);
                Assert.AreEqual(a[i].yaw, b[i].yaw);
                Assert.AreEqual(a[i].seed, b[i].seed);
            }
            Assert.IsFalse(a.Select(i => i.position).SequenceEqual(c.Select(i => i.position)));
        }

        [Test]
        public void EachBottomCornerGetsLargeBouldersAndRosettesCroppedByTheFrame()
        {
            for (int seed = 0; seed < 24; seed++)
            {
                List<ForegroundItem> items = EnvironmentForeground.Layout(position, rotation, fov, aspect, ground,
                    seed);
                foreach (float side in new float[] { -1f, 1f })
                {
                    List<ForegroundItem> corner = items.Where(i => Mathf.Sign(i.position.x) == side).ToList();
                    Assert.That(corner.Count(i => i.kind == ForegroundKind.Boulder), Is.InRange(2, 3),
                        "seed " + seed);
                    Assert.That(corner.Count(i => i.kind == ForegroundKind.Rosette), Is.InRange(1, 2),
                        "seed " + seed);
                }
                foreach (ForegroundItem item in items)
                {
                    string label = $"seed {seed} {item.kind} at {item.position}";
                    if (item.kind == ForegroundKind.Boulder)
                    {
                        Assert.That(item.scale, Is.InRange(1.4f, 2.45f), label);
                    }
                    else
                    {
                        Assert.That(item.scale, Is.InRange(2.1f, 3.5f), label);
                    }
                    Assert.AreEqual(ground, item.position.y, label);

                    Vector2 viewport = EnvironmentForeground.ToViewport(item.position, position, rotation, fov,
                        aspect);
                    Assert.That(viewport.y, Is.LessThan(0.2f), label);
                    Assert.IsTrue(viewport.x < 0.25f || viewport.x > 0.75f, label);

                    // Partly outside: the outer-lower rim of the footprint leaves the frame
                    Vector3 outward = new Vector3(Mathf.Sign(item.position.x) * item.radius, 0f, -item.radius);
                    Vector3 rim = item.position + outward * 0.7071f;
                    Vector2 rimViewport = EnvironmentForeground.ToViewport(rim, position, rotation, fov, aspect);
                    Assert.IsTrue(rimViewport.x < 0f || rimViewport.x > 1f || rimViewport.y < 0f, label);

                    // For the stage pose nothing reaches over the board
                    Assert.That(item.position.z + item.radius, Is.LessThan(grid.yMin), label);
                }
            }
        }

        [Test]
        public void InvalidInputLogsAndLaysOutNothing()
        {
            LogAssert.Expect(LogType.Error, new Regex(@"^\[HLEnvironmentForeground\] Rejected field of view"));
            LogAssert.Expect(LogType.Error, new Regex(@"^\[HLEnvironmentForeground\] Rejected field of view"));
            LogAssert.Expect(LogType.Error, "[HLEnvironmentForeground] The bottom corners of the frame do not see the ground.");

            Assert.IsEmpty(EnvironmentForeground.Layout(position, rotation, 0f, aspect, ground, 1));
            Assert.IsEmpty(EnvironmentForeground.Layout(position, rotation, fov, 0f, ground, 1));
            Assert.IsEmpty(EnvironmentForeground.Layout(position, Quaternion.Euler(-30f, 0f, 0f), fov, aspect, ground, 1));
        }

        [Test]
        public void GroundHitLookingUpMissesTheGround()
        {
            bool isHit = EnvironmentForeground.GroundHit(position, Quaternion.Euler(-30f, 0f, 0f), fov, aspect,
                new Vector2(0.5f, 0.5f), ground, out Vector3 hit);

            Assert.IsFalse(isHit);
        }

        [Test]
        public void BuildMakesOneColliderFreeChildPerItemColouredThroughThePropertyBlock()
        {
            EnvironmentForeground foreground = _go.AddComponent<EnvironmentForeground>();
            TestHelpers.SetPrivateField(foreground, "_groundY", ground);
            TestHelpers.SetPrivateField(foreground, "_seed", 5);
            TestHelpers.SetPrivateField(foreground, "_meshes", LoadMeshes());

            Assert.DoesNotThrow(() => foreground.Build());
            Assert.IsNull(foreground.root);

            foreground.Build(position, rotation, fov, aspect);

            Assert.That(foreground.items.Count, Is.InRange(6, 10));
            Assert.AreEqual(foreground.items.Count, foreground.root.childCount);
            for (int i = 0; i < foreground.items.Count; i++)
            {
                Transform pivot = foreground.root.GetChild(i);
                Assert.That(Vector3.Distance(foreground.items[i].position, pivot.position), Is.LessThan(0.001f));
                int minimumParts = foreground.items[i].kind == ForegroundKind.Boulder ? 1 : 7;
                Assert.That(pivot.childCount, Is.GreaterThanOrEqualTo(minimumParts));
            }
            Assert.AreEqual(0, _go.GetComponentsInChildren<Collider>().Length);

            MeshRenderer[] renderers = foreground.root.GetComponentsInChildren<MeshRenderer>();
            Assert.That(renderers.Length, Is.GreaterThan(foreground.items.Count));
            MaterialPropertyBlock block = new MaterialPropertyBlock();
            foreach (MeshRenderer meshRenderer in renderers)
            {
                Assert.IsTrue(meshRenderer.HasPropertyBlock(), meshRenderer.name);
                meshRenderer.GetPropertyBlock(block);
                Assert.That(block.GetColor("_BaseColor").a, Is.GreaterThan(0f));
            }
        }

        [Test]
        public void InitFromACameraMatchesTheLayout()
        {
            GameObject cameraGo = new GameObject("HLForegroundCamera");
            GameObject managerGo = new GameObject("HLForegroundManager");
            try
            {
                Camera camera = cameraGo.AddComponent<Camera>();
                camera.fieldOfView = fov;
                camera.aspect = aspect;
                cameraGo.transform.SetPositionAndRotation(position, rotation);
                RenderManager manager = managerGo.AddComponent<RenderManager>();
                TestHelpers.SetPrivateField(manager, "_meshes", LoadMeshes());
                EnvironmentForeground foreground = _go.AddComponent<EnvironmentForeground>();
                TestHelpers.SetPrivateField(foreground, "_seed", 11);

                foreground.Init(camera, ground, manager);

                List<ForegroundItem> expected = EnvironmentForeground.Layout(position, rotation, fov, aspect,
                    ground, 11);
                Assert.AreEqual(expected.Count, foreground.items.Count);
                // The camera transform round-trips the pose through its own storage, so compare within float tolerance
                for (int i = 0; i < expected.Count; i++)
                {
                    Assert.That(Vector3.Distance(expected[i].position, foreground.items[i].position),
                        Is.LessThan(0.001f), i.ToString());
                }
                Assert.AreEqual(1, _go.transform.childCount);
            }
            finally
            {
                Object.DestroyImmediate(cameraGo);
                Object.DestroyImmediate(managerGo);
            }
        }
    }
}
