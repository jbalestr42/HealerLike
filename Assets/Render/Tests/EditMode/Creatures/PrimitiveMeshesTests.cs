using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using HealerLike.Render.Stones;
using Object = UnityEngine.Object;

namespace HealerLike.Render.Creatures
{

public class PrimitiveMeshesTests
{
    readonly List<Object> _objects = new List<Object>();

    [TearDown]
    public void TearDown()
    {
        foreach (Object trackedObject in _objects)
        {
            Object.DestroyImmediate(trackedObject);
        }
        _objects.Clear();
    }

    T Track<T>(T trackedObject) where T : Object
    {
        _objects.Add(trackedObject);
        return trackedObject;
    }

    [TestCase(Primitive.Sphere)]
    [TestCase(Primitive.Capsule)]
    [TestCase(Primitive.Cone)]
    [TestCase(Primitive.Torus)]
    [TestCase(Primitive.CylinderSegment)]
    [TestCase(Primitive.Leaf)]
    public void GetMesh_BakedPrimitive_IsFiniteNormalizedAndBounded(Primitive type)
    {
        Mesh mesh = RenderTestAssets.LoadMeshes().GetMesh(type);

        Vector3[] vertices = mesh.vertices;
        Vector3[] normals = mesh.normals;
        int[] indices = mesh.triangles;
        Assert.Greater(indices.Length, 0);
        Assert.AreEqual(vertices.Length, normals.Length);
        foreach (int index in indices)
        {
            Assert.That(index, Is.InRange(0, vertices.Length - 1));
        }

        foreach (Vector3 vertex in vertices)
        {
            Assert.IsTrue(float.IsFinite(vertex.x) && float.IsFinite(vertex.y) && float.IsFinite(vertex.z));
            Assert.LessOrEqual(Mathf.Abs(vertex.x), 0.50001f);
            Assert.LessOrEqual(Mathf.Abs(vertex.y), 0.50001f);
            Assert.LessOrEqual(Mathf.Abs(vertex.z), 0.50001f);
        }

        foreach (Vector3 normal in normals)
        {
            Assert.That(normal.magnitude, Is.EqualTo(1).Within(0.00001));
        }

        Assert.That(mesh.bounds.size.x, Is.EqualTo(1).Within(0.00001));
        float height = type == Primitive.Torus ? Mathf.Sqrt(3f) / 12f : 1f; // tube ratio 0.2, six rings
        Assert.That(mesh.bounds.size.y, Is.EqualTo(height).Within(0.00001));
    }

    [TestCase(0, 0)]
    [TestCase(1, 1)]
    [TestCase(5, 1)] // wraps round the two variants
    public void GetMesh_StoneVariant_PicksByIndex(int variant, int expected)
    {
        PrimitiveMeshes meshes = Track(ScriptableObject.CreateInstance<PrimitiveMeshes>());
        StoneVariants variants = Track(ScriptableObject.CreateInstance<StoneVariants>());
        variants.meshes = new Mesh[] { Track(new Mesh()), Track(new Mesh()) };
        meshes.stoneVariants = variants;

        Mesh mesh = meshes.GetMesh(Primitive.Stone, variant);

        Assert.AreEqual(variants.meshes[expected], mesh);
    }

    [Test]
    public void GetMesh_OtherPrimitiveWithVariant_IgnoresTheVariant()
    {
        Assert.AreEqual(RenderTestAssets.LoadMeshes().cone, RenderTestAssets.LoadMeshes().GetMesh(Primitive.Cone, 4));
    }

    [Test]
    public void GetMesh_ShippedAsset_EveryPrimitiveIsASavedMesh()
    {
        PrimitiveMeshes meshes = RenderTestAssets.LoadMeshes();

        foreach (Primitive primitive in Enum.GetValues(typeof(Primitive)))
        {
            Mesh mesh = meshes.GetMesh(primitive);

            Assert.IsNotNull(mesh, primitive.ToString());
            Assert.IsTrue(AssetDatabase.Contains(mesh), mesh.name);
            Assert.Greater(mesh.vertexCount, 0, mesh.name);
        }
    }

    [Test]
    public void GetMesh_ShippedSolids_AreClosedAndFaceOutward()
    {
        PrimitiveMeshes meshes = RenderTestAssets.LoadMeshes();

        foreach (Primitive primitive in Enum.GetValues(typeof(Primitive)))
        {
            RenderTestAssets.AssertClosed(meshes.GetMesh(primitive));
        }
    }

    [Test]
    public void Tuft_ShippedAsset_IsASavedMeshWithFourFacetedSides()
    {
        Mesh tuft = RenderTestAssets.LoadMeshes().tuft;

        uint indexCount = tuft.GetIndexCount(0);

        Assert.IsTrue(AssetDatabase.Contains(tuft));
        Assert.AreEqual(12u, indexCount); // 4 sides * 3, open at the base on its socle
    }

    // The disc, the annulus and the grass socle are ground markings, flat by nature: one side, facing up
    [Test]
    public void Disc_AnnulusAndSocle_AreSavedFlatAndFaceUp()
    {
        PrimitiveMeshes meshes = RenderTestAssets.LoadMeshes();

        foreach (Mesh mesh in new Mesh[] { meshes.disc, meshes.annulus, meshes.socle })
        {
            Assert.IsTrue(AssetDatabase.Contains(mesh), mesh.name);
            Vector3[] vertices = mesh.vertices;
            int[] triangles = mesh.triangles;
            for (int i = 0; i < triangles.Length; i += 3)
            {
                Vector3 a = vertices[triangles[i]];
                Vector3 normal = Vector3.Cross(vertices[triangles[i + 1]] - a, vertices[triangles[i + 2]] - a);
                Assert.Greater(normal.y, 0f, mesh.name);
                Assert.AreEqual(0f, a.y, mesh.name);
            }
        }
    }
}

}
