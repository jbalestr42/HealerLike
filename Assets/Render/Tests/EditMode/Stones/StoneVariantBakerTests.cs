using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace HealerLike.Render.Stones
{

public class StoneVariantBakerTests
{
    Mesh _first;
    Mesh _second;

    [TearDown]
    public void TearDown()
    {
        if (_first != null)
        {
            Object.DestroyImmediate(_first);
        }
        if (_second != null)
        {
            Object.DestroyImmediate(_second);
        }
    }

    static StoneVariants Variants()
    {
        return AssetDatabase.LoadAssetAtPath<StoneVariants>(StoneVariantBaker.VariantsPath);
    }

    [Test]
    public void Bake_BakedAsset_HoldsTwelveVariants()
    {
        StoneVariants variants = Variants();

        Assert.AreEqual(StoneVariantBaker.VariantCount, variants.meshes.Length);
    }

    [Test]
    public void Bake_EveryVariant_HasTwentyFacesInTwoSubmeshes()
    {
        StoneVariants variants = Variants();

        foreach (Mesh mesh in variants.meshes)
        {
            Assert.AreEqual(2, mesh.subMeshCount, mesh.name);
            Assert.AreEqual(60, mesh.GetTriangles(0).Length + mesh.GetTriangles(1).Length, mesh.name); // 20 faces
        }
    }

    [Test]
    public void Bake_EveryVariant_HasThreeToFiveOchreFacesTurnedToTheCamera()
    {
        StoneVariants variants = Variants();
        Vector3 view = StoneVariantBaker.ViewDirection();

        foreach (Mesh mesh in variants.meshes)
        {
            int[] ochre = mesh.GetTriangles(1);
            Vector3[] normals = mesh.normals;
            Assert.That(ochre.Length / 3, Is.InRange(3, 5), mesh.name);
            for (int i = 0; i < ochre.Length; i += 3)
            {
                Assert.Greater(Vector3.Dot(normals[ochre[i]], view), 0f, mesh.name);
            }
        }
    }

    [Test]
    public void Bake_EveryVariant_ShowsEightToTwelveFacetsToThePortraitCamera()
    {
        StoneVariants variants = Variants();
        Vector3 view = StoneVariantBaker.ViewDirection();

        foreach (Mesh mesh in variants.meshes)
        {
            Vector3[] normals = mesh.normals;
            int[] triangles = mesh.triangles;
            int visible = 0;
            for (int i = 0; i < triangles.Length; i += 3)
            {
                if (Vector3.Dot(normals[triangles[i]], view) > 0f)
                {
                    visible++;
                }
            }

            Assert.That(visible, Is.InRange(8, 12), mesh.name);
        }
    }

    [Test]
    public void Bake_EveryVariant_FitsTheBoulderBoxWithFiniteBoundsAndOutlineNormals()
    {
        StoneVariants variants = Variants();

        foreach (Mesh mesh in variants.meshes)
        {
            Bounds bounds = mesh.bounds;
            Assert.IsTrue(float.IsFinite(bounds.size.sqrMagnitude), mesh.name);
            Assert.Greater(bounds.size.x, 0f, mesh.name);
            Assert.LessOrEqual(bounds.max.y, 1.0001f, mesh.name);
            Assert.GreaterOrEqual(bounds.min.y, -0.7001f, mesh.name);
            Assert.LessOrEqual(Mathf.Max(bounds.extents.x, bounds.extents.z), 1.0001f, mesh.name);
            List<Vector3> outline = new List<Vector3>();
            mesh.GetUVs(3, outline);
            Assert.AreEqual(mesh.vertexCount, outline.Count, mesh.name);
        }
    }

    [Test]
    public void CreateVariant_SameSeed_GivesTheSameFaces()
    {
        _first = StoneVariantBaker.CreateVariant(7);

        _second = StoneVariantBaker.CreateVariant(7);

        Assert.AreEqual(_first.vertices, _second.vertices);
        Assert.AreEqual(_first.GetTriangles(1), _second.GetTriangles(1));
    }
}

}
