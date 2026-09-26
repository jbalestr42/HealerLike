using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using HealerLike.Render.Grammar;

namespace HealerLike.Render.Creatures
{

public class MineralHullTests : MineralMeshFixture
{
    [TestCase(0.02f, -0.8f, 0.001f)]
    [TestCase(0.18f, 0.08f, 0.8f)]
    [TestCase(0.4f, 0.95f, 1f)]
    [TestCase(0.4f, -0.8f, 1f)]
    public void Create_SeededParameterExtremes_AreClosedConvexFiniteSolids(float bevel, float taper, float fracture)
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
            foreach (Vector3 point in points)
            {
                Assert.IsTrue(RenderMath.IsFinite(point));
            }

            for (int i = 0; i < triangles.Length; i += 3)
            {
                Vector3 a = points[triangles[i]];
                Vector3 cross = Vector3.Cross(points[triangles[i + 1]] - a, points[triangles[i + 2]] - a);
                Assert.Greater(cross.sqrMagnitude, 0f);
                Vector3 normal = normals[triangles[i]];
                Assert.That(normal.magnitude, Is.EqualTo(1f).Within(0.0001f));
                Assert.Greater(Vector3.Dot(cross, normal), 0f, "Every triangle must wind out of its cut plane.");
                foreach (Vector3 point in points)
                {
                    Assert.LessOrEqual(
                        Vector3.Dot(normal, point - a),
                        0.00005f,
                        "A fractured mineral must keep broad convex faces, not folded noise."
                    );
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
                Vector3 normal = Vector3
                    .Cross(points[triangles[i + 1]] - a, points[triangles[i + 2]] - a)
                    .normalized;
                float distance = Vector3.Dot(normal, a);
                foreach ((Vector3 normal, float distance, int submesh) plane in planes)
                {
                    if (
                        Vector3.Dot(plane.normal, normal) > 0.99999f
                        && Mathf.Abs(plane.distance - distance) < 0.00001f
                    )
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
            if (shared.TryGetValue(points[i], out Vector3 prior))
            {
                Assert.AreEqual(prior, outline[i]);
            }

            Assert.That(outline[i].magnitude, Is.EqualTo(1f).Within(0.0001f));
            shared[points[i]] = outline[i];
        }
    }

    [TestCase(0.02f, -0.8f, 0.001f)]
    [TestCase(0.2f, 0.1f, 0.8f)]
    [TestCase(0.4f, 0.95f, 1f)]
    public void Create_RidgedExtremes_KeepLimitedWholePlanesAndSurfaceAnchors(
        float bevel,
        float taper,
        float fracture
    )
    {
        for (int variant = 0; variant < 8; variant++)
        {
            ShapeProfile shape = ShapeProfile.Block(bevel, taper, 0.15f, fracture, 1f);
            Mesh mesh = Build(shape, variant);
            RenderTestAssets.AssertClosed(mesh);
            Assert.That(Vector3.Distance(mesh.bounds.size, Vector3.one), Is.LessThan(0.00001f));
            Assert.LessOrEqual(
                new HashSet<Vector3>(mesh.normals).Count,
                22,
                "A ridge adds four intentional planes; tessellation must not add shading planes."
            );
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
        float left = 0f,
            right = 0f;
        for (int i = 0; i < vertices.Length; i += 3)
        {
            Vector3 centre = (vertices[i] + vertices[i + 1] + vertices[i + 2]) / 3f;
            if (centre.z < 0.15f || Mathf.Abs(centre.x) > 0.28f || Mathf.Abs(centre.y) > 0.4f)
            {
                continue;
            }

            Vector3 normal = normals[i];
            if (normal.z < 0.5f)
            {
                continue;
            }

            float area =
                Vector3.Cross(vertices[i + 1] - vertices[i], vertices[i + 2] - vertices[i]).magnitude * 0.5f;
            if (normal.x < -0.2f)
            {
                left += area;
            }

            if (normal.x > 0.2f)
            {
                right += area;
            }
        }

        Assert.Greater(left, 0.04f);
        Assert.Greater(right, 0.04f);
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
}
}
