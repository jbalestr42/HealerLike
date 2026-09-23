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
            meshes.tuft, meshes.pyramid, meshes.star, meshes.boulder, meshes.disc, meshes.annulus
        };
        foreach (Mesh mesh in all)
        {
            Assert.IsNotNull(mesh);
            Assert.IsTrue(AssetDatabase.Contains(mesh), mesh.name);
            Assert.Greater(mesh.vertexCount, 0, mesh.name);
        }
    }

    [Test]
    public void Bake_ShippedAsset_TuftHasThreeCappedPyramids()
    {
        PrimitiveMeshes meshes = AssetDatabase.LoadAssetAtPath<PrimitiveMeshes>(meshesAssetPath);

        uint indexCount = meshes.tuft.GetIndexCount(0);

        Assert.AreEqual(54u, indexCount); // 3 pyramids * (4 sides + 2 cap triangles) * 3
    }
}

}
