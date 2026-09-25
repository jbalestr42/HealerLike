using System.Collections.Generic;
using HealerLike.Render.Grammar;
using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Creatures
{
    public class MineralHullTests
    {
        readonly List<Mesh> _meshes = new List<Mesh>();

        Mesh Build(ShapeProfile shape, int variant)
        {
            Mesh mesh = ProceduralShapeMeshes.Create(shape, variant);
            Assert.IsNotNull(mesh);
            _meshes.Add(mesh);
            return mesh;
        }

        [TearDown]
        public void TearDown()
        {
            foreach (Mesh mesh in _meshes) Object.DestroyImmediate(mesh);
            _meshes.Clear();
        }

        [TestCase(0.02f, -0.8f, 0.001f)]
        [TestCase(0.18f, 0.08f, 0.8f)]
        [TestCase(0.4f, 0.95f, 1f)]
        [TestCase(0.4f, -0.8f, 1f)]
        public void Create_SeededParameterExtremes_AreClosedConvexFiniteSolids(float bevel, float taper,
            float fracture)
        {
            ShapeProfile shape = ShapeProfile.Block(bevel, taper, 0.15f, fracture);
            for (int variant = 0; variant < 16; variant++)
            {
                shape.bend = variant % 2 == 0 ? 1f : -1f;
                Mesh mesh = Build(shape, variant);
                RenderTestAssets.AssertClosed(mesh);
                Assert.That(Vector3.Distance(Vector3.one, mesh.bounds.size), Is.LessThan(0.00001f));
                Assert.That(mesh.bounds.center.sqrMagnitude, Is.LessThan(0.00000001f));
                Assert.LessOrEqual(mesh.vertexCount, 252, "Clipping must stay bounded by its fixed plane count.");
                Vector3[] points = mesh.vertices;
                Vector3[] normals = mesh.normals;
                int[] triangles = mesh.triangles;
                foreach (Vector3 point in points) Assert.IsTrue(RenderMath.IsFinite(point));
                for (int i = 0; i < triangles.Length; i += 3)
                {
                    Vector3 a = points[triangles[i]];
                    Vector3 cross = Vector3.Cross(points[triangles[i + 1]] - a,
                        points[triangles[i + 2]] - a);
                    Assert.Greater(cross.sqrMagnitude, 0f);
                    Vector3 normal = normals[triangles[i]];
                    Assert.That(normal.magnitude, Is.EqualTo(1f).Within(0.0001f));
                    Assert.Greater(Vector3.Dot(cross, normal), 0f, "Every triangle must wind out of its cut plane.");
                    foreach (Vector3 point in points)
                    {
                        Assert.LessOrEqual(Vector3.Dot(normal, point - a), 0.00005f,
                            "A fractured mineral must keep broad convex faces, not folded noise.");
                    }
                }
                AssertOnSurface(mesh, ProceduralShapeMeshes.Anchor(shape, ShapeAnchor.Top, variant));
                AssertOnSurface(mesh, ProceduralShapeMeshes.Anchor(shape, ShapeAnchor.Bottom, variant));
            }
        }

        [TestCase(10)]
        [TestCase(15)]
        public void Create_SkinnyExtremeFanTriangles_ShareTheirWholeCutFaceNormal(int variant)
        {
            ShapeProfile shape = ShapeProfile.Block(0.4f, 0.95f, 0.15f, 1f);
            shape.bend = variant % 2 == 0 ? 1f : -1f;
            Mesh mesh = Build(shape, variant);
            // Six primary planes plus twelve edge cuts; triangle diagonals do not introduce shading planes.
            Assert.LessOrEqual(new HashSet<Vector3>(mesh.normals).Count, 18);
            Vector3[] points = mesh.vertices;
            Vector3[] normals = mesh.normals;
            for (int i = 0; i < points.Length; i += 3)
            {
                Assert.AreEqual(normals[i], normals[i + 1]);
                Assert.AreEqual(normals[i], normals[i + 2]);
                Assert.That(Mathf.Abs(Vector3.Dot(normals[i], points[i + 1] - points[i])), Is.LessThan(0.000001f));
                Assert.That(Mathf.Abs(Vector3.Dot(normals[i], points[i + 2] - points[i])), Is.LessThan(0.000001f));
            }
        }

        [Test]
        public void Create_FractureAndVariants_ChangeWholePlanesDeterministically()
        {
            ShapeProfile profile = ShapeProfile.Block(fracture: 0.8f);
            Mesh a = Build(profile, 17);
            Mesh repeated = Build(profile, 17);
            Mesh different = Build(profile, 29);
            Mesh legacy = Build(ShapeProfile.Block(), 17);
            CollectionAssert.AreEqual(a.vertices, repeated.vertices);
            CollectionAssert.AreEqual(a.triangles, repeated.triangles);
            CollectionAssert.AreNotEqual(a.vertices, different.vertices);
            CollectionAssert.AreNotEqual(a.vertices, legacy.vertices);
            Assert.Less(a.vertexCount, 253);
        }

        [Test]
        public void Create_PlanarFacets_DoNotSplitTheirPaletteAcrossTriangleDiagonals()
        {
            Mesh mesh = Build(ShapeProfile.Block(fracture: 0.85f), 42);
            Assert.AreEqual(2, mesh.subMeshCount);
            Assert.Greater(mesh.GetIndexCount(0), 0u);
            Assert.Greater(mesh.GetIndexCount(1), 0u);
            Vector3[] points = mesh.vertices;
            List<(Vector3 normal, float distance, int submesh)> planes = new List<(Vector3, float, int)>();
            for (int submesh = 0; submesh < mesh.subMeshCount; submesh++)
            {
                int[] triangles = mesh.GetTriangles(submesh);
                for (int i = 0; i < triangles.Length; i += 3)
                {
                    Vector3 a = points[triangles[i]];
                    Vector3 normal = Vector3.Cross(points[triangles[i + 1]] - a,
                        points[triangles[i + 2]] - a).normalized;
                    float distance = Vector3.Dot(normal, a);
                    foreach (var plane in planes)
                    {
                        if (Vector3.Dot(plane.normal, normal) > 0.99999f
                            && Mathf.Abs(plane.distance - distance) < 0.00001f)
                        {
                            Assert.AreEqual(plane.submesh, submesh, "One cut face must keep one material.");
                        }
                    }
                    planes.Add((normal, distance, submesh));
                }
            }
            List<Vector3> outline = new List<Vector3>();
            mesh.GetUVs(3, outline);
            Assert.AreEqual(mesh.vertexCount, outline.Count);
            Dictionary<Vector3, Vector3> shared = new Dictionary<Vector3, Vector3>();
            for (int i = 0; i < points.Length; i++)
            {
                if (shared.TryGetValue(points[i], out Vector3 prior)) Assert.AreEqual(prior, outline[i]);
                Assert.That(outline[i].magnitude, Is.EqualTo(1f).Within(0.0001f));
                shared[points[i]] = outline[i];
            }
        }

        [Test]
        public void Anchor_SlantedCrown_SitsOnItsFaceInteriorRatherThanAnIsolatedHighestCorner()
        {
            ShapeProfile shape = ShapeProfile.Block(fracture: 0.85f);
            Mesh mesh = Build(shape, 17);
            Vector3 anchor = ProceduralShapeMeshes.Anchor(shape, ShapeAnchor.Top, 17);
            AssertOnSurface(mesh, anchor);
            Assert.Less(anchor.y, mesh.bounds.max.y - 0.005f);
            foreach (Vector3 point in mesh.vertices)
            {
                Assert.Greater(Vector3.Distance(anchor, point), 0.01f);
            }
        }

        [TestCase(7)]
        [TestCase(15)]
        [TestCase(31)]
        public void Anchor_FullyClippedBlockCrown_UsesItsHighestSurfacePoint(int variant)
        {
            // Retain the profile which exposed the removed-cap bug independently of future art tuning.
            ShapeProfile shape = ShapeProfile.Block(0.28f, 0.12f, 0.08f, 0.82f);
            Mesh mesh = Build(shape, variant);
            Vector3 anchor = ProceduralShapeMeshes.Anchor(shape, ShapeAnchor.Top, variant);
            Assert.That(anchor.y, Is.EqualTo(mesh.bounds.max.y).Within(0.000001f),
                "A removed crown must not move the accent onto a broad sloping side.");
            AssertOnSurface(mesh, anchor);
        }

        [TestCase(0.02f, -0.8f, 0.001f)]
        [TestCase(0.2f, 0.1f, 0.8f)]
        [TestCase(0.4f, 0.95f, 1f)]
        public void Create_RidgedExtremes_KeepLimitedWholePlanesAndSurfaceAnchors(float bevel, float taper,
            float fracture)
        {
            for (int variant = 0; variant < 8; variant++)
            {
                ShapeProfile shape = ShapeProfile.Block(bevel, taper, 0.15f, fracture, 1f);
                Mesh mesh = Build(shape, variant);
                RenderTestAssets.AssertClosed(mesh);
                Assert.That(Vector3.Distance(mesh.bounds.size, Vector3.one), Is.LessThan(0.00001f));
                Assert.LessOrEqual(new HashSet<Vector3>(mesh.normals).Count, 22,
                    "A ridge adds four intentional planes; tessellation must not add shading planes.");
                foreach (Vector3 normal in mesh.normals)
                {
                    Assert.IsTrue(RenderMath.IsFinite(normal));
                    Assert.That(normal.magnitude, Is.EqualTo(1f).Within(0.0001f));
                }
                AssertOnSurface(mesh, ProceduralShapeMeshes.Anchor(shape, ShapeAnchor.Top, variant));
                AssertOnSurface(mesh, ProceduralShapeMeshes.Anchor(shape, ShapeAnchor.Bottom, variant));
            }
        }

        [Test]
        public void Create_Ridge_ReplacesTheBlankFrontWithTwoBroadMeetingPlanes()
        {
            Mesh mesh = Build(ShapeProfile.Block(0.16f, 0f, 0f, 0f, 0.7f), 17);
            Vector3[] vertices = mesh.vertices;
            Vector3[] normals = mesh.normals;
            float left = 0f, right = 0f;
            for (int i = 0; i < vertices.Length; i += 3)
            {
                Vector3 centre = (vertices[i] + vertices[i + 1] + vertices[i + 2]) / 3f;
                if (centre.z < 0.15f || Mathf.Abs(centre.x) > 0.28f || Mathf.Abs(centre.y) > 0.4f) continue;
                Vector3 normal = normals[i];
                if (normal.z < 0.5f) continue;
                float area = Vector3.Cross(vertices[i + 1] - vertices[i], vertices[i + 2] - vertices[i]).magnitude * 0.5f;
                if (normal.x < -0.2f) left += area;
                if (normal.x > 0.2f) right += area;
            }
            Assert.Greater(left, 0.04f);
            Assert.Greater(right, 0.04f);
        }

        [TestCase(MassBand.Light)]
        [TestCase(MassBand.Sturdy)]
        [TestCase(MassBand.Heavy)]
        public void Compose_AuthoredFiveSpears_KeepEveryAccentAtItsBladeApex(MassBand mass)
        {
            LookVocabulary vocabulary = Object.Instantiate(RenderTestAssets.LoadLookVocabulary());
            CreatureRecipe recipe = null;
            try
            {
                GrowthStoneVocabulary.Apply(vocabulary);
                LookPart source = System.Array.Find(vocabulary.heads[HeadKind.Spear].stone,
                    part => part.id == "SpearBlade");
                LookPart sourceTip = System.Array.Find(vocabulary.heads[HeadKind.Spear].stone,
                    part => part.attachTo == "SpearBlade");
                recipe = LookComposer.Compose(RenderTestAssets.CreateChannels(LookSide.Stone, HeadKind.Spear,
                    CountBand.Many, mass: mass), vocabulary);
                Assert.IsNotNull(recipe);
                int blades = 0;
                for (int i = 0; i < recipe.parts.Length; i++)
                {
                    CreaturePart blade = recipe.parts[i];
                    if (!blade.id.StartsWith("SpearBlade", System.StringComparison.Ordinal)) continue;
                    blades++;
                    Mesh mesh = Build(blade.shape, blade.variant);
                    Vector3 apex = ProceduralShapeMeshes.Anchor(blade.shape, ShapeAnchor.Top, blade.variant);
                    Assert.That(apex.y, Is.EqualTo(mesh.bounds.max.y).Within(0.000001f));
                    AssertOnSurface(mesh, apex);
                    CreaturePart tip = recipe.parts[i + 1];
                    Assert.IsTrue(tip.id.StartsWith(sourceTip.id, System.StringComparison.Ordinal));
                    Vector3 bottom = ProceduralShapeMeshes.Anchor(tip.shape, ShapeAnchor.Bottom, tip.variant);
                    Vector3 bladeContact = blade.localPosition + Quaternion.Euler(blade.localEuler)
                        * Vector3.Scale(blade.dimensions, apex);
                    Vector3 tipContact = tip.localPosition + Quaternion.Euler(tip.localEuler)
                        * Vector3.Scale(tip.dimensions, bottom);
                    float scale = blade.dimensions.y / source.size.y;
                    Assert.That(Vector3.Distance(bladeContact, tipContact),
                        Is.EqualTo(sourceTip.position.magnitude * scale).Within(0.00001f),
                        "The authored small overlap must remain at the blade apex, not halfway down a side.");
                }
                Assert.AreEqual(5, blades);
            }
            finally
            {
                Object.DestroyImmediate(recipe);
                Object.DestroyImmediate(vocabulary);
            }
        }

        [TestCase(0.25f)]
        [TestCase(1f)]
        public void Resolve_FractureEdits_KeepActualSlabAndAccentSurfacesAttached(float fracture)
        {
            LookPart[] fragment =
            {
                new LookPart { id = "Slab", shape = ShapeProfile.Block(fracture: fracture),
                    primitive = Primitive.Stone, pivot = ShapeAnchor.Bottom,
                    size = new Vector3(0.7f, 1.4f, 0.4f), euler = new Vector3(0f, 23f, -8f) },
                new LookPart { id = "Accent", shape = ShapeProfile.Shard(fracture: fracture),
                    primitive = Primitive.Stone, pivot = ShapeAnchor.Bottom, attachTo = "Slab",
                    attachAt = ShapeAnchor.Top, size = new Vector3(0.15f, 0.25f, 0.14f) }
            };
            LookVocabulary vocabulary = Object.Instantiate(RenderTestAssets.LoadLookVocabulary());
            CreatureRecipe recipe = null;
            try
            {
                vocabulary.heads[HeadKind.Fork] = new LookVocabulary.HeadEntry { stone = fragment };
                recipe = LookComposer.Compose(RenderTestAssets.CreateChannels(LookSide.Stone, HeadKind.Fork), vocabulary);
                Assert.IsNotNull(recipe);
                CreaturePart slab = System.Array.Find(recipe.parts, part => part.id == "Slab");
                CreaturePart accent = System.Array.Find(recipe.parts, part => part.id == "Accent");
                Vector3[] contacts = new Vector3[2];
                CreaturePart[] parts = { slab, accent };
                for (int i = 0; i < parts.Length; i++)
                {
                    CreaturePart part = parts[i];
                    ShapeAnchor selected = i == 0 ? ShapeAnchor.Top : ShapeAnchor.Bottom;
                    Vector3 local = ProceduralShapeMeshes.Anchor(part.shape, selected, part.variant);
                    AssertOnSurface(Build(part.shape, part.variant), local);
                    contacts[i] = part.localPosition + Quaternion.Euler(part.localEuler)
                        * Vector3.Scale(part.dimensions, local);
                }
                Assert.That(Vector3.Distance(contacts[0], contacts[1]), Is.LessThan(0.00001f));
            }
            finally
            {
                Object.DestroyImmediate(recipe);
                Object.DestroyImmediate(vocabulary);
            }
        }

        [Test]
        public void Fracture_InvalidValuesRejectAndCacheKeysIncludeTheNewParameter()
        {
            ShapeProfile profile = ShapeProfile.Block();
            foreach (float value in new[] { -0.01f, 1.01f, float.NaN, float.PositiveInfinity })
            {
                profile.fracture = value;
                Assert.IsFalse(profile.IsValid());
                Assert.IsNull(ProceduralShapeMeshes.Create(profile));
            }
            using (ShapeMeshCache cache = new ShapeMeshCache())
            {
                ShapeProfile a = ShapeProfile.Block(fracture: 0.4f);
                ShapeProfile b = ShapeProfile.Block(fracture: 0.8f);
                Assert.AreNotEqual(a, b);
                Assert.AreNotSame(cache.Get(a, 17), cache.Get(b, 17));
                Assert.AreSame(cache.Get(a, 17), cache.Get(a, 17));
            }
        }

        static void AssertOnSurface(Mesh mesh, Vector3 point)
        {
            Vector3[] vertices = mesh.vertices;
            int[] triangles = mesh.triangles;
            for (int i = 0; i < triangles.Length; i += 3)
            {
                Vector3 a = vertices[triangles[i]], b = vertices[triangles[i + 1]], c = vertices[triangles[i + 2]];
                Vector3 normal = Vector3.Cross(b - a, c - a).normalized;
                if (Mathf.Abs(Vector3.Dot(normal, point - a)) > 0.00001f) continue;
                if (Vector3.Dot(Vector3.Cross(b - a, point - a), normal) < -0.00001f) continue;
                if (Vector3.Dot(Vector3.Cross(c - b, point - b), normal) < -0.00001f) continue;
                if (Vector3.Dot(Vector3.Cross(a - c, point - c), normal) < -0.00001f) continue;
                return;
            }
            Assert.Fail("The attachment point is not on the actual generated surface.");
        }
    }
}
