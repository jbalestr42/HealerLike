using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEditor;

namespace HealerLike.Render.Creatures
{

public class PrimitiveMeshBakerTests
{
    static readonly string meshesFolder = "Assets/Render/Creatures/Meshes/";
    readonly List<Mesh> _built = new List<Mesh>();

    [TearDown]
    public void TearDown()
    {
        foreach (Mesh mesh in _built)
        {
            Object.DestroyImmediate(mesh);
        }

        _built.Clear();
    }

    // Every primitive the baker writes, from its builder with the baker's arguments
    static List<Mesh> BuildEveryPrimitive()
    {
        return new List<Mesh>
        {
            RevolvedMeshes.Create("Sphere", Primitive.Sphere, 12, 6, 0.2f),
            RevolvedMeshes.Create("Capsule", Primitive.Capsule, 12, 6, 0.2f),
            RevolvedMeshes.Create("Cone", Primitive.Cone, 12, 6, 0.2f),
            RevolvedMeshes.Create("Cylinder", Primitive.CylinderSegment, 6, 6, 0.2f),
            RevolvedMeshes.Create("Torus", Primitive.Torus, 12, 6, 0.2f),
            FacetedMeshes.CreatePyramid("Tuft", false),
            FacetedMeshes.CreateSocle(),
            FacetedMeshes.CreatePyramid("Pyramid", true),
            FacetedMeshes.CreateLeaf(),
            FacetedMeshes.CreateBoulder(),
            RingMeshes.CreateDisc(32),
            RingMeshes.CreateAnnulus(128),
        };
    }

    [Test]
    public void CreateMesh_EveryBuilder_MatchesTheCommittedAsset()
    {
        _built.AddRange(BuildEveryPrimitive());
        foreach (Mesh mesh in _built)
        {
            Mesh baked = AssetDatabase.LoadAssetAtPath<Mesh>(meshesFolder + mesh.name + ".asset");
            Assert.IsNotNull(baked, mesh.name);
            CollectionAssert.AreEqual(baked.vertices, mesh.vertices, mesh.name);
            CollectionAssert.AreEqual(baked.normals, mesh.normals, mesh.name);
            CollectionAssert.AreEqual(baked.triangles, mesh.triangles, mesh.name);
            CollectionAssert.AreEqual(baked.uv, mesh.uv, mesh.name);
        }
    }
}
}
