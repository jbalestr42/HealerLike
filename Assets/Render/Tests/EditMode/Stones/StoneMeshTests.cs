using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace HealerLike.Render.Stones
{

public class StoneMeshTests
{
    [TestCase(0, 60)]
    [TestCase(1, 240)]
    [TestCase(2, 960)]
    public void CountsAndFlatNormals(int n, int count)
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
            Assert.That(data.normals[i].magnitude, Is.EqualTo(1).Within(1e-5));
            Assert.AreEqual(data.normals[i], data.normals[i + 1]);
            Assert.AreEqual(data.normals[i], data.normals[i + 2]);
            Assert.That(Vector3.Dot(data.normals[i], (a + b + c)), Is.GreaterThan(0));
            Vector3 faceNormal = Vector3.Cross(b - a, c - a).normalized;
            Assert.That(Vector3.Distance(data.normals[i], faceNormal), Is.LessThan(1e-5));
            for (int j = 0; j < 3; j++)
            {
                Vector3 vertex = data.vertices[i + j];
                Assert.That(Vector3.Distance(data.bounds.ClosestPoint(vertex), vertex), Is.LessThan(1e-6));
            }
        }
    }

    [TestCase(0u)]
    [TestCase(1u)]
    [TestCase(uint.MaxValue)]
    [TestCase(827361u)]
    public void DeterministicAndIndependentOfGlobalRandom(uint seed)
    {
        Random.State state = Random.state;
        try
        {
            StoneMeshData a = StoneMesh.Generate(seed, StonePresets.Boulder);
            Assert.AreEqual(state, Random.state);

            Random.InitState(128);
            StoneMesh.Generate(seed + 1, StonePresets.Monolith);
            StoneMeshData b = StoneMesh.Generate(seed, StonePresets.Boulder);
            CollectionAssert.AreEqual(a.vertices, b.vertices);
            CollectionAssert.AreEqual(a.normals, b.normals);
            CollectionAssert.AreEqual(a.indices, b.indices);
            StoneMeshData next = StoneMesh.Generate(seed + 1, StonePresets.Boulder);
            CollectionAssert.AreNotEqual(a.vertices, next.vertices);
        }
        finally
        {
            Random.state = state;
        }
    }

    [Test]
    public void SweepAllSeedsPresetsAndExtremeScalesWithoutDegeneracy()
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
            if (Vector3.Cross(Vector3.Scale(b - a, inv), Vector3.Scale(c - a, inv)).magnitude <= 1e-10f)
            {
                Assert.Fail("Degenerate normalized face");
            }
            if (Mathf.Abs(data.normals[i].magnitude - 1f) > 1e-5f)
            {
                Assert.Fail("Non-unit normal");
            }
        }
    }

    [Test]
    public void CanonicalGeometryHashIsPinned()
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
            hash = StoneSeed.ForPart(hash, unchecked((uint)System.BitConverter.SingleToInt32Bits(component)));
        }
        return hash;
    }

    [Test]
    public void InvalidSettingsRejectedAndMeshCopiesData()
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
        LogAssert.Expect(LogType.Error, new Regex(@"\[StoneMesh\] Subdivisions"));
        Assert.AreEqual(0, StoneMesh.VertexCount(3));

        StoneMeshData data = StoneMesh.Generate(8, StonePresets.Boulder);
        Mesh mesh = StoneMesh.CreateMesh(data);
        try
        {
            CollectionAssert.AreEqual(data.vertices, mesh.vertices);
            CollectionAssert.AreEqual(data.normals, mesh.normals);
            Assert.AreEqual(240, mesh.vertexCount);
        }
        finally
        {
            Object.DestroyImmediate(mesh);
        }
    }
}

}
