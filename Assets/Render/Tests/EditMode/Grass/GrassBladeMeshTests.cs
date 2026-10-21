using HealerLike.Render.Creatures;
using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Grass
{

public class GrassBladeMeshTests
{
    [Test]
    public void Create_OneSegment_IsThePyramidsFacets()
    {
        Mesh blade = GrassBladeMesh.Create(1);
        Mesh pyramid = FacetedMeshes.CreatePyramid("Tuft", false);
        try
        {
            Assert.AreEqual(4, blade.triangles.Length / 3);
            for (int side = 0; side < 4; side++)
            {
                Vector3 bladeNormal = Vector3.Cross(Corner(blade, side, 1) - Corner(blade, side, 0),
                                                    Corner(blade, side, 2) - Corner(blade, side, 0)).normalized;
                Assert.That(Vector3.Distance(bladeNormal, pyramid.normals[side * 3]), Is.LessThan(1e-5f));
            }
        }
        finally
        {
            Object.DestroyImmediate(blade);
            Object.DestroyImmediate(pyramid);
        }
    }

    static Vector3 Corner(Mesh mesh, int triangle, int corner)
    {
        return mesh.vertices[mesh.triangles[triangle * 3 + corner]];
    }

    [Test]
    public void Create_Segments_SpanRootToApexWithinTheTaper()
    {
        Mesh blade = GrassBladeMesh.Create(4);
        try
        {
            Assert.AreEqual(4 * 7, blade.triangles.Length / 3);
            float top = 0f;
            foreach (Vector3 vertex in blade.vertices)
            {
                float halfWidth = 0.5f * (1f - vertex.y) + 1e-5f;
                Assert.LessOrEqual(Mathf.Abs(vertex.x), halfWidth);
                Assert.LessOrEqual(Mathf.Abs(vertex.z), halfWidth);
                top = Mathf.Max(top, vertex.y);
            }

            Assert.AreEqual(1f, top);
        }
        finally
        {
            Object.DestroyImmediate(blade);
        }
    }

    [Test]
    public void Create_EveryTriangle_FacesItsSidesNormal()
    {
        Mesh blade = GrassBladeMesh.Create(3);
        try
        {
            int[] triangles = blade.triangles;
            for (int i = 0; i < triangles.Length; i += 3)
            {
                Vector3 a = blade.vertices[triangles[i]];
                Vector3 face = Vector3.Cross(blade.vertices[triangles[i + 1]] - a, blade.vertices[triangles[i + 2]] - a);
                Assert.Greater(Vector3.Dot(face, blade.normals[triangles[i]]), 0f);
                Assert.Greater(Vector3.Dot(blade.normals[triangles[i]], new Vector3(a.x, 0f, a.z)), -1e-5f,
                    "Normals point out of the spire.");
            }
        }
        finally
        {
            Object.DestroyImmediate(blade);
        }
    }

    [Test]
    public void Shared_SameCount_ReturnsOneMeshAndClampsTheCount()
    {
        Assert.AreSame(GrassBladeMesh.Shared(4), GrassBladeMesh.Shared(4));
        Assert.AreSame(GrassBladeMesh.Shared(1), GrassBladeMesh.Shared(0));
        Assert.AreSame(GrassBladeMesh.Shared(GrassBladeMesh.MaxSegments), GrassBladeMesh.Shared(99));
    }
}

}
