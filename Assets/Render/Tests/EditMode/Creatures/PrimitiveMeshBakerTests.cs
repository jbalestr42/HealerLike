using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

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
            RingMeshes.CreateAnnulus(128)
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
}

}
