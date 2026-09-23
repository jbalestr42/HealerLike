using System.Collections.Generic;
using HealerLike.Render.Creatures;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace HealerLike.Render.Grass
{

public class GrassTuftTests
{
    Mesh _mesh;

    [SetUp]
    public void SetUp()
    {
        _mesh = GrassTuft.CreateMesh();
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
    public void Tilt_KnownLean_MatchesAngleAxis()
    {
        Vector2 lean = new Vector2(0.2f, -0.25f);
        float degrees = lean.magnitude * Mathf.Rad2Deg;
        Vector3 axis = new Vector3(lean.y, 0f, -lean.x).normalized;
        Vector3 tip = new Vector3(0.1f, 1f, -0.2f);

        Vector3 tilted = GrassTuft.Tilt(tip, lean);

        Vector3 expected = Quaternion.AngleAxis(degrees, axis) * tip;
        Assert.That(Vector3.Distance(expected, tilted), Is.LessThan(0.00001f));
    }

    [Test]
    public void Tilt_LeanTowardX_MovesTheTipTowardX()
    {
        Vector3 tip = GrassTuft.Tilt(Vector3.up, new Vector2(0.3f, 0f));

        Assert.That(tip.x, Is.EqualTo(Mathf.Sin(0.3f)).Within(0.00001f));
        Assert.That(tip.y, Is.EqualTo(Mathf.Cos(0.3f)).Within(0.00001f));
        Assert.That(tip.z, Is.EqualTo(0f).Within(0.00001f));
    }

    [Test]
    public void Tilt_NoLean_KeepsTheVector()
    {
        Vector3 v = new Vector3(0.3f, 0.7f, -0.4f);

        Assert.That(Vector3.Distance(v, GrassTuft.Tilt(v, Vector2.zero)), Is.LessThan(0.00001f));
    }

    [Test]
    public void Place_UnitTuft_MatchesRootYawAndAngleAxisTilt()
    {
        Vector3 root = new Vector3(2f, 0.505f, -3f);
        float yaw = 1.1f;
        Vector2 lean = new Vector2(-0.3f, 0.1f);
        Vector3 axis = new Vector3(lean.y, 0f, -lean.x).normalized;
        Quaternion rotation = Quaternion.AngleAxis(lean.magnitude * Mathf.Rad2Deg, axis) * Quaternion.Euler(0f, yaw * Mathf.Rad2Deg, 0f);

        foreach (Vector3 vertex in _mesh.vertices)
        {
            Vector3 placed = GrassTuft.Place(vertex, root, yaw, 0.085f, 0.13f, lean);

            Vector3 expected = root + rotation * Vector3.Scale(vertex, new Vector3(0.085f, 0.13f, 0.085f));
            Assert.That(Vector3.Distance(expected, placed), Is.LessThan(0.00001f));
        }
    }

    [Test]
    public void Place_RigidLean_KeepsTheRootAndTheTipDistance()
    {
        Vector3 root = new Vector3(0f, 0.5f, 0f);

        Vector3 upright = GrassTuft.Place(Vector3.up, root, 0.4f, 0.085f, 0.13f, Vector2.zero);
        Vector3 leaning = GrassTuft.Place(Vector3.up, root, 0.4f, 0.085f, 0.13f, new Vector2(0.35f, 0f));

        Assert.AreEqual(root, GrassTuft.Place(Vector3.zero, root, 0.4f, 0.085f, 0.13f, new Vector2(0.35f, 0f)));
        Assert.That((leaning - root).magnitude, Is.EqualTo((upright - root).magnitude).Within(0.00001f));
        Assert.That(Vector3.Angle(upright - root, leaning - root), Is.EqualTo(0.35f * Mathf.Rad2Deg).Within(0.01f));
    }

    [Test]
    public void PlaceNormal_UnscaledTuft_TurnsWithTheSameRotation()
    {
        Vector2 lean = new Vector2(0.2f, 0.15f);
        Vector3 axis = new Vector3(lean.y, 0f, -lean.x).normalized;
        Quaternion rotation = Quaternion.AngleAxis(lean.magnitude * Mathf.Rad2Deg, axis) * Quaternion.Euler(0f, 40f, 0f);
        Vector3 normal = new Vector3(0f, 0.447f, -0.894f).normalized;

        Vector3 placed = GrassTuft.PlaceNormal(normal, 40f * Mathf.Deg2Rad, 1f, 1f, lean);

        Assert.That(Vector3.Distance(rotation * normal, placed), Is.LessThan(0.00001f));
    }

    [Test]
    public void CreateMesh_EveryEdge_IsSharedByTwoTriangles()
    {
        Dictionary<string, int> edges = CountEdges(_mesh);

        Assert.AreEqual(GrassTuft.BodyCount * GrassTuft.IndicesPerBody, _mesh.triangles.Length);
        foreach (KeyValuePair<string, int> edge in edges)
        {
            Assert.AreEqual(2, edge.Value, edge.Key);
        }
    }

    [Test]
    public void CreateMesh_FacetNormals_PointOutOfEachBody()
    {
        Vector3[] vertices = _mesh.vertices;
        Vector3[] normals = _mesh.normals;
        int[] triangles = _mesh.triangles;

        // Each body is 18 indices; its centroid is inside it, so every facet faces away from it
        for (int body = 0; body < GrassTuft.BodyCount; body++)
        {
            int start = body * GrassTuft.IndicesPerBody;
            Vector3 centroid = Vector3.zero;
            for (int i = start; i < start + GrassTuft.IndicesPerBody; i++)
            {
                centroid += vertices[triangles[i]] / GrassTuft.IndicesPerBody;
            }

            for (int i = start; i < start + GrassTuft.IndicesPerBody; i += 3)
            {
                Vector3 face = (vertices[triangles[i]] + vertices[triangles[i + 1]] + vertices[triangles[i + 2]]) / 3f;
                Assert.Greater(Vector3.Dot(normals[triangles[i]], face - centroid), 0f);
            }
        }
    }

    [Test]
    public void Bake_ShippedTuft_IsClosedAndMatchesTheBuilder()
    {
        Mesh shipped = AssetDatabase.LoadAssetAtPath<PrimitiveMeshes>("Assets/Render/Creatures/Data/PrimitiveMeshes.asset").tuft;

        Assert.IsNotNull(shipped);
        CollectionAssert.AreEqual(_mesh.vertices, shipped.vertices);
        foreach (KeyValuePair<string, int> edge in CountEdges(shipped))
        {
            Assert.AreEqual(2, edge.Value, edge.Key);
        }
    }
}

}
