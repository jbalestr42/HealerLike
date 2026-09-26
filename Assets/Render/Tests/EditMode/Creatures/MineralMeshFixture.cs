using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using HealerLike.Render.Grammar;

namespace HealerLike.Render.Creatures
{

public abstract class MineralMeshFixture
{
    readonly List<Mesh> _meshes = new List<Mesh>();

    protected Mesh Build(ShapeProfile shape, int variant)
    {
        Mesh mesh = ProceduralShapeMeshes.Create(shape, variant);
        Assert.IsNotNull(mesh);
        _meshes.Add(mesh);
        return mesh;
    }

    [TearDown]
    public void TearDown()
    {
        foreach (Mesh mesh in _meshes)
        {
            Object.DestroyImmediate(mesh);
        }

        _meshes.Clear();
    }

    protected static void AssertOnSurface(Mesh mesh, Vector3 point)
    {
        Vector3[] vertices = mesh.vertices;
        int[] triangles = mesh.triangles;
        for (int i = 0; i < triangles.Length; i += 3)
        {
            Vector3 a = vertices[triangles[i]],
                b = vertices[triangles[i + 1]],
                c = vertices[triangles[i + 2]];
            Vector3 normal = Vector3.Cross(b - a, c - a).normalized;
            if (Mathf.Abs(Vector3.Dot(normal, point - a)) > 0.00001f)
            {
                continue;
            }

            if (Vector3.Dot(Vector3.Cross(b - a, point - a), normal) < -0.00001f)
            {
                continue;
            }

            if (Vector3.Dot(Vector3.Cross(c - b, point - b), normal) < -0.00001f)
            {
                continue;
            }

            if (Vector3.Dot(Vector3.Cross(a - c, point - c), normal) < -0.00001f)
            {
                continue;
            }

            return;
        }

        Assert.Fail("The attachment point is not on the actual generated surface.");
    }
}
}
