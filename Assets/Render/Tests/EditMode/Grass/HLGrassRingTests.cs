using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace HealerLike.Render.Grass
{

public class HLGrassRingTests
{
    Mesh _mesh;

    [TearDown]
    public void TearDown()
    {
        if (_mesh != null)
        {
            Object.DestroyImmediate(_mesh);
        }
    }

    [Test]
    public void CreateAnnulus_Default_ClosesTheSeamAndWindsUpward()
    {
        _mesh = HLGrassRing.CreateAnnulus();

        Assert.AreEqual(258, _mesh.vertexCount); // 129 spokes, inner and outer
        Assert.AreEqual(768, _mesh.triangles.Length);
        Vector3[] vertices = _mesh.vertices;
        int[] indices = _mesh.triangles;
        Assert.AreEqual(vertices[0], vertices[256]);
        Assert.AreEqual(vertices[1], vertices[257]);
        for (int i = 0; i < indices.Length; i += 3)
        {
            Vector3 normal = Vector3.Cross(vertices[indices[i + 1]] - vertices[indices[i]], vertices[indices[i + 2]] - vertices[indices[i]]);
            Assert.Greater(normal.y, 0f);
        }
    }

    [Test]
    public void CreateAnnulus_TooFewSegments_LogsAndReturnsNull()
    {
        LogAssert.Expect(LogType.Error, "[HLGrassRing] Rejected 2 segments, the ring needs 3 to 16000.");

        _mesh = HLGrassRing.CreateAnnulus(2);

        Assert.IsNull(_mesh);
    }
}
}
