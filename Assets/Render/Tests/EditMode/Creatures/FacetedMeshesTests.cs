using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Creatures
{

public class FacetedMeshesTests
{
    Mesh _mesh;

    [SetUp]
    public void SetUp()
    {
        _mesh = FacetedMeshes.CreatePyramid("Tuft", false);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_mesh);
    }

    // Counts each undirected edge by the positions it joins, so split flat-shaded vertices still meet
    static Dictionary<string, int> CountEdges(Mesh mesh)
    {
        Vector3[] vertices = mesh.vertices;
        int[] triangles = mesh.triangles;
        Dictionary<string, int> edges = new Dictionary<string, int>();
        for (int i = 0; i < triangles.Length; i += 3)
        {
            for (int j = 0; j < 3; j++)
            {
                string a = vertices[triangles[i + j]].ToString("F5");
                string b = vertices[triangles[i + (j + 1) % 3]].ToString("F5");
                string key = string.CompareOrdinal(a, b) < 0 ? a + b : b + a;
                edges.TryGetValue(key, out int count);
                edges[key] = count + 1;
            }
        }

        return edges;
    }

    [Test]
    public void CreatePyramid_Open_IsFourFlatSidesOpenAtTheBase()
    {
        Dictionary<string, int> edges = CountEdges(_mesh);
        int baseEdges = 0;
        Assert.AreEqual(FacetedMeshes.TuftIndexCount, _mesh.triangles.Length);
        foreach (KeyValuePair<string, int> edge in edges)
        {
            // The four base edges close on the ground, the four side edges join two facets
            bool isBaseEdge = !edge.Key.Contains("1.00000");
            int expectedFaces = isBaseEdge ? 1 : 2;
            Assert.AreEqual(expectedFaces, edge.Value, edge.Key);
            baseEdges += isBaseEdge ? 1 : 0;
        }

        Assert.AreEqual(4, baseEdges);
        Assert.AreEqual(new Vector3(1f, 1f, 1f), _mesh.bounds.size);
        Assert.AreEqual(0f, _mesh.bounds.min.y);
    }

    [Test]
    public void CreatePyramid_Open_FacetNormalsPointOutOfTheBody()
    {
        Vector3[] vertices = _mesh.vertices;
        Vector3[] normals = _mesh.normals;
        int[] triangles = _mesh.triangles;
        Vector3 inside = new Vector3(0f, 0.3f, 0f);
        // The pyramid is convex, so every facet faces away from a point on its axis, and each facet is flat
        for (int i = 0; i < triangles.Length; i += 3)
        {
            Vector3 face = (vertices[triangles[i]] + vertices[triangles[i + 1]] + vertices[triangles[i + 2]]) / 3f;
            Assert.Greater(Vector3.Dot(normals[triangles[i]], face - inside), 0f);
            Assert.AreEqual(normals[triangles[i]], normals[triangles[i + 1]]);
            Assert.AreEqual(normals[triangles[i]], normals[triangles[i + 2]]);
        }
    }

    [Test]
    public void CreatePyramid_Open_AuthorsNoColours()
    {
        Assert.IsEmpty(_mesh.colors);
    }

    [Test]
    public void CreateSocle_Octagon_IsAFlatFanFacingUp()
    {
        Mesh socle = FacetedMeshes.CreateSocle();
        Vector3[] vertices = socle.vertices;
        int[] triangles = socle.triangles;
        Assert.AreEqual(FacetedMeshes.SocleIndexCount, triangles.Length);
        for (int i = 0; i < triangles.Length; i += 3)
        {
            Vector3 a = vertices[triangles[i]];
            Vector3 normal = Vector3.Cross(vertices[triangles[i + 1]] - a, vertices[triangles[i + 2]] - a);
            Assert.Greater(normal.y, 0f);
            Assert.AreEqual(Vector3.up, socle.normals[triangles[i]]);
        }

        foreach (Vector3 vertex in vertices)
        {
            Assert.AreEqual(0f, vertex.y);
            float radius = new Vector2(vertex.x, vertex.z).magnitude;
            Assert.That(
                radius == 0f || Mathf.Abs(radius - FacetedMeshes.SocleRadius) < 0.00001f,
                vertex.ToString()
            );
        }

        Assert.That(FacetedMeshes.SocleRadius, Is.EqualTo(1.2414f).Within(0.0001f)); // 0.36 / 0.29
        // Each rim edge once, each spoke between two triangles
        int rim = 0;
        foreach (KeyValuePair<string, int> edge in CountEdges(socle))
        {
            rim += edge.Value == 1 ? 1 : 0;
        }

        Assert.AreEqual(FacetedMeshes.SocleSides, rim);
        Object.DestroyImmediate(socle);
    }
}
}
