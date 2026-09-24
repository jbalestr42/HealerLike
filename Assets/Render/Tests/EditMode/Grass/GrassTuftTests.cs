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
        Quaternion rotation = Quaternion.AngleAxis(lean.magnitude * Mathf.Rad2Deg, axis)
                              * Quaternion.Euler(0f, yaw * Mathf.Rad2Deg, 0f);

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
        Quaternion rotation = Quaternion.AngleAxis(lean.magnitude * Mathf.Rad2Deg, axis)
                              * Quaternion.Euler(0f, 40f, 0f);
        Vector3 normal = new Vector3(0f, 0.447f, -0.894f).normalized;

        Vector3 placed = GrassTuft.PlaceNormal(normal, 40f * Mathf.Deg2Rad, 1f, 1f, lean);

        Assert.That(Vector3.Distance(rotation * normal, placed), Is.LessThan(0.00001f));
    }

    [Test]
    public void Place_HalfRadianLean_MovesTheApexByItsSine()
    {
        Vector3 root = new Vector3(1f, 0.505f, 2f);

        Vector3 apex = GrassTuft.Place(Vector3.up, root, 0.7f, 0.08f, 0.25f, new Vector2(0f, 0.5f));

        Assert.That(apex.z - root.z, Is.EqualTo(0.25f * Mathf.Sin(0.5f)).Within(0.00001f)); // 0.12 sideways
        Assert.That(apex.y - root.y, Is.EqualTo(0.25f * Mathf.Cos(0.5f)).Within(0.00001f));
        Assert.That(apex.x - root.x, Is.EqualTo(0f).Within(0.00001f));
    }

    [Test]
    public void CreateMesh_Pyramid_IsFourFlatSidesOpenAtTheBase()
    {
        Dictionary<string, int> edges = CountEdges(_mesh);
        int baseEdges = 0;

        Assert.AreEqual(GrassTuft.IndexCount, _mesh.triangles.Length);
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
    public void CreateMesh_FacetNormals_PointOutOfTheBody()
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
    public void CreateMesh_Colours_AreNotAuthored()
    {
        Assert.IsEmpty(_mesh.colors);
    }

    [Test]
    public void CreateSocle_Octagon_IsAFlatFanFacingUp()
    {
        Mesh socle = GrassTuft.CreateSocle();
        Vector3[] vertices = socle.vertices;
        int[] triangles = socle.triangles;

        Assert.AreEqual(GrassTuft.SocleIndexCount, triangles.Length);
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
            Assert.That(radius == 0f || Mathf.Abs(radius - GrassTuft.SocleRadius) < 0.00001f, vertex.ToString());
        }
        Assert.That(GrassTuft.SocleRadius, Is.EqualTo(1.2414f).Within(0.0001f)); // 0.36 / 0.29

        // Each rim edge once, each spoke between two triangles
        int rim = 0;
        foreach (KeyValuePair<string, int> edge in CountEdges(socle))
        {
            rim += edge.Value == 1 ? 1 : 0;
        }
        Assert.AreEqual(GrassTuft.SocleSides, rim);
        Object.DestroyImmediate(socle);
    }

    [Test]
    public void Bake_ShippedTuftAndSocle_MatchTheBuilder()
    {
        string meshesPath = "Assets/Render/Creatures/Data/PrimitiveMeshes.asset";
        PrimitiveMeshes meshes = AssetDatabase.LoadAssetAtPath<PrimitiveMeshes>(meshesPath);
        Mesh socle = GrassTuft.CreateSocle();

        Assert.IsNotNull(meshes.tuft);
        Assert.IsNotNull(meshes.socle);
        CollectionAssert.AreEqual(_mesh.vertices, meshes.tuft.vertices);
        CollectionAssert.AreEqual(_mesh.triangles, meshes.tuft.triangles);
        CollectionAssert.AreEqual(socle.vertices, meshes.socle.vertices);
        CollectionAssert.AreEqual(socle.triangles, meshes.socle.triangles);
        Object.DestroyImmediate(socle);
    }
}

}
