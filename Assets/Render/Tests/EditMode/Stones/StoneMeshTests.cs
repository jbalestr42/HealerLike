using System.Text.RegularExpressions;
using HealerLike.Render.Creatures;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace HealerLike.Render.Stones
{

public class StoneMeshTests
{
    Random.State _randomState;
    Mesh _mesh;

    [SetUp]
    public void SetUp()
    {
        _randomState = Random.state;
    }

    [TearDown]
    public void TearDown()
    {
        Random.state = _randomState;
        if (_mesh != null)
        {
            Object.DestroyImmediate(_mesh);
        }
    }

    [Test]
    public void CreateMesh_Boulder_IsClosedAndFacesOutward()
    {
        _mesh = StoneMesh.CreateMesh(3u, StonePresets.Boulder);

        RenderTestAssets.AssertClosed(_mesh);
    }

    [TestCase(0, 60)]
    [TestCase(1, 240)]
    [TestCase(2, 960)]
    public void Generate_Subdivisions_HasExpectedCountsAndFlatOutwardNormals(int n, int count)
    {
        StoneSettings settings = StonePresets.Boulder;
        settings.subdivisions = n;
        StoneMeshData data = StoneMesh.Generate(0, settings);
        Assert.AreEqual(count, data.vertices.Length);
        Assert.AreEqual(count, data.indices.Length);
        for (int i = 0; i < count; i += 3)
        {
            Vector3 a = data.vertices[i];
            Vector3 b = data.vertices[i + 1];
            Vector3 c = data.vertices[i + 2];
            Assert.That(data.normals[i].magnitude, Is.EqualTo(1).Within(0.00001));
            Assert.AreEqual(data.normals[i], data.normals[i + 1]);
            Assert.AreEqual(data.normals[i], data.normals[i + 2]);
            Assert.That(Vector3.Dot(data.normals[i], (a + b + c)), Is.GreaterThan(0));
            Vector3 faceNormal = Vector3.Cross(b - a, c - a).normalized;
            Assert.That(Vector3.Distance(data.normals[i], faceNormal), Is.LessThan(0.00001));
            for (int j = 0; j < 3; j++)
            {
                Vector3 vertex = data.vertices[i + j];
                Assert.That(Vector3.Distance(data.bounds.ClosestPoint(vertex), vertex), Is.LessThan(0.000001));
            }
        }
    }

    [TestCase(0u)]
    [TestCase(1u)]
    [TestCase(uint.MaxValue)]
    [TestCase(827361u)]
    public void Generate_SameSeed_IsDeterministicAndLeavesUnityRandomAlone(uint seed)
    {
        StoneMeshData a = StoneMesh.Generate(seed, StonePresets.Boulder);
        Assert.AreEqual(_randomState, Random.state);

        Random.InitState(128);
        StoneMesh.Generate(seed + 1, StonePresets.Monolith);
        StoneMeshData b = StoneMesh.Generate(seed, StonePresets.Boulder);

        CollectionAssert.AreEqual(a.vertices, b.vertices);
        CollectionAssert.AreEqual(a.normals, b.normals);
        CollectionAssert.AreEqual(a.indices, b.indices);
        StoneMeshData next = StoneMesh.Generate(seed + 1, StonePresets.Boulder);
        CollectionAssert.AreNotEqual(a.vertices, next.vertices);
    }

    [Test]
    public void Generate_EverySeedPresetAndExtremeScale_HasNoDegenerateFace()
    {
        StoneSettings[] shapes = { StonePresets.Boulder, StonePresets.Cairn, StonePresets.Monolith };
        float[] roughnessValues = { 0f, 0.18f };
        for (int n = 0; n <= 2; n++)
        {
            foreach (StoneSettings shape in shapes)
            {
                foreach (float rough in roughnessValues)
                {
                    for (int axes = 0; axes < 8; axes++)
                    {
                        for (uint seed = 0; seed < 1024; seed++)
                        {
                            StoneSettings settings = shape;
                            settings.subdivisions = n;
                            settings.roughness = rough;
                            // Exercise every seed with all eight independent min/max scale combinations.
                            settings.size = (axes & 1) == 0 ? 0.02f : 8f;
                            settings.elongation = (axes & 2) == 0 ? 0.25f : 5f;
                            settings.depthRatio = (axes & 4) == 0 ? 0.25f : 2f;
                            CheckFaces(StoneMesh.Generate(seed, settings), settings, seed, n);
                        }
                    }
                }
            }
        }
    }

    static void CheckFaces(StoneMeshData data, StoneSettings settings, uint seed, int n)
    {
        float height = settings.size * settings.elongation;
        float depth = settings.size * settings.depthRatio;
        Vector3 inv = new Vector3(2f / settings.size, 2f / height, 2f / depth);
        for (int i = 0; i < data.vertices.Length; i += 3)
        {
            Vector3 a = data.vertices[i];
            Vector3 b = data.vertices[i + 1];
            Vector3 c = data.vertices[i + 2];
            Vector3 cross = Vector3.Cross(b - a, c - a);
            if (!float.IsFinite(cross.sqrMagnitude) || cross.sqrMagnitude <= 0f
                || Vector3.Dot(cross, a + b + c) <= 0f)
            {
                Assert.Fail($"Bad scaled face seed {seed}, n {n}");
            }
            if (Vector3.Cross(Vector3.Scale(b - a, inv), Vector3.Scale(c - a, inv)).magnitude <= 0.0000000001f)
            {
                Assert.Fail("Degenerate normalized face");
            }
            if (Mathf.Abs(data.normals[i].magnitude - 1f) > 0.00001f)
            {
                Assert.Fail("Non-unit normal");
            }
        }
    }

    [Test]
    public void Generate_CanonicalSeed_MatchesPinnedGeometryHash()
    {
        uint hash = 2166136261;
        StoneMeshData data = StoneMesh.Generate(827361, StonePresets.Boulder);
        foreach (Vector3 vertex in data.vertices)
        {
            hash = HashVector(hash, vertex);
        }
        foreach (Vector3 normal in data.normals)
        {
            hash = HashVector(hash, normal);
        }
        foreach (int index in data.indices)
        {
            hash = StoneSeed.ForPart(hash, (uint)index);
        }
        Assert.AreEqual(3051263645u, hash, "Version-1 geometry golden on Unity 6000.6");
    }

    static uint HashVector(uint hash, Vector3 value)
    {
        foreach (float component in new float[] { value.x, value.y, value.z })
        {
            hash = StoneSeed.ForPart(hash, (uint)System.BitConverter.SingleToInt32Bits(component));
        }
        return hash;
    }

    [Test]
    public void Generate_InvalidSettings_LogsAndReturnsNoStone()
    {
        StoneSettings settings = StonePresets.Boulder;
        settings.size = float.NaN;
        LogAssert.Expect(LogType.Error, new Regex(@"\[StoneMesh\] No stone"));
        Assert.IsNull(StoneMesh.Generate(0, settings).vertices);

        settings = StonePresets.Boulder;
        settings.elongation = 0f;
        LogAssert.Expect(LogType.Error, new Regex(@"\[StoneMesh\] No stone"));
        Assert.IsNull(StoneMesh.Generate(0, settings).vertices);

        settings = StonePresets.Boulder;
        settings.depthRatio = 3f;
        LogAssert.Expect(LogType.Error, new Regex(@"\[StoneMesh\] No stone"));
        Assert.IsNull(StoneMesh.Generate(0, settings).vertices);

        settings = StonePresets.Boulder;
        settings.roughness = 0.19f;
        Assert.IsFalse(StoneMesh.TryGenerate(0, settings, out StoneMeshData rejected));
        Assert.IsNull(rejected.vertices);
        LogAssert.Expect(LogType.Error, new Regex(@"\[StoneMesh\] No stone mesh"));
        Assert.IsNull(StoneMesh.CreateMesh(0, settings));
    }

    [Test]
    public void CreateMesh_GeneratedData_CopiesVerticesAndNormals()
    {
        StoneMeshData data = StoneMesh.Generate(8, StonePresets.Boulder);

        _mesh = StoneMesh.CreateMesh(data);

        CollectionAssert.AreEqual(data.vertices, _mesh.vertices);
        CollectionAssert.AreEqual(data.normals, _mesh.normals);
        Assert.AreEqual(240, _mesh.vertexCount);
    }
}

}
