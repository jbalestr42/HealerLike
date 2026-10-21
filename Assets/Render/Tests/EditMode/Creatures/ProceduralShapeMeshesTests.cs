using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Creatures
{

public class ProceduralShapeMeshesTests
{
    readonly List<Mesh> _meshes = new List<Mesh>();

    static IEnumerable<ShapeProfile> Profiles()
    {
        yield return ShapeProfile.Bulb();
        yield return ShapeProfile.Segment();
        yield return ShapeProfile.Leaf();
        yield return ShapeProfile.Leaf(0.12f, 0.7f, -0.9f);
        yield return ShapeProfile.Block();
        yield return ShapeProfile.Shard();
        yield return ShapeProfile.Ring();
        yield return ShapeProfile.Ring(0.3f, true);
    }

    [Test]
    public void Create_BowedLeaf_HasAConcaveInnerEdgeBetweenItsAttachedPoles()
    {
        ShapeProfile shape = ShapeProfile.Leaf(0.12f, 0.7f, -0.9f);
        Mesh leaf = Build(shape);
        Vector3 bottom = ProceduralShapeMeshes.Anchor(shape, ShapeAnchor.Bottom);
        Vector3 top = ProceduralShapeMeshes.Anchor(shape, ShapeAnchor.Top);
        float innerEdge = float.MinValue;
        foreach (Vector3 vertex in leaf.vertices)
        {
            if (Mathf.Abs(vertex.y) < 0.001f)
            {
                innerEdge = Mathf.Max(innerEdge, vertex.x);
            }
        }

        Assert.Greater(innerEdge, float.MinValue, "The profile must contain a middle cross-section.");
        Assert.Less(
            innerEdge,
            Mathf.Min(bottom.x, top.x) - 0.12f,
            "The leaf must bow around an opening, rather than remain a straight lance."
        );
        CheckSolid(leaf);
    }

    Mesh Build(ShapeProfile shape, int variant = 0)
    {
        Mesh mesh = ProceduralShapeMeshes.Create(shape, variant);
        Assert.IsNotNull(mesh);
        _meshes.Add(mesh);
        return mesh;
    }

    [TearDown]
    public void TearDown()
    {
        foreach (Mesh mesh in _meshes)
        {
            Object.DestroyImmediate(mesh);
        }

        _meshes.Clear();
    }

    [Test]
    public void Create_EveryProfile_IsClosedFiniteAndFitsTheAuthoredUnitBox()
    {
        foreach (ShapeProfile shape in Profiles())
        {
            CheckSolid(Build(shape));
        }
    }

    [Test]
    public void Create_DefaultBulb_HasARoundSurfaceBeforeTheRecipeAppliesItsProportions()
    {
        Mesh bulb = Build(ShapeProfile.Bulb());
        foreach (Vector3 vertex in bulb.vertices)
        {
            Assert.That(
                vertex.sqrMagnitude,
                Is.EqualTo(0.25f).Within(0.00001f),
                "A default bulb must follow a sphere, including the shoulders near its poles."
            );
        }

        CheckSolid(bulb);
    }

    [Test]
    public void Create_BoundedParameterExtremes_RetainClosedNondegenerateSolids()
    {
        foreach (ShapeProfile source in Profiles())
        {
            for (int end = 0; end < 2; end++)
            {
                ShapeProfile shape = source;
                shape.radialSegments = end == 0 ? 6 : 32;
                shape.lengthSegments = end == 0 ? 4 : 24;
                shape.fullness = end == 0 ? 0.05f : 3f;
                shape.taper = end == 0 ? -0.8f : 0.95f;
                shape.bend = end == 0 ? -1f : 1f;
                shape.bow = end == 0 ? -1.5f : 1.5f;
                shape.bevel = end == 0 ? 0.02f : 0.4f;
                shape.asymmetry = 0.15f;
                shape.tubeRatio = end == 0 ? 0.06f : 0.45f;
                CheckSolid(Build(shape, end));
            }
        }
    }

    static void CheckSolid(Mesh mesh)
    {
        RenderTestAssets.AssertClosed(mesh);
        Assert.That(Vector3.Distance(Vector3.one, mesh.bounds.size), Is.LessThan(0.00001f), mesh.name);
        Assert.That(mesh.bounds.center.sqrMagnitude, Is.LessThan(0.00000001f), mesh.name);
        Vector3[] vertices = mesh.vertices;
        Vector3[] normals = mesh.normals;
        int[] triangles = mesh.triangles;
        for (int i = 0; i < vertices.Length; i++)
        {
            Assert.IsTrue(RenderMath.IsFinite(vertices[i]), mesh.name);
            Assert.IsTrue(RenderMath.IsFinite(normals[i]), mesh.name);
            Assert.That(normals[i].magnitude, Is.EqualTo(1f).Within(0.0001f), mesh.name);
        }

        for (int i = 0; i < triangles.Length; i += 3)
        {
            Vector3 a = vertices[triangles[i]];
            Vector3 cross = Vector3.Cross(vertices[triangles[i + 1]] - a, vertices[triangles[i + 2]] - a);
            Assert.Greater(cross.sqrMagnitude, 0f, mesh.name + " contains a degenerate face");
        }
    }

    [Test]
    public void Create_InvalidProfiles_RejectNonfiniteValuesAndUnboundedTessellation()
    {
        foreach (float bad in new[] { float.NaN, float.PositiveInfinity, float.NegativeInfinity })
        {
            ShapeProfile shape = ShapeProfile.Leaf();
            shape.bend = bad;
            Assert.IsFalse(shape.IsValid());
            Assert.IsNull(ProceduralShapeMeshes.Create(shape));
            shape = ShapeProfile.Block();
            shape.bevel = bad;
            Assert.IsFalse(shape.IsValid());
            shape = ShapeProfile.Leaf();
            shape.bow = bad;
            Assert.IsFalse(shape.IsValid());
            shape = ShapeProfile.Block();
            shape.ridge = bad;
            Assert.IsFalse(shape.IsValid());
        }

        ShapeProfile invalid = ShapeProfile.Ring();
        invalid.tubeRatio = 0.6f;
        Assert.IsNull(ProceduralShapeMeshes.Create(invalid));
        invalid = ShapeProfile.Bulb();
        invalid.radialSegments = int.MaxValue;
        Assert.IsNull(ProceduralShapeMeshes.Create(invalid));
        invalid = ShapeProfile.Bulb();
        invalid.lengthSegments = 0;
        Assert.IsNull(ProceduralShapeMeshes.Create(invalid));
        invalid = ShapeProfile.Bulb();
        invalid.kind = (ShapeKind)99;
        Assert.IsNull(ProceduralShapeMeshes.Create(invalid));
        Assert.IsTrue(default(ShapeProfile).IsValid());
        Assert.IsNull(ProceduralShapeMeshes.Create(default));
    }

    [Test]
    public void Create_BendTaperFullnessAndBevel_ChangeVerticesInsideTheSameBounds()
    {
        AssertDifferent(ShapeProfile.Leaf(0f), ShapeProfile.Leaf(0.8f));
        AssertDifferent(ShapeProfile.Leaf(), ShapeProfile.Leaf(bow: -0.9f));
        AssertDifferent(ShapeProfile.Segment(0f), ShapeProfile.Segment(0.8f));
        AssertDifferent(ShapeProfile.Bulb(0.5f), ShapeProfile.Bulb(2f));
        AssertDifferent(ShapeProfile.Block(0.08f), ShapeProfile.Block(0.35f));
        AssertDifferent(ShapeProfile.Ring(0.12f), ShapeProfile.Ring(0.4f));
    }

    void AssertDifferent(ShapeProfile a, ShapeProfile b)
    {
        Mesh first = Build(a);
        Mesh second = Build(b);
        Assert.That(Vector3.Distance(first.bounds.size, second.bounds.size), Is.LessThan(0.00001f));
        CollectionAssert.AreNotEqual(first.vertices, second.vertices);
    }

    [Test]
    public void Create_Minerals_HaveFlatFacesSharedOutlineNormalsAndTwoPaletteSubmeshes()
    {
        foreach (
            ShapeProfile shape in new[]
            {
                ShapeProfile.Block(),
                ShapeProfile.Shard(),
                ShapeProfile.Ring(0.2f, true),
            }
        )
        {
            Mesh mesh = Build(shape);
            Assert.AreEqual(2, mesh.subMeshCount);
            Assert.Greater(mesh.GetIndexCount(0), 0);
            Assert.Greater(mesh.GetIndexCount(1), 0);
            List<Vector3> outline = new List<Vector3>();
            mesh.GetUVs(3, outline);
            Assert.AreEqual(mesh.vertexCount, outline.Count);
            Vector3[] vertices = mesh.vertices;
            Vector3[] normals = mesh.normals;
            Dictionary<Vector3, Vector3> welded = new Dictionary<Vector3, Vector3>();
            for (int i = 0; i < vertices.Length; i++)
            {
                Assert.That(outline[i].magnitude, Is.EqualTo(1f).Within(0.0001f));
                if (welded.TryGetValue(vertices[i], out Vector3 previous))
                {
                    Assert.AreEqual(previous, outline[i]);
                }

                welded[vertices[i]] = outline[i];
                Assert.AreEqual(normals[(i / 3) * 3], normals[i]);
            }
        }
    }

    [Test]
    public void Create_SeededBlock_IsDeterministicAndDifferentSeedsChangeTheProfile()
    {
        Mesh a = Build(ShapeProfile.Block(), 17);
        Mesh b = Build(ShapeProfile.Block(), 17);
        Mesh c = Build(ShapeProfile.Block(), 23);
        CollectionAssert.AreEqual(a.vertices, b.vertices);
        CollectionAssert.AreEqual(a.triangles, b.triangles);
        CollectionAssert.AreEqual(a.normals, b.normals);
        CollectionAssert.AreNotEqual(a.vertices, c.vertices);
    }
}
}
