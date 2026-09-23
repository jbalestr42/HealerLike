using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace HealerLike.Render.Creatures
{

public class PrimitiveMeshBakerTests
{
    static readonly string meshesAssetPath = "Assets/Render/Creatures/Data/PrimitiveMeshes.asset";

    // Welds corners by position, skips zero-area triangles, then needs every edge used once in each direction
    // and a positive enclosed volume: a closed solid with its faces turned outward.
    public static void AssertClosed(Mesh mesh)
    {
        Vector3[] vertices = mesh.vertices;
        int[] triangles = mesh.triangles;
        Dictionary<Vector3Int, int> welded = new Dictionary<Vector3Int, int>();
        int[] ids = new int[vertices.Length];
        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3Int key = Vector3Int.RoundToInt(vertices[i] * 100000f);
            if (!welded.TryGetValue(key, out ids[i]))
            {
                ids[i] = welded.Count;
                welded.Add(key, ids[i]);
            }
        }

        Dictionary<long, int> edges = new Dictionary<long, int>();
        float volume = 0f;
        for (int i = 0; i < triangles.Length; i += 3)
        {
            Vector3 a = vertices[triangles[i]];
            Vector3 b = vertices[triangles[i + 1]];
            Vector3 c = vertices[triangles[i + 2]];
            int[] corners = { ids[triangles[i]], ids[triangles[i + 1]], ids[triangles[i + 2]] };
            bool isDegenerate = corners[0] == corners[1] || corners[1] == corners[2] || corners[2] == corners[0];
            if (isDegenerate || Vector3.Cross(b - a, c - a).sqrMagnitude < 0.00000000000001f)
            {
                continue;
            }

            volume += Vector3.Dot(a, Vector3.Cross(b, c)) / 6f;
            for (int j = 0; j < 3; j++)
            {
                long edge = (long)corners[j] * welded.Count + corners[(j + 1) % 3];
                edges.TryGetValue(edge, out int count);
                edges[edge] = count + 1;
            }
        }

        foreach (KeyValuePair<long, int> edge in edges)
        {
            long from = edge.Key / welded.Count;
            long to = edge.Key % welded.Count;
            edges.TryGetValue(to * welded.Count + from, out int reverse);
            Assert.AreEqual(1, edge.Value, mesh.name + " has an edge used twice in one direction");
            Assert.AreEqual(1, reverse, mesh.name + " has an open or one-sided edge");
        }

        Assert.Greater(volume, 0f, mesh.name + " encloses no volume or faces inward");
    }

    [Test]
    public void Bake_ShippedAsset_ReferencesEveryMesh()
    {
        PrimitiveMeshes meshes = AssetDatabase.LoadAssetAtPath<PrimitiveMeshes>(meshesAssetPath);

        Assert.IsNotNull(meshes);
        Mesh[] all =
        {
            meshes.sphere, meshes.capsule, meshes.cone, meshes.cylinder, meshes.torus, meshes.thinTorus,
            meshes.tuft, meshes.pyramid, meshes.star, meshes.leaf, meshes.boulder, meshes.disc, meshes.annulus
        };
        foreach (Mesh mesh in all)
        {
            Assert.IsNotNull(mesh);
            Assert.IsTrue(AssetDatabase.Contains(mesh), mesh.name);
            Assert.Greater(mesh.vertexCount, 0, mesh.name);
        }
    }

    [Test]
    public void Bake_ShippedAsset_TuftHasFourFacetedSidesAndACap()
    {
        PrimitiveMeshes meshes = AssetDatabase.LoadAssetAtPath<PrimitiveMeshes>(meshesAssetPath);

        uint indexCount = meshes.tuft.GetIndexCount(0);

        Assert.AreEqual(66u, indexCount); // (4 sides * 5 triangles + 2 cap triangles) * 3
    }

    [Test]
    public void Bake_ShippedSolids_AreClosedAndFaceOutward()
    {
        PrimitiveMeshes meshes = AssetDatabase.LoadAssetAtPath<PrimitiveMeshes>(meshesAssetPath);
        Mesh[] solids =
        {
            meshes.sphere, meshes.capsule, meshes.cone, meshes.cylinder, meshes.torus, meshes.thinTorus,
            meshes.pyramid, meshes.star, meshes.leaf, meshes.boulder
        };

        foreach (Mesh mesh in solids)
        {
            AssertClosed(mesh);
        }
    }

    // The disc and the annulus are ground markings, flat by nature: one side, facing up
    [Test]
    public void Bake_ShippedGroundMarkings_AreFlatAndFaceUp()
    {
        PrimitiveMeshes meshes = AssetDatabase.LoadAssetAtPath<PrimitiveMeshes>(meshesAssetPath);

        foreach (Mesh mesh in new Mesh[] { meshes.disc, meshes.annulus })
        {
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

    [Test]
    public void Bake_ShippedStar_IsThickAndSpiky()
    {
        Mesh star = AssetDatabase.LoadAssetAtPath<PrimitiveMeshes>(meshesAssetPath).star;

        Assert.That(star.bounds.size.x, Is.EqualTo(2f).Within(0.00001)); // rays reach one unit
        Assert.That(star.bounds.size.z, Is.EqualTo(0.6f).Within(0.00001)); // an apex on each face
    }
}

}
