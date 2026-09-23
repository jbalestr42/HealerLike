using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace HealerLike.Render.Creatures
{

public class PrimitiveMeshBakerTests
{
    static readonly string meshesAssetPath = "Assets/Render/Creatures/Data/PrimitiveMeshes.asset";

    [Test]
    public void Bake_ShippedAsset_ReferencesEveryMesh()
    {
        PrimitiveMeshes meshes = AssetDatabase.LoadAssetAtPath<PrimitiveMeshes>(meshesAssetPath);

        Assert.IsNotNull(meshes);
        Mesh[] all =
        {
            meshes.sphere, meshes.capsule, meshes.cone, meshes.cylinder, meshes.torus, meshes.thinTorus,
            meshes.bladeCone, meshes.pyramid, meshes.star, meshes.boulder, meshes.disc, meshes.annulus
        };
        foreach (Mesh mesh in all)
        {
            Assert.IsNotNull(mesh);
            Assert.IsTrue(AssetDatabase.Contains(mesh), mesh.name);
            Assert.Greater(mesh.vertexCount, 0, mesh.name);
        }
    }

    [Test]
    public void Bake_ShippedAsset_BladeConeHasGrassSides()
    {
        PrimitiveMeshes meshes = AssetDatabase.LoadAssetAtPath<PrimitiveMeshes>(meshesAssetPath);

        uint indexCount = meshes.bladeCone.GetIndexCount(0);

        Assert.AreEqual(9u * (uint)Grass.GrassField.BladeSides, indexCount); // side quads and base cap
    }
}
}
